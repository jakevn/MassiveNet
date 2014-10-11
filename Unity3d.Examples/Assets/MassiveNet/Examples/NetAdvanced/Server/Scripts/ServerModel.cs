// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using System.Net;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    [RequireComponent(typeof(NetSocket), typeof(NetViewManager), typeof(NetScopeManager))]
    public class ServerModel : MonoBehaviour {

        public string ServerAddress = "127.0.0.1";
        public int ServerPortRoot = 17000;

        public List<string> PeerAddresses = new List<string>();

        private NetSocket socket;
        private NetZoneServer zoneServer;
        private NetZoneManager zoneManager;

        private AiManager aiManager;

        private void Start() {
            socket = GetComponent<NetSocket>();
            zoneManager = GetComponent<NetZoneManager>();
            zoneServer = GetComponent<NetZoneServer>();

            aiManager = GetComponent<AiManager>();

            ExampleItems.PopulateItemDatabase();

            zoneServer.OnAssignment += AssignedToZone;

            socket.ProtocolAuthority = true;
            socket.AcceptConnections = true;
            socket.MaxConnections = 512;

            socket.Events.OnPeerApproval += PeerApproval;
            socket.Events.OnSocketStart += SocketStart;
            socket.Events.OnFailedToConnect += FailedToConnect;

            socket.StartSocket(ServerAddress + ":" + ServerPortRoot);
            socket.RegisterRpcListener(this);
        }

        private void SocketStart() {
            if (socket.Port != ServerPortRoot) {
                // If another server is on the same machine, connect to it:
                socket.ConnectToPeer(socket.Address + ":" + (socket.Port - 1));
            } else if (PeerAddresses.Count > 0) {
                // Else, if there are peer addresses defined in PeerAddresses, connect:
                socket.ConnectToPeer(PeerAddresses[0]);
            } else {
                ConfigureZones();
            }
        }

        private void ConfigureZones() {
            zoneManager.Authority = true;
            zoneManager.CreateZone(new Vector3(0, 0, 200));
            zoneManager.CreateZone(new Vector3(0, 0, -200));
            zoneManager.AddSelfAsServer();
        }

        private void FailedToConnect(IPEndPoint endpoint) {
            string epString = endpoint.ToString();
            if (PeerAddresses.Contains(epString)) {
                int index = PeerAddresses.IndexOf(epString);
                if (index + 1 == PeerAddresses.Count) return;
                index++;
                socket.ConnectToPeer(PeerAddresses[index]);
            } else if (socket.Address == endpoint.Address.ToString() && socket.Port - endpoint.Port > 1) {
                if (endpoint.Port == ServerPortRoot) return;
                socket.ConnectToPeer(ServerAddress + ":" + (endpoint.Port - 1));
            } else Debug.LogError("Failed to connect to peer(s).");
        }

        private bool PeerApproval(IPEndPoint endPoint, NetStream data) {
            if (endPoint.Port > ServerPortRoot + 512 || endPoint.Port < ServerPortRoot) return false;
            string address = endPoint.Address.ToString();
            return (address == ServerAddress || PeerAddresses.Contains(address));
        }

        private void AssignedToZone() {
            aiManager.StartSpawning(zoneServer.Position);
            PickupSpawner.Instance.StartSpawning(zoneServer.Position);
        }

    }

}