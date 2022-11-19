using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Item
{
    public Animator anim;
    
    [Header("IK")]
    public Transform leftHandIkTarget = null;
    public Transform rightHandIkTarget = null;
    [Space]
    public Transform leftHandIkHint = null;
    public Transform rightHandIkHint = null;

    [Header("Drop Settings")]
    public float dropHeightOffset = 0.1f;

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
    public virtual void OnPickup() { }
    public virtual void OnDrop() {
        interactCollider.enabled = true;
        inUse = false;
    }

    public override void Interact() {
        base.Interact();
        PlayerController.instance.GetComponent<PlayerWeaponHandler>().EquipWeapon(this);
    }
    public override bool CanInteract() { return true; }

    public virtual void Update() { }

    public override void OnAddedToInventory() {
        PlayerInventory.instance.weapons.Add(this);
    }
    public override void OnRemovedToInventory() {
        PlayerInventory.instance.weapons.Remove(this);
    }
}
