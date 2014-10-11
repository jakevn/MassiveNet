// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {
    public class PlayerPeer : MonoBehaviour {

        private NetView view;

        void Die() {
            
        }

        private void Awake() {
            view = GetComponent<NetView>();
            view.OnReadInstantiateData += ReadInstantiateData;
            view.OnReadSync += ReadSync;
        }

        private void ReadSync(NetStream stream) {
            transform.position = stream.ReadVector3();
        }

        private void ReadInstantiateData(NetStream stream) {
            transform.position = stream.ReadVector3();
        }
    }
}
