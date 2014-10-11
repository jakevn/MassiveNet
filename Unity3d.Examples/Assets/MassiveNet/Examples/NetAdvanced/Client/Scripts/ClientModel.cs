// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    [RequireComponent(typeof(NetSocket), typeof(NetViewManager))]
    public class ClientModel : MonoBehaviour {
 
        private NetConnection server;

        private NetSocket socket;
        private NetViewManager viewManager;
        private NetZoneClient zoneClient;

        private void Start() {
            socket = GetComponent<NetSocket>();
            viewManager = GetComponent<NetViewManager>();
            zoneClient = GetComponent<NetZoneClient>();

            ExampleItems.PopulateItemDatabase();

            zoneClient.OnZoneSetupSuccess += ZoneSetupSuccessful;

            socket.Events.OnDisconnectedFromServer += DisconnectedFromServer;

            socket.StartSocket();
            socket.RegisterRpcListener(this);
        }

        private void ZoneSetupSuccessful(NetConnection zoneServer) {

        }

        private void DisconnectedFromServer(NetConnection serv) {
            viewManager.DestroyViewsServing(serv);
        }

    }

}
