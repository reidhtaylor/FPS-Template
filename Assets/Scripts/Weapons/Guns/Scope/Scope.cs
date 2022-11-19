using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scope : Item
{
    [Header("Drop Settings")]
    public float dropOffset = 0.1f;

    [Header("Aim")]
    public float aimFOV = 60;
    [Range(0, 1f)]
    public float sensitivityMultiplier = 0.6f;

    [Header("Zoom")]
    public bool zoom = false;
    public float zoomX = 1;
    public MeshRenderer zoomProjector;
    public Camera zoomCamera;

    private Vector4 scopeShift = new Vector4(0, 0, 0, 0);
    private float shiftSpeed = 1.5f;
    private float shiftPower = 0.3f;

    private PlayerWeaponHandler handler;
    private PlayerCameraController camController;

    private void Start() {
        handler = PlayerController.instance.weaponHandler;
        camController = PlayerController.instance.cameraController;
    }

    public override bool CanInteract() {
        return handler.equippedWeapon != null && (handler.equippedWeapon is Gun);
    }
    public override void Interact() {
        base.Interact();
        Gun gun = handler.equippedWeapon as Gun;
        gun.EquipScope(this);
    }

    public void OnUpdate() {
        if (zoom) {
            zoomCamera.transform.position = camController.cam.transform.position;
            zoomCamera.transform.rotation = camController.cam.transform.rotation;
            zoomCamera.fieldOfView = camController.cam.fieldOfView / (2 * zoomX);

            scopeShift.x = Mathf.Lerp(scopeShift.x, -camController.yDelta, Time.deltaTime * shiftSpeed) * shiftPower;
            scopeShift.y = Mathf.Lerp(scopeShift.y, camController.xDelta, Time.deltaTime * shiftSpeed) * shiftPower;
            zoomProjector.sharedMaterial.SetVector("_ScopeShift", scopeShift);
        }
    }

    public void OnAim() {
        if (zoom) {
            zoomCamera.gameObject.SetActive(true);
            zoomProjector.gameObject.SetActive(true);
        }
        camController.sensitivityMultiplier = sensitivityMultiplier;
    }
    public void OnAimUp() {
        if (zoom) {
            zoomCamera.gameObject.SetActive(false);
            zoomProjector.gameObject.SetActive(false);
        }
        camController.sensitivityMultiplier = 1f;
    }

    public void OnEquip() {

    }
    public void OnUnequip() {
        inUse = false;
        interactCollider.enabled = true;
    }

    public void OnWeaponEquip() {
        Utility.SetLayerOfObject(gameObject, "Player");
    }
    public void OnWeaponUnequip() {
        Utility.SetLayerOfObject(gameObject, "Default");
        camController.sensitivityMultiplier = 1f;
    }
}
