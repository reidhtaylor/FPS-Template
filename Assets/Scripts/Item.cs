using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ITEM_TYPE { 
        Round, 
        AR, Pistol, Shotgun, Sniper, 
        Scope, 
    }

    [Header("Data")]
    public ITEM_TYPE itemType;
    public string itemName;
    public Sprite itemIcon;

    [Header("Other")]
    public bool inUse = false;
    public Collider interactCollider;

    public virtual void Interact() {
        interactCollider.enabled = false;
        inUse = true;
    }
    public virtual bool CanInteract() {
        return true;
    }

    public virtual void OnAddedToInventory() { }
    public virtual void OnRemovedToInventory() { }
}
