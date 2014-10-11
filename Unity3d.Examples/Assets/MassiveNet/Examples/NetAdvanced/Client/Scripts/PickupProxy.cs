// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class PickupProxy : MonoBehaviour {

        private NetView view;
        private IInvItem item;

        private GameObject mesh;

        private void Awake() {
            view = GetComponent<NetView>();
            view.OnReadInstantiateData += ReadInstantiateData;
        }

        [NetRPC]
        private void Set(IInvItem newItem, Vector3 position) {
            transform.position = position;
            item = newItem;
            SetMesh();
        }

        public int GetViewId() {
            return view.Id;
        }

        private void SetMesh() {
            mesh = (GameObject)Instantiate(Resources.Load(item.Name), transform.position, Quaternion.identity);
            mesh.transform.parent = transform;
        }

        private void UnsetMesh() {
            Destroy(mesh);
        }

        [NetRPC]
        private void Unset() {
            item = null;
            UnsetMesh();
        }

        private void ReadInstantiateData(NetStream stream) {
            if (!stream.ReadBool()) return;
            transform.position = stream.ReadVector3();
            item = (IInvItem)ExampleItems.ItemDeserializer(stream);
            SetMesh();
        }

    }

}