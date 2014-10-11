using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentRenderer : MonoBehaviour {

    public List<Transform> MountPoints = new List<Transform>();
 
    private readonly List<Transform> equipmentObjects = new List<Transform>();

    public bool Equip(string equipName, string mountPoint) {
        Transform foundMount;
        return TryGetMount(mountPoint, out foundMount) && TryMountEquipment(foundMount, equipName);
    }

    public bool UnequipByMount(string mountName) {
        return TryDestroyMountChild(mountName);
    }

    public bool UnequipByEquipName(string equipName) {
        return TryDestroyEquipment(equipName);
    }

    bool TryGetMount(string mountPoint, out Transform foundPoint) {
        foreach (Transform trans in MountPoints) {
            if (trans.name != mountPoint) continue;
            foundPoint = trans;
            return true;
        }
        Debug.LogError("Could not find mount point named: " + mountPoint);
        foundPoint = null;
        return false;
    }
    bool TryMountEquipment(Transform parentPoint, string prefabName) {
        var go = (GameObject)Instantiate(Resources.Load(prefabName), transform.position, Quaternion.identity);
        if (go == null) {
            Debug.LogError("Failed to instantiate equipment prefab with name: " + prefabName);
            return false;
        }
        go.transform.rotation = parentPoint.rotation;
        go.transform.position = parentPoint.position;
        go.transform.parent = parentPoint;
        equipmentObjects.Add(go.transform);
        return true;
    }

    bool TryDestroyEquipment(string prefabName) {
        Transform foundEquip = equipmentObjects.FirstOrDefault(trans => trans.name == prefabName);
        if (foundEquip == null) return false;
        equipmentObjects.Remove(foundEquip);
        foundEquip.parent = null;
        Destroy(foundEquip.gameObject);
        return true;
    }

    bool TryDestroyMountChild(string mountName) {
        Transform foundEquip = null;
        foreach (Transform trans in MountPoints) {
            if (trans.name != mountName) continue;
            foreach (Transform equip in equipmentObjects) {
                if (equip.parent != trans) continue;
                foundEquip = equip;
                break;
            }
            break;
        }
        if (foundEquip == null) return false;
        equipmentObjects.Remove(foundEquip);
        foundEquip.parent = null;
        Destroy(foundEquip.gameObject);
        return true;
    }
}
