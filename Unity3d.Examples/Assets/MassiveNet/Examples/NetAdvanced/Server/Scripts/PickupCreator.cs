// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class PickupCreator : MonoBehaviour {

        private NetView view;
        private IInvItem item;

        private bool set;

        private void Awake() {
            view = GetComponent<NetView>();

            view.Scope.DisableScopeCalculation();
            view.OnWriteProxyData += WriteInstantiateData;
        }

        public void Set(IInvItem newItem, Vector3 position) {
            view.Scope.EnableScopeCalculation();
            transform.position = position;
            item = newItem;
            set = true;
            view.SendReliable("Set", RpcTarget.NonControllers, newItem, position);
        }

        public int GetViewId() {
            return view.Id;
        }

        public IInvItem Unset() {
            IInvItem itm = item;
            item = null;
            set = false;
            view.SendReliable("Unset", RpcTarget.NonControllers);
            view.Scope.DisableScopeCalculation();
            PickupSpawner.Instance.PickupUnset(this);
            return itm;
        }

        private void WriteInstantiateData(NetStream stream) {
            stream.WriteBool(set);
            if (!set) return;
            stream.WriteVector3(transform.position);
            ExampleItems.ItemSerializer(stream, item);
        }

    }
}