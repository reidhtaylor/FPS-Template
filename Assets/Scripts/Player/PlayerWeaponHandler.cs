using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerWeaponHandler : MonoBehaviour
{
    public PlayerController player;
    public Transform weaponHolder;
    [Space]
    public Weapon hands;
    [Space]
    public Weapon equippedWeapon = null;

    private float verMagAnim, horMagAnim;

    [Header("IK")]
    public RigBuilder rig;
    public Animator anim;
    public TwoBoneIKConstraint leftHandIk;
    public TwoBoneIKConstraint rightHandIk;
    public Transform defaultLeftHandHint;
    public Transform defaultRightHandHint;

    private Transform leftHandIkTarget;
    private Transform rightHandIkTarget;

    [Header("Drop Weapon")]
    public LayerMask dropLocationLayer;

    [Header("Weapon Holder Control")]
    private Vector3 additiveHolderPos;
    private Vector3 additiveHolderRot;
    public Vector3 holderOriginalPosition { get; set; }
    public Vector3 holderOriginalRotation { get; set; }

    public Dictionary<Weapon, Vector3> holderPositionRecord = new Dictionary<Weapon, Vector3>();
    public Dictionary<Weapon, Vector3> holderRotationRecord = new Dictionary<Weapon, Vector3>();

    private void Start() {
        EquipWeapon(hands);

        holderOriginalPosition = weaponHolder.localPosition;
        holderOriginalRotation = weaponHolder.localEulerAngles;
    }
    private void OnEnable() {
        player.input.onSwitchWeapons += SwitchWeapons;
    }
    private void OnDisable() {
        player.input.onSwitchWeapons -= SwitchWeapons;
    }

    private void Update() {
        if (equippedWeapon != null) {
            if (leftHandIkTarget != null) leftHandIkTarget.position = equippedWeapon.leftHandIkTarget.position;
            if (rightHandIkTarget != null) rightHandIkTarget.position = equippedWeapon.rightHandIkTarget.position;

            verMagAnim = Mathf.Lerp(verMagAnim, player.verticalMagnitude, Time.deltaTime * 2);
            horMagAnim = Mathf.Lerp(horMagAnim, player.horizontalMagnitude, Time.deltaTime * 2);

            equippedWeapon.anim.SetFloat("VerticalMagnitude", verMagAnim);
            equippedWeapon.anim.SetFloat("HorizontalMagnitude", horMagAnim);
            equippedWeapon.anim.SetBool("IsWalking", player.movementState == MovementState.Walking);
            equippedWeapon.anim.SetBool("IsRunning", player.movementState == MovementState.Running);
            equippedWeapon.anim.SetBool("IsSliding", player.movementState == MovementState.Sliding);
        }

        additiveHolderPos = Vector3.zero;
        additiveHolderRot = Vector3.zero;
        foreach(var r in holderPositionRecord.Values) additiveHolderPos += r;
        foreach(var r in holderRotationRecord.Values) additiveHolderRot += r;
        
        weaponHolder.localPosition = holderOriginalPosition + additiveHolderPos;
        weaponHolder.localEulerAngles = holderOriginalRotation + additiveHolderRot;
    }

    public void SwitchWeapons() {
        if (PlayerInventory.instance.weapons.Count > 0) EquipWeapon(PlayerInventory.instance.weapons[0]);
        else UnequipWeapon();
    }
    public void EquipWeapon(Weapon weapon) {
        if (PlayerInventory.HasWeapon(weapon)) {
            // Take weapon out of inventory
            PlayerInventory.RemoveItem(weapon);

            // Put current weapon in inventory
            if (equippedWeapon != null && equippedWeapon != hands) UnequipWeapon();

            // Equip new weapon
            EquipWeapon(weapon);
        }
        else {
            // if no space available -> Drop
            // else -> Store in Inventory
            if (equippedWeapon != null && equippedWeapon != hands) UnequipWeapon();

            Weapon newWeapon = weapon;
            if (weapon == hands) {
                // if equipping hands -> activate hands
                hands.gameObject.SetActive(true);
            }
            else {
                // else -> Place weapon and start its animator
                hands.gameObject.SetActive(false);

                newWeapon.transform.SetParent(weaponHolder);
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;

                newWeapon.anim.enabled = true;
            }
            
            // Setup ik for weapon
            if (leftHandIkTarget != null) Destroy(leftHandIkTarget.gameObject);
            if (rightHandIkTarget != null) Destroy(rightHandIkTarget.gameObject);
            if (newWeapon.leftHandIkTarget != null) {
                leftHandIkTarget = Instantiate(newWeapon.leftHandIkTarget, newWeapon.leftHandIkTarget.position, newWeapon.leftHandIkTarget.rotation, null);
                leftHandIk.data.target = leftHandIkTarget; leftHandIk.weight = 1;
            }
            if (newWeapon.rightHandIkTarget != null) {
                rightHandIkTarget = Instantiate(newWeapon.rightHandIkTarget, newWeapon.rightHandIkTarget.position, newWeapon.rightHandIkTarget.rotation, null);
                rightHandIk.data.target = rightHandIkTarget; rightHandIk.weight = 1;
            }
            
            if (newWeapon.leftHandIkHint != null) leftHandIk.data.hint = newWeapon.leftHandIkHint;
            else leftHandIk.data.hint = defaultLeftHandHint;
            if (newWeapon.rightHandIkHint != null) rightHandIk.data.hint = newWeapon.rightHandIkHint;
            else rightHandIk.data.hint = defaultRightHandHint;

            StartCoroutine(RefreshIK());
            
            // Initialize
            equippedWeapon = newWeapon;
            newWeapon.OnPickup();
            newWeapon.OnEquip();
        }
    }
    public void UnequipWeapon() {
        if (equippedWeapon == null) return;

        equippedWeapon.transform.SetParent(null);
        equippedWeapon.anim.enabled = false;
        
        if (leftHandIkTarget != null) Destroy(leftHandIkTarget.gameObject);
        if (rightHandIkTarget != null) Destroy(rightHandIkTarget.gameObject);

        equippedWeapon.OnUnequip();

        if (PlayerInventory.instance.weapons.Count <= 0) {
            // Add to inventory
            PlayerInventory.AddItem(equippedWeapon);
        }
        else {
            // Drop
            RaycastHit hit = GetDropPoint();
            if (hit.collider != null) {
                equippedWeapon.transform.position = hit.point + hit.normal * equippedWeapon.dropHeightOffset;
                equippedWeapon.transform.rotation = Quaternion.LookRotation(transform.right);
                equippedWeapon.transform.localEulerAngles = new Vector3(equippedWeapon.transform.localEulerAngles.x, equippedWeapon.transform.localEulerAngles.y, 90);
            }
            else {
                Destroy(equippedWeapon.gameObject);
            }
            equippedWeapon.OnDrop();
        }
        
        equippedWeapon = null;
        EquipWeapon(hands);
    }

    private IEnumerator RefreshIK() {
        anim.enabled = false;
        rig.Build();

        yield return null;

        anim.Rebind();

        anim.enabled = true;
    }

    public RaycastHit GetDropPoint() {
        RaycastHit hit;
        Ray ray = new Ray(transform.position + Vector3.up * 2f + transform.forward * 1f, Vector3.down);

        Physics.Raycast(ray, out hit, 5, dropLocationLayer, QueryTriggerInteraction.Ignore);

        return hit;
    }
}
