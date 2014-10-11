// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Responsible for handling Zone creation and assignment of peers to unassigned Zones.
    /// </summary>
    public class NetZoneManager : MonoBehaviour {

        private readonly List<NetZone> zones = new List<NetZone>();

        /// <summary> Servers that have not been assigned or finished assignment. The value is true if the server is currently being assigned. </summary>
        private readonly Dictionary<NetConnection, bool> unassignedPeers = new Dictionary<NetConnection, bool>();

        private readonly List<NetConnection> peers = new List<NetConnection>();

        /// <summary> The current allocation position. Whenever a ViewId range is allocated, this is incremented by ViewIdBlockSize. </summary>
        private int viewAllocationPosition = 1000;

        /// <summary> ViewIDs are allocated in blocks of this size. Each Zone will receive this number of ViewIDs to use. </summary>
        private const int ViewIdBlockSize = 32000;

        /// <summary> The upper limit of ViewId. A ViewId larger than this number would break communications. </summary>
        private const int MaxViewId = 1025000;

        internal NetSocket Socket;

        /// <summary> When true, acts as Zone authority. </summary>
        public bool Authority = false;

        private void Awake() {
            Socket = GetComponent<NetSocket>();
            Socket.RegisterRpcListener(this);
            Socket.Events.OnPeerConnected += PeerConnected;
            Socket.Events.OnPeerDisconnected += PeerDisconnected;
        }

        private void PeerDisconnected(NetConnection connection) {
            NetLog.Info("ZoneManager: Peer Disconnected: " + connection.Endpoint);
            if (peers.Contains(connection)) peers.Remove(connection);
            if (Authority) RemoveServer(connection);
        }

        private void PeerConnected(NetConnection connection) {
            if (!peers.Contains(connection)) peers.Add(connection);
            if (Authority) AddServer(connection);
        }

        /// <summary> Creates a Zone at the specified position. </summary>
        public NetZone CreateZone(Vector3 position) {

            if (!CanAllocateIdBlock()) {
                throw new Exception("Cannot Create Zone: Unable to allocate ViewID block, limit has been reached.");
            }

            var newZone = new NetZone(position, GetViewIdMin(), GetViewIdMax());
            zones.Add(newZone);
            AssignServers();
            return newZone;
        }

        /// <summary> Attempts to find a Zone for the provided position. </summary>
        public bool TryGetZone(Vector3 position, out NetZone zone) {

            NetZone closestZone = null;
            float closestDistance = float.MaxValue;

            foreach (NetZone z in zones) {
                if (!z.Assigned) continue;

                float distance = z.Distance(position);
                if (distance > z.HandoverMaxDistance) continue;
                if (distance > closestDistance) continue;

                closestZone = z;
                closestDistance = distance;
            }

            if (closestZone == null) {
                zone = null;
                return false;
            }

            zone = closestZone;
            return true;
        }

        /// <summary> Adds self as Zone assignment candidate. </summary>
        public void AddSelfAsServer() {
            AddServer(Socket.Self);
        }

        private void AddServer(NetConnection server) {
            unassignedPeers.Add(server, false);
            AssignServers();
        }

        private void RemoveServer(NetConnection server) {
            for (int i = 0; i < zones.Count; i++) {
                if (zones[i].Server == server) zones[i].RemoveServer();
            }
            if (unassignedPeers.ContainsKey(server)) unassignedPeers.Remove(server);
        }

        private void AssignServers() {
            if (!ServersAvailable()) return;
            foreach (NetZone t in zones) {
                if (t.Assigned) continue;

                NetConnection server = FirstAvailableServer();
                unassignedPeers[server] = true;
                StartCoroutine(SetServer(server, t));

                if (!ServersAvailable()) return;
            }
        }

        private bool ServersAvailable() {
            return unassignedPeers.ContainsValue(false);
        }

        private NetConnection FirstAvailableServer() {
            foreach (KeyValuePair<NetConnection, bool> kvp in unassignedPeers) {
                if (kvp.Value == false) return kvp.Key;
            }
            throw new Exception("Cannot get first available server. Check for available first.");
        }

        internal void ZoneAssigned(NetZone zone) {
            if (unassignedPeers.ContainsKey(zone.Server)) unassignedPeers.Remove(zone.Server);
            for (int i = 0; i < zones.Count; i++) {
                NetZone peerZone = zones[i];
                if (zone == peerZone || peerZone.Server == null) continue;
                if (peerZone.Server == Socket.Self) GetComponent<NetZoneServer>().AddPeerSelf(zone);
                if (zone.Server != Socket.Self) Socket.Send("AddPeer", zone.Server, peerZone);
                if (peerZone.Server != Socket.Self) Socket.Send("AddPeer", peerZone.Server, zone);
            }
        }

        private bool CanAllocateIdBlock() {
            return viewAllocationPosition + ViewIdBlockSize <= MaxViewId;
        }

        private int GetViewIdMin() {
            return viewAllocationPosition;
        }

        private int GetViewIdMax() {
            viewAllocationPosition += ViewIdBlockSize;
            return viewAllocationPosition - 1;
        }

        public IEnumerator SetServer(NetConnection server, NetZone zone) {

            zone.Assigned = true;

            if (server != Socket.Self) {
                var setServerRequest = Socket.Request.Send<string>("AssignZone", server, zone);

                yield return setServerRequest.WaitUntilDone;

                if (!setServerRequest.IsSuccessful) {
                    zone.Assigned = false;
                    yield break;
                }

                zone.PublicEndpoint = setServerRequest.Result;
            } else {
                NetZoneServer zoneServ = GetComponent<NetZoneServer>();
                zoneServ.AssignZoneSelf(zone);
            }

            zone.Server = server;
            zone.ServerEndpoint = zone.Server.Endpoint;
            if (string.IsNullOrEmpty(zone.PublicEndpoint)) zone.PublicEndpoint = zone.Server.Endpoint.ToString();
            ZoneAssigned(zone);
            NetLog.Info("Server assigned to zone. Endpoint: " + server.Endpoint);
            zone.Available = true;
        }


    }
}