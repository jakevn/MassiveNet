// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Assigned its duties by NetZoneManager, NetZoneServer provides functionality for maintaining
    /// awareness of peers and handing off appropriate NetViews to peers, as well as signaling to newly
    /// connected clients (NetZoneClient) of adjacent Zones to connect to.
    /// </summary>
    public class NetZoneServer : MonoBehaviour {

        public delegate void OnReceivedAssignment();

        /// <summary> Called when assigned to a Zone. </summary>
        public event OnReceivedAssignment OnAssignment;

        public delegate void OnClientSetupSuccess(NetConnection connection);

        /// <summary> Called when a client succeeds to connect to all required Zone servers. </summary>
        public event OnClientSetupSuccess OnClientSuccess;

        public delegate void OnClientSetupFailed(NetConnection connection);

        /// <summary> Called when a client fails to connect to all required Zone servers. </summary>
        public event OnClientSetupFailed OnClientFailed;

        /// <summary> Returns the position of the assigned Zone. </summary>
        public Vector3 Position {
            get { return self.Position; }
        }

        private readonly List<NetZone> peers = new List<NetZone>();
        private readonly Dictionary<IPEndPoint, NetZone> peerLookup = new Dictionary<IPEndPoint, NetZone>();

        private NetZone self;
        private int viewIdOffset;

        internal NetSocket Socket;
        internal NetViewManager ViewManager;

        private void Awake() {
            Socket = GetComponent<NetSocket>();
            ViewManager = GetComponent<NetViewManager>();

            Socket.RegisterRpcListener(this);
            Socket.Events.OnPeerConnected += PeerConnected;
            Socket.Events.OnPeerDisconnected += PeerDisconnected;
            Socket.Events.OnClientConnected += ClientConnected;
        }

        private void Update() {
            if (self == null || peers.Count == 0) return;
            IncrementalHandoffCheck();
        }

        private int AllocateViewId() {
            if (self == null) return 0;
            if (self.ViewIdMin + viewIdOffset > self.ViewIdMax) return 0;
            return self.ViewIdMin + viewIdOffset++;
        }

        internal void AssignZoneSelf(NetZone zone) {
            if (ViewManager.GenerateViewId == null) ViewManager.GenerateViewId += AllocateViewId;
            NetLog.Info("Assigned to zone.");
            self = zone;
            if (OnAssignment != null) OnAssignment();
        }

        [NetRPC]
        private string AssignZone(NetZone zone, NetConnection connection) {
            if (!connection.IsPeer && !connection.IsServer) return "";
            AssignZoneSelf(zone);
            return Socket.Address;
        }

        [NetRPC]
        private void AddPeer(NetZone zone, NetConnection connection) {
            if (!connection.IsPeer && !connection.IsServer) return;
            AddPeerSelf(zone);
            if (!Socket.EndpointConnected(zone.ServerEndpoint)) Socket.ConnectToPeer(zone.ServerEndpoint);
        }

        internal void AddPeerSelf(NetZone zone) {
            if (peerLookup.ContainsKey(zone.ServerEndpoint)) peerLookup.Remove(zone.ServerEndpoint);
            peerLookup.Add(zone.ServerEndpoint, zone);

            if (peers.Contains(zone)) peers.Remove(zone);
            peers.Add(zone);

            if (Socket.EndpointConnected(zone.ServerEndpoint)) PeerConnected(Socket.EndpointToConnection(zone.ServerEndpoint));
        }

        [NetRPC]
        private void RemovePeer(NetZone zone, NetConnection connection) {
            if (!connection.IsPeer && !connection.IsServer) return;
            RemovePeerSelf(zone);
        }

        internal void RemovePeerSelf(NetZone zone) {
            NetLog.Info("Removing zone");

            if (peerLookup.ContainsKey(zone.ServerEndpoint)) peerLookup.Remove(zone.ServerEndpoint);
            else if (peerLookup.ContainsValue(zone)) NetLog.Warning("RemovePeer: Zone endpoint mismatch.");

            if (peers.Contains(zone)) peers.Remove(zone);
            else NetLog.Info("RemovePeer: Zone not in peer list.");
        }

        // Loop controls that need to persist between invocations of IncrementalHandoffCheck:
        private int incHandoffFrame;
        private int handoffBatchSize = 4;

        private void IncrementalHandoffCheck() {
            int pos = incHandoffFrame*handoffBatchSize;

            for (int i = pos; i < pos + handoffBatchSize; i++) {

                if (i >= ViewManager.Views.Count) break;

                NetView view = ViewManager.Views[i];
                if (view.Server != Socket.Self) continue;

                float dist = Vector3.Distance(view.Scope.Position, self.Position);
                if (dist < self.HandoverDistance) continue;

                for (int j = 0; j < peers.Count; j++) {
                    NetZone peer = peers[j];
                    float peerDist = Vector3.Distance(view.Scope.Position, peer.Position);
                    if (peerDist > peer.HandoverMinDistance && (dist < self.HandoverMaxDistance || dist < peerDist)) continue;
                    StartHandoff(view, peer);
                    break;
                }
            }

            incHandoffFrame++;
            if (incHandoffFrame != (Application.targetFrameRate*2)) return;

            incHandoffFrame = 0;
            handoffBatchSize = (ViewManager.Views.Count/(Application.targetFrameRate*2)) + 1;
        }

        private void StartHandoff(NetView view, NetZone peer) {
            view.SendCreatorData(peer.Server);
            view.Server = peer.Server;
            foreach (NetConnection connection in view.Controllers) {
                if (connection.View == view) connection.InternalScope = view.Scope;
            }
            ViewManager.DestroyView(view);
        }

        private void PeerConnected(NetConnection connection) {
            if (!peerLookup.ContainsKey(connection.Endpoint)) return;
            NetZone peer = peerLookup[connection.Endpoint];
            NetLog.Info("Connection is Zone Peer: " + peer);
            peer.Server = connection;
            connection.InternalScope = new NetScope();
            connection.InternalScope.Position = peer.Position;
            connection.InternalScope.OutScopeDist = (int)(peer.HandoverMaxDistance * 1.25);
            connection.InternalScope.InScopeDist = peer.HandoverMaxDistance;
        }

        private void PeerDisconnected(NetConnection connection) {
            if (!peerLookup.ContainsKey(connection.Endpoint)) return;
            RemovePeerSelf(peerLookup[connection.Endpoint]);
        }

        private void ClientConnected(NetConnection connection) {
            SendPeers(connection);
        }

        [NetRPC]
        void ZoneConnectSuccess(NetConnection connection) {
            if (OnClientSuccess != null) OnClientSuccess(connection);
        }

        [NetRPC]
        void ZoneConnectFail(IPEndPoint ep, NetConnection connection) {
            if (OnClientFailed != null) OnClientFailed(connection);
        }

        private void SendPeers(NetConnection connection) {
            foreach (NetZone zone in peers) {
                Socket.Send("ConnectToZone", connection, peers.Count, zone.Server.Endpoint);
            }
        }
    }
}