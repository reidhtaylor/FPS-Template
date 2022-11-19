using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GUN_ROUND_TYPE { Energy, Shell, Thunder }

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;
    private void Awake() {
        instance = this;
    }

    // Inventory
    [Header("Inventory")]
    public Transform inventoryHolder;
    
    public static void AddItem(Item i) {
        i.transform.SetParent(instance.inventoryHolder);
        i.transform.localPosition = Vector3.zero;
        i.transform.localEulerAngles = Vector3.zero;
        i.OnAddedToInventory();
    }
    public static void RemoveItem(Item i) {
        i.transform.SetParent(null);
        i.OnRemovedToInventory();
    }
    // Inventory

    // Weapons
    [Header("Weapons")]
    public List<Weapon> weapons = new List<Weapon>();

    public static bool HasWeapon(Weapon i) {
        return instance.weapons.Contains(i);
    }
    // Weapons

    // Rounds
    [Header("Rounds")]
    public int energyRounds = 0;
    public int shellRounds = 0;
    public int thunderRounds = 0;

    public static int CountRounds(GUN_ROUND_TYPE t) {
        switch (t) {
            case GUN_ROUND_TYPE.Energy: return instance.energyRounds;
            case GUN_ROUND_TYPE.Shell: return instance.shellRounds;
            case GUN_ROUND_TYPE.Thunder: return instance.thunderRounds;
            default: return 0;
        }
    }
    public static bool HasRounds(GUN_ROUND_TYPE t, int a) {
        return CountRounds(t) >= a;;
    }
    public static void UseRounds(GUN_ROUND_TYPE t, int a) {
        switch (t) {
            case GUN_ROUND_TYPE.Energy: instance.energyRounds -= a; break;
            case GUN_ROUND_TYPE.Shell: instance.shellRounds -= a; break;
            case GUN_ROUND_TYPE.Thunder: instance.thunderRounds -= a; break;
        }
        UIHandler.instance.RefreshWeaponPanel();
    }
    public static void AddRounds(GUN_ROUND_TYPE t, int a) => UseRounds(t, -a);
    // Rounds
}
