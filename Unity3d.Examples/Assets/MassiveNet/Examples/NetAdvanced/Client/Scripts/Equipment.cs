// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class Equipment : MonoBehaviour {

        private Inventory inventory;
        private EquipmentRenderer equipRenderer;

        void Awake() {
            inventory = GetComponent<Inventory>();
            equipRenderer = GetComponentInChildren<EquipmentRenderer>();

            inventory.OnItemAdded += ItemAdded;
            inventory.OnItemRemoved += ItemRemoved;
        }

        void ItemAdded(IInvItem item) {
            if (item is IEquipItem) EquipAdded(item);
        }

        void ItemRemoved(IInvItem item) {
            if (item is IEquipItem) EquipRemoved(item);
        }

        void EquipAdded(IInvItem item) {
            var equip = (IEquipItem)item;
            if (equip.Equipped) equipRenderer.Equip(item.Name, equip.MountPoint);
        }

        void EquipRemoved(IInvItem item) {
            var equip = (IEquipItem)item;
            if (equip.Equipped) equipRenderer.UnequipByEquipName(item.Name);
        }
    }
}
