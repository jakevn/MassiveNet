// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetSimple {
    public class PlayerCreator : MonoBehaviour {

        private NetView view;

        void Awake() {
            view = GetComponent<NetView>();

            view.OnWriteSync += WriteSync;
            view.OnReadSync += ReadSync;

            // A different method is often used for different instantiate level
            // For example, Owner might need to know what's in their inventory
            // but Proxy shouldn't know that information, so OnWriteOwnerData
            // and OnWriteProxy data would be different. In this case, however,
            // we only write position, so the same method is used for all.
            //
            // Omitting a handler for any OnWrite___Data event will mean a View
            // will never be instantiated in that manner. For example, if we exempt
            // OnWriteProxyData, the View will never be instantiated as a Proxy.
            view.OnWriteOwnerData += WriteInstantiateData;
            view.OnWriteProxyData += WriteInstantiateData;
            view.OnWritePeerData += WriteInstantiateData;
            view.OnWriteCreatorData += WriteInstantiateData;

            view.OnReadInstantiateData += ReadInstantiateData;
        }

        private Vector3 lastPos = Vector3.zero;
        private Quaternion lastRot = Quaternion.identity;
        private Vector2 lastVel = Vector3.zero;

        RpcTarget WriteSync(NetStream syncStream) {
            // If we don't want to sync for this frame, return RpcTarget.None
            // If lastPos == Vector3.zero, position change has already been synced.
            if (lastPos == Vector3.zero) return RpcTarget.None;

            syncStream.WriteVector3(lastPos);
            syncStream.WriteFloat(lastRot.eulerAngles.y);
            syncStream.WriteVector2(lastVel);

            lastPos = Vector3.zero;

            // We return the RpcTarget for who we want to send the sync info to. Since we receive
            // the position, rotation, and velocity from the PlayerOwner, we want to send to everyone
            // BUT the PlayerOwner, so we choose to return RpcTarget.NonControllers.
            return RpcTarget.NonControllers;
        }

        void ReadSync(NetStream syncStream) {
            Vector3 position = syncStream.ReadVector3();
            Quaternion rotation = syncStream.ReadQuaternion();
            Vector2 velocity = syncStream.ReadVector2();
            lastPos = position;
            lastRot = rotation;
            lastVel = velocity;
            transform.position = position;
            transform.rotation = rotation;
        }

        void WriteInstantiateData(NetStream stream) {
            stream.WriteVector3(transform.position);
        }

        void ReadInstantiateData(NetStream stream) {
            transform.position = stream.ReadVector3();
        }
    }
}
