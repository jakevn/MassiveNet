using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class AiProxy : MonoBehaviour {

        private NetView view;
        private NetController controller;
        private CatAnimator catAnimator;

        void Awake() {
            view = GetComponent<NetView>();
            controller = GetComponent<NetController>();
            catAnimator = GetComponentInChildren<CatAnimator>();

            // Note: Always register OnReadInstantiateData delegate in Awake
            // OnReadInstantiateData is called immediately after a View is created, so registering
            // in Start instead of Awake means you might miss out on the instantiate data.
            view.OnReadInstantiateData += Instantiate;
        }

        [NetRPC]
        void Die() {
            controller.enabled = false;
            catAnimator.Dead = true;
        }

        void OnEnable() {
            transform.rotation = Quaternion.identity;
        }

        void Instantiate(NetStream stream) {
            if (stream.ReadBool()) Die();
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            float posX = stream.ReadFloat();
            float posZ = stream.ReadFloat();
            transform.position = new Vector3(posX, 0, posZ);
        }

    }

}
