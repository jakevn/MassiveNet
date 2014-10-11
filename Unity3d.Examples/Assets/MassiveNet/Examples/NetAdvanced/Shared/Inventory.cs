using System.Collections.Generic;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class Inventory : MonoBehaviour {

        private static readonly List<IInvItem> Database = new List<IInvItem>();

        public static void DbAddNewDefinition<T>(T item) where T : IInvItem {
            if (Database.Contains(item)) return;
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i].Name == item.Name || Database[i].DbId == item.DbId) return;
            }
             Database.Add(item);
        }

        public static bool DbHasDefinitionFor<T>(string withName) where T : IInvItem {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i].Name == withName) return true;
            }
            return false;
        }

        public static bool DbHasDefinitionFor<T>(uint withDbId) where T : IInvItem {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i].DbId == withDbId) return true;
            }
            return false;
        }

        public static bool TryCloneFromDb<T>(string withName, int quantity, out T clonedItem) where T : IInvItem {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i] is T && Database[i].Name == withName && Database[i].QuantityMax >= quantity) {
                    clonedItem = (T)Database[i].Clone(quantity);
                    return true;
                }
            }
            clonedItem = default(T);
            return false;
        }

        public static bool TryCloneFromDb<T>(uint withDbId, int quantity, out T clonedItem) where T : IInvItem {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i] is T && Database[i].DbId == withDbId && Database[i].QuantityMax >= quantity) {
                    clonedItem = (T)Database[i].Clone(quantity);
                    return true;
                }
            }
            clonedItem = default(T);
            return false;
        }

        public static bool TryCloneFromDb(uint withDbId, int quantity, out IInvItem clonedItem) {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i].DbId == withDbId && Database[i].QuantityMax >= quantity) {
                    clonedItem = Database[i].Clone(quantity);
                    return true;
                }
            }
            clonedItem = default(IInvItem);
            return false;
        }

        public static bool TryCloneFromDb(uint withDbId, int quantity, NetStream stream, out IInvItem clonedItem) {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i].DbId == withDbId && Database[i].QuantityMax >= quantity) {
                    clonedItem = Database[i].Clone(quantity, stream);
                    return true;
                }
            }
            clonedItem = default(IInvItem);
            return false;
        }

        public static IInvItem DbCloneRandom<T>() where T : IInvItem {
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i] is T) {
                    return Database[i].Clone(Database[i].Quantity);
                }
            }
            return default(IInvItem);
        }

        private const int maxCount = 32;

        private readonly List<IInvItem> inv = new List<IInvItem>();

        public delegate void ItemAdded(IInvItem item);

        public event ItemAdded OnItemAdded;

        public delegate void ItemRemoved(IInvItem item);

        public event ItemRemoved OnItemRemoved;

        public delegate void InventoryCleared();

        public event InventoryCleared OnInventoryCleared;

        /// <summary>
        /// Attempts to create an item of the given type, name, and quantity and add it to inventory. 
        /// Fails if the given type/name combination does not exist in the item database or inventory is full.
        /// </summary>
        public bool CreateAndAdd<T>(string withName, int quantity) where T : IInvItem {
            T newItem;
            return TryCloneFromDb(withName, quantity, out newItem) && TryAdd(newItem);
        }

        /// <summary>
        /// Attempts to create an item of the given type, id, and quantity and add it to inventory. 
        /// Fails if the given type/id combination does not exist in the item database or inventory is full.
        /// </summary>
        public bool CreateAndAdd<T>(uint withDbId, int quantity) where T : IInvItem {
            T newItem;
            return TryCloneFromDb(withDbId, quantity, out newItem) && TryAdd(newItem);
        }

        /// <summary>
        /// Attempts to create an item of the given type, id, and quantity and add it to inventory. 
        /// Fails if the given type/id combination does not exist in the item database or inventory is full.
        /// </summary>
        private void CreateAndAdd(uint withDbId, int quantity) {     
            for (int i = Database.Count - 1; i >= 0; i--) {
                if (Database[i].DbId == withDbId && Database[i].QuantityMax >= quantity) {
                    TryAdd(Database[i].Clone(quantity));
                }
            }
        }

        [NetRPC]
        private void ReceiveAdd(IInvItem item, NetConnection connection) {
            if (!connection.IsServer && !connection.IsPeer) return;
            TryAdd(item);
        }

        [NetRPC]
        private void ReceiveRemove(IInvItem item, NetConnection connection) {
            if (!connection.IsServer && !connection.IsPeer) return;
            TryRemove(item.DbId, item.Quantity);
        }

        [NetRPC]
        private void ReceiveSetAllFromStream(NetStream stream, NetConnection connection) {
            if (!connection.IsServer && !connection.IsPeer) return;
            SetAllFromStream(stream);
        }

        /// <summary>
        /// Resets the inventory and re-populates it with item data read from
        /// the provided stream.
        /// </summary>
        public void SetAllFromStream(NetStream stream) {
            if (inv.Count != 0) inv.Clear();
            int count = stream.ReadInt();
            for (int i = 0; i < count; i++) {
                IInvItem item;
                TryCloneFromDb(stream.ReadUInt(), stream.ReadInt(), stream, out item);
                TryAdd(item);
            }
        }

        public void WriteAllToStream(NetStream stream) {
            stream.WriteInt(inv.Count);
            for (int i = 0; i < inv.Count; i++) {
                stream.WriteUInt(inv[i].DbId);
                stream.WriteInt(inv[i].Quantity);
                inv[i].WriteAdditional(stream);
            }
        }

        public void WriteMatchesToStream<T>(NetStream stream, byte flag) where T : IInvItem, IFlagChecker {
            stream.WriteInt(0);
            int origPos = stream.Position;
            int count = 0;
            for (int i = 0; i < inv.Count; i++) {
                if (inv[i] is T) {
                    IFlagChecker checker = inv[i] as IFlagChecker;
                    if (!checker.FlagsEqual(flag)) continue;
                    count++;
                    stream.WriteUInt(inv[i].DbId);
                    stream.WriteInt(inv[i].Quantity);
                    inv[i].WriteAdditional(stream);
                }
            }
            if (count == 0) return;
            int pos = stream.Position;
            stream.Position = origPos;
            stream.WriteInt(count);
            stream.Position = pos;
        }

        /// <summary>
        /// Adds the provided item to the inventory.
        /// Fails and returns false if inventory full or already contains item.
        /// Fires OnItemAdded.
        /// </summary>
        public bool TryAdd<T>(T item) where T : IInvItem {
            if (Full() || inv.Contains(item)) return false;
            inv.Add(item);
            if (OnItemAdded != null) OnItemAdded(item);
            return true;
        }

        /// <summary> When true, inventory contains at least one of the given item type. </summary>
        public bool Has<T>() where T : IInvItem {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T) return true;
            }
            return false;
        }

        /// <summary> When true, inventory contains at least one of the given item type and Name. </summary>
        public bool Has<T>(string withName) where T : IInvItem {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == withName) return true;
            }
            return false;
        }

        /// <summary> When true, inventory contains at least one of the given item Name. </summary>
        public bool Has(string withName) {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i].Name == withName) return true;
            }
            return false;
        }

        /// <summary> Returns true and assigns first instance of given type to the out parameter, false otherwise. </summary>
        public bool TryGet<T>(out T found) where T : IInvItem {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T) {
                    found = (T)inv[i];
                    RemoveAtIndex(i);
                    return true;
                }
            }
            found = default(T);
            return false;
        }

        /// <summary> Returns true and assigns first instance of given type and name to the out parameter, false otherwise. </summary>
        public bool TryGet<T>(string findName, out T found) where T : IInvItem {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == findName) {
                    found = (T)inv[i];
                    RemoveAtIndex(i);
                    return true;
                }
            }
            found = default(T);
            return false;
        }

        /// <summary> Returns true and assigns first instance of given name to the out parameter, false otherwise. </summary>
        public bool TryGet(string findName, out IInvItem found) {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i].Name == findName) {
                    found = inv[i];
                    RemoveAtIndex(i);
                    return true;
                }
            }
            found = default(IInvItem);
            return false;
        }

        /// <summary> Returns true if successfully removed the first occurence of the given item type. Fires OnItemRemoved. </summary>
        public bool TryRemoveFirst<T>() where T : IInvItem {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T) {
                    RemoveAtIndex(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if successfully removed the first occurence of the given item type and name.
        /// Fires OnItemRemoved.
        /// </summary>
        public bool TryRemoveFirst<T>(string withName) where T : IInvItem {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == withName) {
                    RemoveAtIndex(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if successfully removed the first occurence of the given item name.
        /// Fires OnItemRemoved.
        /// </summary>
        public bool TryRemoveFirst(string withName) {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i].Name == withName) {
                    RemoveAtIndex(i);
                    return true;
                }
            }
            return false;
        }

        public bool TryRemove(uint dbId, int quantity) {
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i].DbId == dbId && inv[i].Quantity == quantity) {
                    RemoveAtIndex(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes all of the given item type from the inventory and returns the count of items removed.
        /// Fires OnItemRemoved for each.
        /// </summary>
        public int RemoveAll<T>() where T : IInvItem {
            int removed = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T) {
                    RemoveAtIndex(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary> Removes all items from the inventory and fires OnInventoryCleared. </summary>
        public void Clear() {
            inv.Clear();
            if (OnInventoryCleared != null) OnInventoryCleared();
        }

        /// <summary> Returns the count of all occurences of the given type in the inventory. </summary>
        public int CountOf<T>() where T : IInvItem {
            int count = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T) count++;
            }
            return count;
        }

        /// <summary> Returns the count of all occurences of the given type and name in the inventory. </summary>
        public int CountOf<T>(string withName) where T : IInvItem {
            int count = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == withName) count++;
            }
            return count;
        }

        /// <summary> Returns the count of all occurences of the given name in the inventory. </summary>
        public int CountOf(string withName) {
            int count = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i].Name == withName) count++;
            }
            return count;
        }

        /// <summary> Count of all items in inventory. </summary>
        public int Count() {
            return inv.Count;
        }

        /// <summary> The maximum size of the inventory. </summary>
        public int MaxCount() {
            return maxCount;
        }

        /// <summary> True if the inventory count is equal to or greater than the max count. </summary>
        public bool Full() {
            return inv.Count >= maxCount;
        }

        /// <summary> Returns the sum of all Quantity properties for the given item type. </summary>
        public int QuantityOf<T>() where T : IInvItem {
            int quantity = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T) quantity += inv[i].Quantity;
            }
            return quantity;
        }

        /// <summary> Returns the sum of all Quantity properties for the given item type and name. </summary>
        public int QuantityOf<T>(string withName) where T : IInvItem {
            int quantity = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == withName) quantity += inv[i].Quantity;
            }
            return quantity;
        }

        /// <summary> Returns the sum of all Quantity properties for the given name. </summary>
        public int QuantityOf(string withName) {
            int quantity = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i].Name == withName) quantity += inv[i].Quantity;
            }
            return quantity;
        }

        /// <summary> Returns the sum of all Quantity properties for all inventory items. </summary>
        public int TotalQuantity() {
            int quantity = 0;
            for (int i = inv.Count - 1; i >= 0; i--) {
                quantity += inv[i].Quantity;
            }
            return quantity;
        }

        /// <summary> Returns the lowest value Quantity for a given item type and name.  </summary>
        private int LowestQuantity<T>(string withName) where T : IInvItem {
            int i = LowestQuantityIndex<T>(withName);
            return i == -1 ? 0 : inv[i].Quantity;
        }

        private int IncrementAtIndex<T>(int amount, int index) where T : IInvItem {
            if (amount <= 0) return amount;
            IInvItem item = inv[index];
            RemoveAtIndex(index);
            if (amount > item.QuantityMax - item.Quantity) {
                amount -= item.QuantityMax - item.Quantity;
                item.Quantity = item.QuantityMax;    
            } else {
                item.Quantity += amount;
                amount = 0;
            }
            TryAdd(item);
            return amount; // Remainder, if any
        }

        private int DecrementAtIndex<T>(int amount, int index) where T : IInvItem {
            if (amount <= 0) return amount;
            IInvItem item = inv[index];
            RemoveAtIndex(index);
            if (amount - item.Quantity >= 0) {
                amount -= item.Quantity;
            } else {
                item.Quantity -= amount;
                amount = 0;
                TryAdd(item);
            }
            return amount; // Remainder, if any
        }

        private int LowestQuantityIndex<T>(string withName) where T : IInvItem {
            int lowest = -1;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == withName) {
                    if (lowest == -1) lowest = i;
                    else if (inv[i].Quantity < inv[lowest].Quantity) lowest = i;
                }
            }
            return lowest; // Will be -1 if none found
        }

        private int HighestNonMaxQuantityIndex<T>(string withName) where T : IInvItem {
            int highest = -1;
            for (int i = inv.Count - 1; i >= 0; i--) {
                if (inv[i] is T && inv[i].Name == withName && inv[i].Quantity < inv[i].QuantityMax) {
                    if (highest == -1) highest = i;
                    else if (inv[i].Quantity > inv[highest].Quantity) highest = i;
                }
            }
            return highest; // Will be -1 if none found
        }

        private void RemoveAtIndex(int index) {
            IInvItem item = inv[index];
            inv.RemoveAt(index);
            if (OnItemRemoved != null) OnItemRemoved(item);
        }

        /// <summary> Removes the supplied amount from Quantity of provided item type and name. Removes items when Quantity is reduced to 0.
        /// Will remove Quantity from multiple items if needed. Returns the remainder (amount that couldn't be removed), if any. Decrements
        /// Quantity in order of ascending Quantity. </summary>
        public int DecrementAmount<T>(int amount, string withName) where T : IInvItem {
            while (amount > 0) {
                int lowestIndex = LowestQuantityIndex<T>(withName);
                if (lowestIndex == -1) break;
                int prev = amount;
                amount = DecrementAtIndex<T>(amount, lowestIndex);
                if (prev == amount) break;
            }
            return amount; // Remainder, if any
        }

        /// <summary> Increases the Quantity of supplied item type and name by given amount. Will increase an item's quantity up to its max
        /// quantity, and if any amount remains, it will do the same to additional items until amount is exhausted. Returns the remainder
        /// (unapplied amount), if any. Increments Quantity in order of descending Quantity. </summary>
        public int IncrementAmount<T>(int amount, string withName) where T : IInvItem {
            while (amount > 0) {
                int highestIndex = HighestNonMaxQuantityIndex<T>(withName);
                if (highestIndex == -1) break;
                int prev = amount;
                amount = IncrementAtIndex<T>(amount, highestIndex);
                if (prev == amount) break;
            }
            return amount; // Remainder, if any
        }

    }

}