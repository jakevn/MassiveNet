// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetSimple {
    [RequireComponent(typeof(NetSocket), typeof(NetViewManager))]
    public class ClientModel : MonoBehaviour {

        public string ServerAddress = "127.0.0.1:17000";
        private NetConnection server;

        private NetSocket socket;
        private NetViewManager viewManager;
        private NetZoneClient zoneClient;

        private void Start() {
            socket = GetComponent<NetSocket>();
            viewManager = GetComponent<NetViewManager>();
            zoneClient = GetComponent<NetZoneClient>();

            zoneClient.OnZoneSetupSuccess += ZoneSetupSuccessful;

            socket.Events.OnDisconnectedFromServer += DisconnectedFromServer;
            socket.Events.OnConnectedToServer += ConnectedToServer;

            socket.StartSocket();
            socket.RegisterRpcListener(this);

            socket.Connect(ServerAddress);
        }

        private void ConnectedToServer(NetConnection connection) {
            if (connection.Endpoint.ToString() == ServerAddress) {
                Debug.Log("Finished connection to main server: " + connection.Endpoint);
                server = connection;
            } else {
                Debug.Log("Finished connection to server: " + connection.Endpoint);
            }
        }

        private void ZoneSetupSuccessful(NetConnection zoneServer) {
            if (server == zoneServer) SendSpawnRequest();
        }

        private void SendSpawnRequest() {
            Debug.Log("Sending spawn request RPC to server...");
            socket.Send("SpawnRequest", server);
        }

        private void DisconnectedFromServer(NetConnection serv) {
            viewManager.DestroyViewsServing(serv);
        }
    }
}
