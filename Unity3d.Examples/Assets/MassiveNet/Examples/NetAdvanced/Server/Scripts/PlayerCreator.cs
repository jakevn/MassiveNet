// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System.Collections.Generic;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {
    public class PlayerCreator : MonoBehaviour {

        public NetView View { get; private set; }
        private Inventory inventory;

        public string PlayerName { get; private set; }

        public static readonly Dictionary<string, PlayerCreator> Players = new Dictionary<string, PlayerCreator>();

        private const int MaxHp = 100;
        private int currentHp = 100;
        public bool Dead { get { return currentHp <= 0; } }

        void Awake() {
            View = GetComponent<NetView>();
            inventory = GetComponent<Inventory>();

            inventory.OnItemAdded += ItemAdded;
            inventory.OnItemRemoved += ItemRemoved;

            View.OnWriteSync += WriteSync;
            View.OnReadSync += ReadSync;

            // A different method is often used for different instantiate level
            // For example, Owner might need to know what's in their inventory
            // but Proxy shouldn't know that information, so OnWriteOwnerData
            // and OnWriteProxy data would be different. In this case, however,
            // we only write position, so the same method is used for all.
            //
            // Omitting a handler for any OnWrite___Data event will mean a View
            // will never be instantiated in that manner. For example, if we exempt
            // OnWriteProxyData, the View will never be instantiated as a Proxy.
            View.OnWriteOwnerData += WriteOwnerData;
            View.OnWriteProxyData += WriteInstantiateData;
            View.OnWritePeerData += WriteInstantiateData;
            View.OnWriteCreatorData += WriteOwnerData;

            View.OnReadInstantiateData += ReadInstantiateData;
        }

        void OnDestroy() {
            if (Players.ContainsKey(PlayerName)) Players.Remove(PlayerName);
        }

        private Vector3 lastPos = Vector3.zero;
        //private Quaternion lastRot = Quaternion.identity;
        private Vector2 lastVel = Vector3.zero;

        RpcTarget WriteSync(NetStream syncStream) {
            // If we don't want to sync for this frame, return RpcTarget.None
            // If lastPos == Vector3.zero, position change has already been synced.
            if (lastPos == Vector3.zero) return RpcTarget.None;

            syncStream.WriteFloat(transform.position.x);
            syncStream.WriteFloat(transform.position.z);
            syncStream.WriteVector2(lastVel);

            lastPos = Vector3.zero;

            // We return the RpcTarget for who we want to send the sync info to. Since we receive
            // the position, rotation, and velocity from the PlayerOwner, we want to send to everyone
            // BUT the PlayerOwner, so we choose to return RpcTarget.NonControllers.
            return RpcTarget.NonControllers;
        }


        void ItemAdded(IInvItem item) {
            var equipItem = item as IEquipItem;
            if (equipItem != null) {
                if (equipItem.Equipped) {
                    // TODO: Equipstuff
                    View.SendReliable("ReceiveAdd", RpcTarget.NonControllers, item);
                }
            }
            View.SendReliable("ReceiveAdd", RpcTarget.Controllers, item);
        }

        void ItemRemoved(IInvItem item) {
            var equipItem = item as IEquipItem;
            if (equipItem != null) {
                if (equipItem.Equipped) View.SendReliable("ReceiveRemove", RpcTarget.NonControllers, item);
            }
            View.SendReliable("ReceiveRemove", RpcTarget.Controllers, item);
        }

        void ReadSync(NetStream syncStream) {
            Vector3 position = syncStream.ReadVector3();
            Quaternion rotation = syncStream.ReadQuaternion();
            Vector2 velocity = syncStream.ReadVector2();
            lastPos = position;
            //lastRot = rotation;
            lastVel = velocity;
            transform.position = position;
            transform.rotation = rotation;
        }

        [NetRPC]
        void PickupItem(int pickupViewId) {
            if (inventory.Count() >= inventory.MaxCount()) return;
            PickupCreator creator;
            if (PickupSpawner.Instance.TryGetByViewId(pickupViewId, out creator)) {
                inventory.TryAdd(creator.Unset());
            }
        }

        [NetRPC]
        void StartJump() {
            View.SendReliable("Jump", RpcTarget.NonControllers);
        }

        void WriteInstantiateData(NetStream stream) {
            stream.WriteBool(Dead);
            stream.WriteString(PlayerName);
            stream.WriteVector3(transform.position);
            inventory.WriteMatchesToStream<MeleeWeapon>(stream, (byte)MeleeWeapon.Flag.Equipped);
        }

        void WriteOwnerData(NetStream stream) {
            stream.WriteString(PlayerName);
            stream.WriteInt(currentHp);
            stream.WriteVector3(transform.position);
            inventory.WriteAllToStream(stream);
        }

        void ReadInstantiateData(NetStream stream) {
            PlayerName = stream.ReadString();
            currentHp = stream.ReadInt();
            transform.position = stream.ReadVector3();
            inventory.SetAllFromStream(stream);
            if (!Players.ContainsKey(PlayerName)) Players.Add(PlayerName, this);
        }
    }
}
