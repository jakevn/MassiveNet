// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Provides facilities for clients to perform necessary setup in a Zone-configured environment.
    /// Listens for commands to connect to other Zones and signals successful connections or failures.
    /// </summary>
    public class NetZoneClient : MonoBehaviour {

        private NetSocket socket;
 
        private List<NetConnection> pendingConn = new List<NetConnection>();
        private List<IPEndPoint> pendingEp = new List<IPEndPoint>();
        private Dictionary<NetConnection, int> pendingSetup = new Dictionary<NetConnection, int>();

        public delegate void ZoneSetupSuccess(NetConnection server);

        /// <summary> Called when succesfully connected to all required endpoints. </summary>
        public event ZoneSetupSuccess OnZoneSetupSuccess;

        public delegate void ZoneSetupFailed(NetConnection server);

        /// <summary> Called if failed to connect to a required endpoint. </summary>
        public event ZoneSetupFailed OnZoneSetupFailed;

        void Awake() {
            socket = GetComponent<NetSocket>();

            socket.RegisterRpcListener(this);

            socket.Events.OnFailedToConnect += ConnectionFailed;
            socket.Events.OnConnectedToServer += ConnectedToZone;
        }

        void ConnectedToZone(NetConnection connection) {
            RemoveEndpoint(connection.Endpoint);
        }

        void RemoveEndpoint(IPEndPoint ep) {
            while (pendingEp.Contains(ep)) {
                int i = pendingEp.IndexOf(ep);
                NetConnection server = pendingConn[i];
                pendingConn.RemoveAt(i);
                pendingEp.RemoveAt(i);
                DecrementPending(server);
            }
        }

        void DecrementPending(NetConnection conn) {
            pendingSetup[conn]--;
            if (pendingSetup[conn] > 0) return;
            pendingSetup.Remove(conn);
            NotifySuccess(conn);
        }

        void ConnectionFailed(IPEndPoint ep) {
            if (pendingEp.Contains(ep)) {
                NotifyFailure(ep);
            }
        }

        void NotifyFailure(IPEndPoint ep) {
            int i = pendingEp.IndexOf(ep);
            NetConnection server = pendingConn[i];
            socket.Send("ZoneConnectFail", server, ep);
            if (OnZoneSetupFailed != null) OnZoneSetupFailed(server);
        }

        void NotifySuccess(NetConnection server) {
            socket.Send("ZoneConnectSuccess", server);
            if (OnZoneSetupSuccess != null) OnZoneSetupSuccess(server);
        }

        [NetRPC]
        void ConnectToZone(int count, IPEndPoint ep, NetConnection conn) {
            if (!conn.IsServer) return;
            if (!pendingSetup.ContainsKey(conn)) pendingSetup.Add(conn, count);
            if (socket.EndpointConnected(ep)) DecrementPending(conn);
            else {
                pendingConn.Add(conn);
                pendingEp.Add(ep);
                if (!socket.ConnectingTo(ep)) socket.Connect(ep);
            }
        }

    }
}
