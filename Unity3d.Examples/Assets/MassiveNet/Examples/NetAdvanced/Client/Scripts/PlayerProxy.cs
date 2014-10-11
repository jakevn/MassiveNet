using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class PlayerProxy : MonoBehaviour {

        private NetView view;
        private CatAnimator catAnimator;
        private Inventory inventory;

        public string PlayerName { get; private set; }

        void Awake() {
            view = GetComponent<NetView>();
            catAnimator = GetComponentInChildren<CatAnimator>();
            inventory = GetComponent<Inventory>();

            // Note: Always register OnReadInstantiateData delegate in Awake
            // OnReadInstantiateData is called immediately after a View is created, so registering
            // in Start instead of Awake means you might miss out on the instantiate data.
            view.OnReadInstantiateData += Instantiate;
        }

        void Instantiate(NetStream stream) {
            catAnimator.Dead = stream.ReadBool();
            PlayerName = stream.ReadString();
            transform.position = stream.ReadVector3();
            inventory.SetAllFromStream(stream);
        }

    }

}
