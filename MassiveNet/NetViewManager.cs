// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace MassiveNet {
    // Param enum for RPC target endpoints
    public enum RpcTarget {
        /// <summary> Send an RPC to all connections, even if they are controllers. </summary>
        All,

        /// <summary> Send an RPC to all connections in the group, even if currently not in-scope. </summary>
        AllInclOutOfScope,

        /// <summary> Send an RPC only to the controllers of this NetView. </summary>
        Controllers,

        /// <summary> Send an RPC to all in-scope connections except for the controllers of this NetView. </summary>
        NonControllers,

        /// <summary> Send an RPC to the server of this netview. </summary>
        Server,

        None
    }

    internal enum ViewCmd : ushort {
        /// <summary> Instantiation command to a connection that is not the owner of the new NetView. </summary>
        CreateProxyView = 2030,

        /// <summary> Sender provides data for to create a new NetView with receiver as owner. </summary>
        CreateOwnerView = 2029,

        CreateCreatorView = 2028,

        CreatePeerView = 2027,

        /// <summary> Destroy the object associated with the NetViewID. </summary>
        DestroyView = 2026,

        /// <summary> View server has changed for supplied view ID. Sender of command is new server. </summary>
        ChangeServer = 2025,

        /// <summary> Sender tells client that the View has gone out of scope for them. </summary>
        OutOfScope = 2024,

        /// <summary> Sender provides stream for state synchronization. </summary>
        Sync = 2023
    }

    /// <summary>
    /// Handles creation, destruction, synchronization, and messaging for NetViews.
    /// </summary>
    public class NetViewManager : MonoBehaviour {

        /// <summary> How often Views should be synchronized across the network. </summary>
        public int SyncsPerSecond = 10;

        /// <summary> Incremental sync is designed to spread sync load across each frame. Since it is
        /// dependent on framerate and only beneficial when number of owned views is high, this should
        /// be set to false for clients. </summary>
        public bool IncrementalSync = true;

        /// <summary> The starting value for generating ViewIDs. </summary>
        private const int ViewIdMin = 1000;

        /// <summary> The maximum value for generating ViewIDs. </summary>
        private const int ViewIdMax = 1000000;

        public delegate int GetNewViewId();

        /// <summary> Delegate for generating a new ViewID. </summary>
        public GetNewViewId GenerateViewId;

        public delegate GameObject InstantiateViewPrefab(string prefabRoot, NetView.Relation relation);

        /// <summary> Delegate for instantiating a prefab. This is especially useful for pooling prefabs. </summary>
        public InstantiateViewPrefab InstantiatePrefab;

        public delegate void DestroyViewObj(GameObject view);

        internal delegate void NetViewCreated(NetView view);

        internal event NetViewCreated OnNetViewCreated;

        /// <summary> Delegate for destroying a NetView. Useful for returning to a pool. </summary>
        public DestroyViewObj DestroyViewObject;

        /// <summary> NetView lookup by ViewID. </summary> 
        internal readonly Dictionary<int, NetView> ViewLookup = new Dictionary<int, NetView>();

        internal readonly List<NetView> Views = new List<NetView>();

        internal NetSocket Socket;

        private void Awake() {
            Socket = GetComponent<NetSocket>();
            Socket.Events.OnMessageReceived += ReceiveMessage;

            RegisterCommands();
        }

        private void LateUpdate() {
            if (IncrementalSync) IncrementalSyncViews();
            else if (!IsInvoking("TimedSyncViews")) Invoke("TimedSyncViews", 1f / SyncsPerSecond);
        }

        public bool TryGetView(int viewId, out NetView foundView) {
            if (ViewLookup.ContainsKey(viewId)) {
                foundView = ViewLookup[viewId];
                return true;
            }
            foundView = null;
            return false;
        }

        private void RegisterCommands() {
            Socket.Command.Register((ushort)ViewCmd.CreateProxyView, ReceiveCreateView,
                new List<Type> { typeof(int), typeof(int), typeof(string), typeof(NetStream) });
            Socket.Command.Register((ushort)ViewCmd.CreateOwnerView, ReceiveCreateView,
                new List<Type> { typeof(int), typeof(int), typeof(string), typeof(NetStream) });
            Socket.Command.Register((ushort)ViewCmd.CreateCreatorView, ReceiveCreateView,
                new List<Type> { typeof(int), typeof(int), typeof(string), typeof(NetStream), typeof(IPEndPoint) });
            Socket.Command.Register((ushort)ViewCmd.CreatePeerView, ReceiveCreateView,
                new List<Type> { typeof(int), typeof(int), typeof(string), typeof(NetStream), typeof(IPEndPoint) });
            Socket.Command.Register((ushort)ViewCmd.DestroyView, ReceiveDestroyView, new List<Type> { typeof(int) });
            Socket.Command.Register((ushort)ViewCmd.ChangeServer, ReceiveChangeServer, new List<Type> { typeof(int) });
            Socket.Command.Register((ushort)ViewCmd.OutOfScope, ReceiveOutOfScope, new List<Type> { typeof(int) });
            Socket.Command.Register((ushort)ViewCmd.Sync, DeliverSyncStream, new List<Type> { typeof(NetStream) });
        }

        private void DeliverSyncStream(NetMessage message, NetConnection connection) {
            if (!ViewLookup.ContainsKey((int)message.ViewId)) return;
            var view = ViewLookup[(int)message.ViewId];
            if (!view.IsController(connection) && connection != view.Server) {
                if (!connection.IsServer) {
                    NetLog.Warning("Connection attempting to send to unauthorized View: " + connection.Endpoint);
                    return;
                }
                view.Server = connection;
            }
            view.TriggerReadSync((NetStream)message.Parameters[0]);
        }

        private void ReceiveMessage(NetMessage message, NetConnection connection) {
            if (!ViewLookup.ContainsKey((int)message.ViewId)) return;

            string methodName = Socket.Rpc.IdToName(message.MessageId);
            var view = ViewLookup[(int)message.ViewId];

            if (!view.IsController(connection) && connection != view.Server) {
                if (!connection.IsServer) {
                    NetLog.Warning("Connection attempting to send to unauthorized View: " + connection.Endpoint);
                    return;
                }
                view.Server = connection;
            }
            view.DispatchRpc(methodName, message, connection);
        }

        /// <summary> Handles the OutOfScope command by triggering the OnOutOfScope delegate for the view. </summary>
        private void ReceiveOutOfScope(NetMessage message, NetConnection connection) {
            if (!connection.IsServer && !connection.IsPeer) return;

            int viewId = (int)message.Parameters[0];
            if (!ViewLookup.ContainsKey(viewId)) return;
            ViewLookup[viewId].Scope.FireOutEvent();
        }

        /// <summary> Sends a command to all controllers of the View that we are the new Server. </summary>
        internal void SendChangeViewServer(int viewId) {
            if (!ViewLookup.ContainsKey(viewId)) return;

            var view = ViewLookup[viewId];
            if (view.Controllers.Count == 0) return;

            var message = NetMessage.Create((ushort)ViewCmd.ChangeServer, 0, new object[] { viewId }, true);
            for (int i = 0; i < view.Controllers.Count; i++) view.Controllers[i].Send(message);
        }

        /// <summary> Handles the ChangeServer command by changing the View's Server to that of the sender. </summary>
        internal void ReceiveChangeServer(NetMessage message, NetConnection connection) {
            if (!connection.IsServer && !connection.IsPeer) return;

            int viewId = (int)message.Parameters[0];
            if (!ViewLookup.ContainsKey(viewId)) return;
            ViewLookup[viewId].Server = connection;
        }

        /// <summary> Post-instantiation configuration of NetView. </summary>
        private void RegisterNetView(NetView view) {

            if (view.CachedRpcObjects == null) view.SetRpcCache(RpcInfoCache.CreateInstanceLookup(view.gameObject));
            view.Scope.Trans = view.gameObject.transform;
            view.Socket = Socket;
            view.ViewManager = this;

            if (!ViewLookup.ContainsKey(view.Id)) ViewLookup.Add(view.Id, view);
            if (!Views.Contains(view)) Views.Add(view);
        }

        /// <summary> Destroys all Views being served by the supplied connection. </summary>
        public void DestroyViewsServing(NetConnection connection) {
            for (int i = Views.Count - 1; i >= 0; i--) {
                var view = Views[i];
                if (view.Server == connection) DestroyView(view);
            }
        }

        /// <summary> Destroys all Views that the supplied connection is authorized for. </summary>
        public void DestroyAuthorizedViews(NetConnection connection) {
            for (int i = connection.Authorizations.Count - 1; i >= 0; i--) {
                int viewId = connection.Authorizations[i];
                if (ViewLookup.ContainsKey(viewId)) DestroyView(ViewLookup[viewId]);
            }
        }

        /// <summary> Sends an RPC to connections that are in-scope for the provided view. </summary>
        internal void Send(NetView view, NetMessage netMessage, RpcTarget target) {
            switch (target) {
                case (RpcTarget.All):
                    for (int i = 0; i < Socket.Connections.Count; i++) {
                        var connection = Socket.Connections[i];
                        if (!connection.HasScope) continue;
                        if (view.Group != 0 && !connection.InGroup(view.Group)) continue;
                        if ((netMessage.Reliable && connection.Scope.In(view.Id)) || connection.Scope.In(view.Id, syncFrame) || view.IsController(connection)) connection.Send(netMessage);
                    }
                    break;
                case (RpcTarget.Controllers):
                    foreach (NetConnection controller in view.Controllers) {
                        if (controller == Socket.Self) continue;
                        controller.Send(netMessage);
                    }
                    break;
                case (RpcTarget.NonControllers):
                    for (int i = 0; i < Socket.Connections.Count; i++) {
                        var connection = Socket.Connections[i];
                        if (connection.IsServer || !connection.HasScope) continue;
                        if (view.IsController(connection)) continue;
                        if (view.Group != 0 && !connection.InGroup(view.Group)) continue;
                        if ((netMessage.Reliable && connection.Scope.In(view.Id)) || connection.Scope.In(view.Id, syncFrame)) connection.Send(netMessage);
                    }
                    break;
                case (RpcTarget.Server):
                    if (view.Server != Socket.Self) view.Server.Send(netMessage);
                    else NetLog.Warning("Trying to send message to self.");
                    break;
                case (RpcTarget.AllInclOutOfScope):
                    for (int i = 0; i < Socket.Connections.Count; i++) {
                        var connection = Socket.Connections[i];
                        if (view.Group != 0 && !connection.InGroup(view.Group)) continue;
                        connection.Send(netMessage);
                    }
                    break;
            }
        }

        /// <summary> Send overload that creates the NetMessage for the RPC. </summary>
        internal void Send(int viewId, bool reliable, string methodName, RpcTarget target, params object[] parameters) {
            if (!Socket.Rpc.HasId(methodName)) {
                NetLog.Error("Send failed: RPC method name has not been assigned an ID.");
                return;
            }
            if (!ViewLookup.ContainsKey(viewId)) return;
            NetView view = ViewLookup[viewId];
            var netMessage = NetMessage.Create(Socket.Rpc.NameToId(methodName), (uint)viewId, parameters, reliable);
            Send(view, netMessage, target);
        }

        /// <summary> Send overload that creates the NetMessage for the RPC. </summary>
        internal void Send(int viewId, bool reliable, string methodName, NetConnection target, params object[] parameters) {
            if (!Socket.Rpc.HasId(methodName)) {
                NetLog.Error("Send failed: RPC method name has not been assigned an ID.");
                return;
            }
            var netMessage = NetMessage.Create(Socket.Rpc.NameToId(methodName), (uint)viewId, parameters, reliable);
            target.Send(netMessage);
        }

        /// <summary> Send overload that creates the NetMessage for the RPC. </summary>
        internal void Send(int viewId, bool reliable, string methodName, List<NetConnection> targets, params object[] parameters) {
            if (!Socket.Rpc.HasId(methodName)) {
                NetLog.Error("Send failed: RPC method name has not been assigned an ID.");
                return;
            }

            var message = NetMessage.Create(Socket.Rpc.NameToId(methodName), (uint)viewId, parameters, reliable);

            for (int i = 0; i < targets.Count; i++) targets[i].Send(message);
        }

        internal void SendOutOfScope(int viewId, NetConnection connection) {
            Socket.Command.Send((int)ViewCmd.OutOfScope, connection, viewId);
        }

        // Loop controls that need to persist between invocations of IncrementalSyncViews:
        private int syncFrame;
        private int incSyncFrame;
        private int framesPerSync = 4;
        private int incBatchSize = 4;

        /// <summary>
        /// Syncs views every frame in batches. The batch size (number of views to sync per frame)
        /// is calculated using the number of views, target frame rate, and syncs per second.
        /// This is the preferred server-side method to use when there are a large number of views
        /// to sync and framerate is stable. This method allows load to be spread evenly across frames.
        /// </summary>
        private void IncrementalSyncViews() {
            int pos = incSyncFrame * incBatchSize;

            for (int i = pos; i < pos + incBatchSize; i++) {
                if (i >= Views.Count) break;
                var view = Views[i];
                if (view.Server != Socket.Self && !view.IsController(Socket.Self)) continue;
                view.TriggerSyncEvent();
            }

            incSyncFrame++;
            if (incSyncFrame != framesPerSync) return;

            incSyncFrame = 0;
            framesPerSync = Socket.TargetFrameRate / SyncsPerSecond;
            incBatchSize = (Views.Count / framesPerSync) + 1;

            syncFrame++;
            if (syncFrame > 4) syncFrame = 1;
        }

        /// <summary>
        /// Syncs all owned views at intervals determined by SyncsPerSecond.
        /// This is generally the preferred method for client-side use.
        /// </summary>
        private void TimedSyncViews() {
            foreach (NetView view in Views) {
                if (view.Server != Socket.Self && !view.IsController(Socket.Self)) continue;
                view.TriggerSyncEvent();
            }

            if (!IncrementalSync) Invoke("TimedSyncViews", 1f / SyncsPerSecond);
        }

        public NetView CreateView(string prefabBase, NetStream instantiateData) {
            NetView view = CreateView(null, 0, prefabBase, instantiateData);
            return view;
        }

        /// <summary> Authoritatively creates a view that the server owns and triggers network instantiation. </summary>
        public NetView CreateView(string prefabBase) {
            return CreateView(0, prefabBase);
        }

        public NetView CreateView(NetConnection controller, string prefabBase, NetStream instantiateData) {
            NetView view = CreateView(controller, 0, prefabBase, instantiateData);
            return view;
        }

        /// <summary> Authoritatively creates a view for a connected client and triggers network instantiation. </summary>
        public NetView CreateView(NetConnection controller, string prefabBase) {
            return CreateView(controller, 0, prefabBase);
        }

        public NetView CreateView(int group, string prefabBase, NetStream instantiateData) {
            NetView view = CreateView(null, group, prefabBase, instantiateData);
            return view;
        }

        /// <summary> Authoritatively creates a view that the server owns and triggers network instantiation. </summary>
        public NetView CreateView(int group, string prefabBase) {
            return CreateView(null, group, prefabBase);
        }

        public NetView CreateView(NetConnection controller, int group, string prefabBase, NetStream instantiateData) {
            var view = CreateView(controller, Socket.Self, NewViewId(), group, prefabBase, NetView.Relation.Creator);
            view.TriggerReadInstantiateData(instantiateData);
            if (OnNetViewCreated != null) OnNetViewCreated(view);
            return view;
        }

        /// <summary> Authoritatively creates a view for a connected client and triggers network instantiation. </summary>
        public NetView CreateView(NetConnection controller, int group, string prefabBase) {
            var view = CreateView(controller, Socket.Self, NewViewId(), group, prefabBase, NetView.Relation.Creator);
            if (OnNetViewCreated != null) OnNetViewCreated(view);
            return view;
        }

        private NetView CreateView(NetConnection controller, NetConnection server, int viewId, int group, string prefabRoot, NetView.Relation relation) {

            NetView view = null;
            if (ViewLookup.ContainsKey(viewId)) {
                NetView oldView = ViewLookup[viewId];
                if (oldView.CurrentRelation == relation) {
                    view = oldView;
                } else if (server == Socket.Self) {
                    NetScope oldScope = oldView.Scope;
                    Vector3 oldPos = oldView.transform.position;
                    if (oldView.Server != server) SendChangeViewServer(viewId);
                    DestroyView(oldView);
                    view = InstantiateView(prefabRoot, relation);
                    view.InternalScope = oldScope;
                    view.transform.position = oldPos;
                }
            }

            if (view == null) view = InstantiateView(prefabRoot, relation);

            view.Server = server;
            view.Id = viewId;
            view.PrefabRoot = prefabRoot;
            view.Group = group;
            view.CurrentRelation = relation;

            if (controller != null) {
                view.AddController(controller);
                controller.AddAuthorization(view.Id);
                controller.AddToGroup(group);
                if (controller != Socket.Self) {
                    controller.View = view;
                    if (controller.InternalScope != null && view.Controllers.Count == 1) {
                        view.InternalScope = controller.InternalScope;
                    }
                    controller.InternalScope = null;
                }
            }
            view.Scope.FireInEvent();
            RegisterNetView(view);

            return view;
        }

        private static string Prefab(string prefabRoot, NetView.Relation relation) {
            if (relation == NetView.Relation.Creator) return prefabRoot + "@Creator";
            if (relation == NetView.Relation.Owner) return prefabRoot + "@Owner";
            if (relation == NetView.Relation.Peer) return prefabRoot + "@Peer";
            return prefabRoot + "@Proxy";
        }

        private void ReceiveCreateView(NetMessage message, NetConnection server) {
            if (!server.IsServer && !server.IsPeer) return;

            int viewId = (int)message.Parameters[0];
            int group = (int)message.Parameters[1];
            string prefabRoot = (string)message.Parameters[2];
            var stream = (NetStream)message.Parameters[3];

            NetView.Relation relation = default(NetView.Relation);
            NetConnection controller = null;
            switch (message.MessageId) {
                case (int)ViewCmd.CreateOwnerView:
                    controller = Socket.Self;
                    relation = NetView.Relation.Owner;
                    break;
                case (int)ViewCmd.CreatePeerView:
                    relation = NetView.Relation.Peer;
                    break;
                case (int)ViewCmd.CreateCreatorView:
                    server = Socket.Self;
                    relation = NetView.Relation.Creator;
                    break;
                case (int)ViewCmd.CreateProxyView:
                    relation = NetView.Relation.Proxy;
                    break;
            }

            if (relation == NetView.Relation.Creator || relation == NetView.Relation.Peer) {
                var ipendpoint = (IPEndPoint)message.Parameters[4];
                if (ipendpoint != null) {
                    if (Socket.EndpointConnected(ipendpoint)) controller = Socket.EndpointToConnection(ipendpoint);
                    else NetLog.Error("Failed to create view, controller endpoint not connected: " + ipendpoint);
                }
            }

            var view = CreateView(controller, server, viewId, group, prefabRoot, relation);
            view.TriggerReadInstantiateData(stream);
            if (OnNetViewCreated != null) OnNetViewCreated(view);
        }

        private NetView InstantiateView(string prefabRoot, NetView.Relation relation) {
            var viewObject = InstantiatePrefab != null
                ? InstantiatePrefab(prefabRoot, relation)
                : (GameObject)Instantiate(Resources.Load(Prefab(prefabRoot, relation)));

            var view = viewObject.GetComponent<NetView>();

            if (view != null) return view;

            Destroy(viewObject);
            throw new Exception("Prefab does not have a NetView component attached.");
        }

        private int NewViewId() {
            if (GenerateViewId != null) return GenerateViewId();

            for (int i = ViewIdMin; i < ViewIdMax; i++) {
                if (ViewLookup.ContainsKey(i)) continue;
                return i;
            }

            throw new Exception("NewViewId failed: Limit reached.");
        }

        /// <summary> Called by the server to destroy a NetView across the network. </summary>
        public void DestroyView(NetView view) {
            if (view.Server == Socket.Self) SendDestroyView(view);

            if (view.Controllers.Count != 0) {
                foreach (NetConnection connection in view.Controllers) {
                    connection.RemoveAuthorization(view.Id);
                }
            }
            ViewLookup.Remove(view.Id);
            Views.Remove(view);

            if (DestroyViewObject != null) DestroyViewObject(view.gameObject);
            else Destroy(view.gameObject);
        }

        /// <summary> Sends command to all connected clients to destroy the provided view. </summary>
        private void SendDestroyView(NetView view) {
            var destroyViewMessage = NetMessage.Create((ushort)ViewCmd.DestroyView, 0, 1, true);

            destroyViewMessage.Parameters[0] = view.Id;

            for (int i = 0; i < Socket.Connections.Count; i++) {
                var connection = Socket.Connections[i];
                if (connection.IsServer) continue;
                if (view.Group == 0 || connection.InGroup(view.Group)) connection.Send(destroyViewMessage);
            }
        }

        /// <summary> Processes a DestroyView command, this destroys the GameObject associated with the provided viewId. </summary>
        private void ReceiveDestroyView(NetMessage message, NetConnection connection) {
            int viewId = (int)message.Parameters[0];
            if (!ViewLookup.ContainsKey(viewId)) return;

            var view = ViewLookup[viewId];
            if (view.Server != connection) return;

            DestroyView(view);
        }

        /// <summary>
        ///  Returns the connection that matches the supplied target.
        ///  Returns the server for Server or the first controller for Controllers.
        /// </summary>
        internal NetConnection GetTarget(RpcTarget target, NetView view) {
            switch (target) {
                case RpcTarget.Server:
                    return view.Server;
                case RpcTarget.Controllers:
                    return view.Controllers[0];
            }

            NetLog.Error("Invalid RpcTarget for GetTarget. Only RpcTarget.Server or RpcTarget.Controllers can be used.");
            return null;
        }
    }
}