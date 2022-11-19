using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraInteract : MonoBehaviour
{
    public PlayerInput input;
    public PlayerCameraController camController;
    [Space]
    public float reachDistance = 5;

    private Item selected;

    private void OnEnable() {
        input.onInteract += Interact;
    }
    private void OnDisable() {
        input.onInteract -= Interact;
    }

    private void Update() {
        if (selected != null) UIHandler.instance.UpdateInteractable(selected);

        if (camController.hit.collider != null && camController.hit.distance < reachDistance) {
            // Hit something
            Item i = camController.hit.collider.GetComponent<Item>();
            if (i != null) {
                if (selected != i) {
                    // Select Item
                    selected = i;
                    UIHandler.instance.SelectInteractable(i);
                }
            }
            else Deselect();
        }
        else Deselect();
    }

    private void Interact() {
        if (selected != null && selected.CanInteract()) selected.Interact();
    }
    private void Deselect() {
        UIHandler.instance.DeselectInteractable();
        selected = null;
    }
}
