// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetSimple {
    public class AiCreator : MonoBehaviour {
        private NetView netView;
        private Vector3 target;
        private Vector3 velocity;

        private Vector3 targetRoot;

        void Start() {
            netView = GetComponent<NetView>();
            netView.OnWriteSync += WriteSync;
            netView.OnWriteProxyData += WriteProxyData;
            SetTargetPosition();
        }

        RpcTarget WriteSync(NetStream syncStream) {
            syncStream.WriteVector3(transform.position);
            syncStream.WriteFloat(transform.rotation.eulerAngles.y);
            syncStream.WriteVector2(new Vector2(velocity.x, velocity.z));
            return RpcTarget.NonControllers;
        }

        void Update() {
            Vector3 oldPos = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 4);
            velocity = transform.position - oldPos;
            if (Vector3.Distance(transform.position, target) < 0.3) SetTargetPosition();
        }

        void SetTargetPosition() {
            target = new Vector3(Random.Range(-200, 200) + targetRoot.x, 0, Random.Range(-200, 200) + targetRoot.z);
        }

        public void SetTargetRoot(Vector3 rootPos) {
            targetRoot = rootPos;
        }

        private void WriteProxyData(NetStream stream) {
            stream.WriteVector3(transform.position);
        }
    }
}
