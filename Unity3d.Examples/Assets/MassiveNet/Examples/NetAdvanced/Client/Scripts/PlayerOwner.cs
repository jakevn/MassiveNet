// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class PlayerOwner : MonoBehaviour {

        private NetView view;

        private Inventory inventory;
        private EquipmentRenderer equipRenderer;
        private WeaponAnimator weaponAnimator;
        private CatAnimator catAnimator;

        public string PlayerName { get; private set; }
        public int Hp { get; private set; }

        void Awake() {
            view = GetComponent<NetView>();
            inventory = GetComponent<Inventory>();
            catAnimator = GetComponentInChildren<CatAnimator>();

            view.OnReadInstantiateData += Instantiate;

            InputHandler.Instance.ListenToKeyDown(TryPickup, KeyBind.Code(Bind.Interact));
        }

        [NetRPC]
        void Die() {
            catAnimator.Dead = true;
        }

        void TryPickup() {
            PickupProxy[] pickups = FindObjectsOfType<PickupProxy>();
            foreach (var pickup in pickups) {
                if (Vector3.Distance(pickup.transform.position, transform.position) > 30) continue;
                view.SendReliable("PickupItem", RpcTarget.Server, pickup.GetViewId());
                break;
            }
        }

        void Instantiate(NetStream stream) {
            PlayerName = stream.ReadString();
            Hp = stream.ReadInt();
            Vector3 pos = stream.ReadVector3();
            inventory.SetAllFromStream(stream);
            // Prevemt jumpiness during handoff by ignoring position data if similar enough:
            if (transform.position != Vector3.zero && Vector3.Distance(transform.position, pos) < 5) return;
            transform.position = pos;
        }

    }

}
