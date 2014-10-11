// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using UnityEngine;
using Massive.Examples.NetAdvanced;
using MassiveNet;

public class ExampleItems {

    public static void PopulateItemDatabase() {
        uint dbId = 1;
        Inventory.DbAddNewDefinition(new MeleeWeapon(dbId++, "Sword", 20, 20, 15, 1.5f, true));
        Inventory.DbAddNewDefinition(new MeleeWeapon(dbId++, "Hammer", 30, 30, 12, 1.5f, true));
        Inventory.DbAddNewDefinition(new Armor(dbId++, "Helmet", 30, 30, 5, "HeadMount", false));
        Inventory.DbAddNewDefinition(new Armor(dbId, "Chestplate", 30, 30, 12, "ChestMount", false));
        
        NetSerializer.Add<MeleeWeapon>(ItemSerializer, ItemDeserializer);
        NetSerializer.Add<IInvItem>(ItemSerializer, ItemDeserializer);
    }

    public static void ItemSerializer(NetStream stream, object instance) {
        IInvItem item = (IInvItem) instance;
        stream.WriteUInt(item.DbId);
        stream.WriteInt(item.Quantity);
        item.WriteAdditional(stream);
    }

    public static object ItemDeserializer(NetStream stream) {
        uint dbId = stream.ReadUInt();
        int quantity = stream.ReadInt();
        IInvItem item;
        if (!Inventory.TryCloneFromDb(dbId, quantity, stream, out item)) {
            Debug.LogError("Failed to deserialize IInvItem. Item with given ID not found in database: " + dbId);
        }
        return item;
    }

}


public class MeleeWeapon : IInvItem, IEquipItem, IFlagChecker {

    [Flags]
    public enum Flag : byte {
        None = 0x0,
        Equipped = 0x1
    }

    public MeleeWeapon(uint dbId, string name, int quantityMax, int quantity, int damage, float cooldown, bool equipped) {
        DbId = dbId;
        Name = name;
        QuantityMax = quantityMax;
        Quantity = quantity;

        Damage = damage;
        Cooldown = cooldown;

        Equipped = equipped;
        MountPoint = "WeaponMount";
    }

    public int Damage { get; private set; }
    public float Cooldown { get; private set; }

    public uint DbId { get; private set; }
    public string Name { get; private set; }
    public int QuantityMax { get; private set; }
    public int Quantity { get; set; }

    public IInvItem Clone(int withQuantity) {
        return new MeleeWeapon(DbId, Name, QuantityMax, withQuantity, Damage, Cooldown, Equipped);
    }

    public IInvItem Clone(int withQuantity, NetStream stream) {
        return new MeleeWeapon(DbId, Name, QuantityMax, withQuantity, Damage, Cooldown, stream.ReadBool());
    }

    public void WriteAdditional(NetStream stream) {
        stream.WriteBool(Equipped);
    }

    public bool Equipped { get; set; }

    public string MountPoint { get; set; }

    public bool FlagsEqual(byte flag) {
        Flag flags = Flag.None;
        if (Equipped) flags |= Flag.Equipped;
        return (byte) flags == flag;
    }
}


public class Armor : IInvItem, IEquipItem {

    [Flags]
    public enum Flag : byte {
        None = 0x0,
        Equipped = 0x1
    }


    public Armor(uint dbId, string name, int quantityMax, int quantity, int protection, string mountPoint, bool equipped) {
        DbId = dbId;
        Name = name;
        QuantityMax = quantityMax;
        Quantity = quantity;

        Protection = protection;

        MountPoint = mountPoint;
        Equipped = equipped;
    }

    public int Protection { get; private set; }

    public uint DbId { get; private set; }
    public string Name { get; private set; }
    public int QuantityMax { get; private set; }
    public int Quantity { get; set; }

    public IInvItem Clone(int withQuantity) {
        return new Armor(DbId, Name, QuantityMax, withQuantity, Protection, MountPoint, Equipped);
    }

    public IInvItem Clone(int withQuantity, NetStream stream) {
        return new Armor(DbId, Name, QuantityMax, withQuantity, Protection, MountPoint, stream.ReadBool());
    }

    public void WriteAdditional(NetStream stream) {
        stream.WriteBool(Equipped);
    }

    public bool Equipped { get; set; }
    public string MountPoint { get; set; }

    public bool FlagsEqual(byte flag) {
        Flag flags = Flag.None;
        if (Equipped) flags |= Flag.Equipped;
        return (byte)flags == flag;
    }
}