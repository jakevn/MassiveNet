// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System.Collections.Generic;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// NetView is a core component that should be added to any Unity prefab that is used for a network object.
    /// It is responsible for handling RPCs and synchronization for a particular NetView.
    /// A NetView is identified across the network via a ViewID.
    /// </summary>
    public class NetView : MonoBehaviour {
        /// <summary> Server-assigned unique network identifier for this view. </summary>
        public int Id { get; internal set; }

        /// <summary> The group number that this NetView belongs to. Only connections that are in this group will receive communications for this NetView. </summary>
        public int Group { get; internal set; }

        /// <summary> Returns true if we are the server for this View. </summary>
        public bool AmServer { get { return (Server != null && Server.IsServer == false && Server.IsPeer == false); } }

        /// <summary> The server that is currently responsible for the view. This is initially the server that created the NetView, but can change in handoff. </summary>
        public NetConnection Server { get; internal set; }

        public NetSocket Socket { get; internal set; }

        public NetViewManager ViewManager { get; internal set; }

        /// <summary>
        /// The different types of relationship for a NetView and a NetConnection.
        /// </summary>
        public enum Relation {
            Creator,
            Owner,
            Proxy,
            Peer
        }

        /// <summary>
        /// What type of relationship we have to this NetView.
        /// </summary>
        public Relation CurrentRelation { get; internal set; }

        /// <summary>
        /// The root name of the prefab for this View. The root name is everything before the @ suffix.
        /// </summary>
        public string PrefabRoot { get; internal set; }

        /// <summary> The connections that are authorized to send
        ///  RPCs to this NetView. The server uses this to determine which RPCs to allow from where. </summary>
        internal readonly List<NetConnection> Controllers = new List<NetConnection>();

        /// <summary> This is the cache for object instances for RPC methods. The key is the method name and the value is the Monobehavior instance. </summary>
        internal Dictionary<string, object> CachedRpcObjects;

        public delegate void ReadSync(NetStream syncStream);

        /// <summary> When a sync stream is received, OnReadSync provides the NetStream which contains the sync data. </summary>
        public event ReadSync OnReadSync;

        public delegate RpcTarget WriteSync(NetStream syncStream);

        /// <summary> OnWriteSync signals the sync event. Any RPC sent during sync will be batched and sent unreliably. </summary>
        public event WriteSync OnWriteSync;

        public delegate void ReadInstantiateData(NetStream stream);

        /// <summary> Provides the NetStream that contains instantiation data (if any). </summary>
        public event ReadInstantiateData OnReadInstantiateData;

        public delegate void WriteProxyData(NetStream stream);

        /// <summary> All data necessary to replicate this View's state for a proxy should be written to the stream. </summary>
        public event WriteProxyData OnWriteProxyData;

        public delegate void WriteOwnerData(NetStream stream);

        /// <summary> All data necessary to replicate this View's state for the owner should be written to the stream. </summary>
        public event WriteOwnerData OnWriteOwnerData;

        public delegate void WriteCreatorData(NetStream stream);

        /// <summary> All data necessary to fully replicate this View's state should be written to the stream. </summary>
        public event WriteCreatorData OnWriteCreatorData;

        public delegate void WritePeerData(NetStream stream);

        /// <summary> All data necessary to replicate this View's state for a peer should be written to the stream. </summary>
        public event WritePeerData OnWritePeerData;

        /// <summary> Instead of creating a new NetStream for each sync event, it's cached here for performance. </summary>
        private NetStream syncStream;

        /// <summary> Instead of creating a new NetMessage for each sync event, it's cached here for performance. </summary>
        private NetMessage syncMessage;

        /// <summary> The scope config determines how this View is treated during scope calculations. </summary>
        public NetScope Scope {
            get { return InternalScope; }
        }

        internal NetScope InternalScope = new NetScope();

        /// <summary> Adds the provided connection to the controller list, allowing connection to send RPCs to this view. </summary>
        internal void AddController(NetConnection connection) {
            Controllers.Add(connection);
        }

        /// <summary> Returns true if the supplied connection is in the controllers list. </summary>
        internal bool IsController(NetConnection connection) {
            return Controllers.Contains(connection);
        }

        internal void RemoveController(NetConnection connection) {
            if (Controllers.Contains(connection)) Controllers.Remove(connection);
        }

        /// <summary>
        /// When an RPC is received, the targeted NetView is identified and DispatchRPC is called for that
        /// particular NetView object. DispatchRPC then identifies the targeted method and invokes it with
        /// the supplied parameters.
        /// </summary>
        internal void DispatchRpc(string methodName, NetMessage message, NetConnection connection) {
            if (!CachedRpcObjects.ContainsKey(methodName)) {
                NetLog.Error(string.Format("Can't find RPC method \"{0}\" for View {1}.", methodName, Id));
                return;
            }
            Socket.Rpc.Invoke(CachedRpcObjects[methodName], methodName, message, connection);
        }

        /// <summary>
        /// Assigns the generated lookup of RPC method names and the MonoBehaviour instance that contains the method.
        /// </summary>
        internal void SetRpcCache(Dictionary<string, object> cache) {
            CachedRpcObjects = cache;
        }

        /// <summary>
        /// Sends a reliable RPC. This should be used for infrequent messages which require guaranteed delivery.
        /// Reliable should never be used for state-sync due to the increased bandwidth, garbage, and CPU overhead.
        /// </summary>
        /// <param name="methodName"> The name of the RPC method. </param>
        /// <param name="target"> Who to send the RPC to. For a client, this should always be Server. </param>
        /// <param name="parameters">The parameters to send.</param>
        public void SendReliable(string methodName, RpcTarget target, params object[] parameters) {
            ViewManager.Send(Id, true, methodName, target, parameters);
        }


        /// <summary>
        /// Sends a reliable RPC. This should be used for infrequent messages which require guaranteed delivery.
        /// Reliable should never be used for state-sync due to the increased bandwidth, garbage, and CPU overhead.
        /// </summary>
        /// <param name="methodName"> The name of the RPC method. </param>
        /// <param name="target"> The NetConnection to send the RPC to. </param>
        /// <param name="parameters">The parameters to send.</param>
        public void SendReliable(string methodName, NetConnection target, params object[] parameters) {
            ViewManager.Send(Id, true, methodName, target, parameters);
        }

        /// <summary>
        /// Sends a reliable RPC. This should be used for infrequent messages which require guaranteed delivery.
        /// Reliable should never be used for state-sync due to the increased bandwidth, garbage, and CPU overhead.
        /// </summary>
        /// <param name="methodName"> The name of the RPC method. </param>
        /// <param name="targets"> The NetConnections to send the RPC to. </param>
        /// <param name="parameters">The parameters to send.</param>
        public void SendReliable(string methodName, List<NetConnection> targets, params object[] parameters) {
            ViewManager.Send(Id, true, methodName, targets, parameters);
        }

        /// <summary>
        /// Sends a reliable RPC and awaits a response. NetRequests are useful for getting a return value from a RPC.
        /// Requests should not be used for very frequent messages such as state-sync due to overhead.
        /// </summary>
        /// <param name="methodName"> The name of the RPC method associated with the request. </param>
        /// <param name="target"> Who to send the request to. Requests should only be sent to a single target. </param>
        /// <param name="parameters">The parameters to send for the request.</param>
        public Request<T> SendRequest<T>(string methodName, RpcTarget target, params object[] parameters) {
            return Socket.Request.Send<T>((uint) Id, methodName, ViewManager.GetTarget(target, this), parameters);
        }

        /// <summary>
        /// Sends an unreliable RPC. This should be used for more frequent messages that don't require reliability.
        /// </summary>
        /// <param name="methodName"> The name of the RPC method. </param>
        /// <param name="target">Who to send the RPC to. For a client, this should always be Server.</param>
        /// <param name="parameters">The parameters to send.</param>
        public void SendUnreliable(string methodName, RpcTarget target, params object[] parameters) {
            ViewManager.Send(Id, false, methodName, target, parameters);
        }

        /// <summary>
        /// Sends the NetStream to the RPC at the supplied target.
        /// </summary>
        /// <param name="target"> Who to send the RPC to. For a client, this should always be Server. </param>
        private void SendSync(RpcTarget target) {
            if (target == RpcTarget.None) return;
            if (syncMessage == null) {
                syncMessage = NetMessage.Create((ushort)ViewCmd.Sync, (uint) Id, 1, false);
            }
            syncMessage.Parameters[0] = syncStream;
            ViewManager.Send(this, syncMessage, target);
        }

        public void DisableSync() {
            syncEnabled = false;
        }

        public void EnableSync() {
            syncEnabled = true;
        }

        private bool syncEnabled = true;
        /// <summary> Fires sync event to trigger sync messsage creation. </summary>
        internal void TriggerSyncEvent() {
            if (!syncEnabled || OnWriteSync == null) return;
            if (syncStream == null) {
                syncStream = NetStream.New();
            }
            syncStream.Reset();
            SendSync(OnWriteSync(syncStream));
        }

        internal void TriggerReadSync(NetStream stream) {
            if (OnReadSync != null) OnReadSync(stream);
            stream.Release();
        }

        internal bool CanInstantiateFor(NetConnection connection) {
            if (IsController(connection) && HasOwnerData()) return true;
            if (connection.IsPeer && HasPeerData()) return true;
            if (!connection.IsPeer && HasProxyData()) return true;
            return false;
        }

        internal void SendInstantiateData(NetConnection connection) {
            if (IsController(connection)) {
                if (!HasOwnerData()) return;
                SendOwnerData(connection);
            } 
            //else if (connection.PeerApproval && HasCreatorData()) SendCreatorData(connection);
            else if (connection.IsPeer && HasPeerData()) SendPeerData(connection);
            else if (!connection.IsPeer && HasProxyData()) SendProxyData(connection); 
        }

        internal void TriggerReadInstantiateData(NetStream stream) {
            if (OnReadInstantiateData != null) OnReadInstantiateData(stream);
            stream.Release();
        }

        private void SendPeerData(NetConnection connection) {
            var msg = NetMessage.Create((ushort)ViewCmd.CreatePeerView, (uint)Id, 5, true);
            msg.Parameters[0] = Id;
            msg.Parameters[1] = Group;
            msg.Parameters[2] = PrefabRoot;
            msg.Parameters[3] = TriggerGetPeerData();
            if (Controllers.Count > 0) msg.Parameters[4] = Controllers[0].Endpoint;
            connection.Send(msg);
        }

        private void SendProxyData(NetConnection connection) {
            var msg = NetMessage.Create((ushort)ViewCmd.CreateProxyView, (uint)Id, 4, true);
            msg.Parameters[0] = Id;
            msg.Parameters[1] = Group;
            msg.Parameters[2] = PrefabRoot;
            msg.Parameters[3] = TriggerGetProxyData();
            connection.Send(msg);
        }

        private void SendOwnerData(NetConnection connection) {
            var msg = NetMessage.Create((ushort)ViewCmd.CreateOwnerView, (uint)Id, 4, true);
            msg.Parameters[0] = Id;
            msg.Parameters[1] = Group;
            msg.Parameters[2] = PrefabRoot;
            msg.Parameters[3] = TriggerGetOwnerData();
            connection.Send(msg);
        }

        internal void SendCreatorData(NetConnection connection) {
            if (!HasCreatorData()) return;
            var msg = NetMessage.Create((ushort)ViewCmd.CreateCreatorView, (uint)Id, 5, true);
            msg.Parameters[0] = Id;
            msg.Parameters[1] = Group;
            msg.Parameters[2] = PrefabRoot;
            msg.Parameters[3] = TriggerGetCreatorData();
            if (Controllers.Count > 0) msg.Parameters[4] = Controllers[0].Endpoint;
            connection.Send(msg);
        }

        internal bool HasProxyData() {
            return (OnWriteProxyData != null);
        }

        internal NetStream TriggerGetProxyData() {
            var proxyStream = NetStream.New();
            OnWriteProxyData(proxyStream);
            return proxyStream;
        }

        internal bool HasOwnerData() {
            return (OnWriteOwnerData != null);
        }

        internal NetStream TriggerGetOwnerData() {
            var ownerStream = NetStream.New();
            OnWriteOwnerData(ownerStream);
            return ownerStream;
        }

        internal bool HasPeerData() {
            return (OnWritePeerData != null);
        }

        internal NetStream TriggerGetPeerData() {
            var peerStream = NetStream.New();
            OnWritePeerData(peerStream);
            return peerStream;
        }

        internal bool HasCreatorData() {
            return (OnWriteCreatorData != null);
        }

        internal NetStream TriggerGetCreatorData() {
            var fullStream = NetStream.New();
            OnWriteCreatorData(fullStream);
            return fullStream;
        }

        public bool TryGetCreatorData(out NetStream creatorData) {
            if (!HasCreatorData()) {
                creatorData = null;
                return false;
            }
            creatorData = TriggerGetCreatorData();
            return true;
        }
    }
}