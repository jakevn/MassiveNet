// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetSimple {
    public class PlayerOwner : MonoBehaviour {

        private NetView view;

        void Awake() {
            view = GetComponent<NetView>();
            view.OnReadInstantiateData += Instantiate;
        }

        void Instantiate(NetStream stream) {
            Vector3 pos = stream.ReadVector3();
            // Prevemt jumpiness during handoff by ignoring position data if similar enough:
            if (transform.position != Vector3.zero && Vector3.Distance(transform.position, pos) < 5) return;
            transform.position = pos;
        }
    }
}
