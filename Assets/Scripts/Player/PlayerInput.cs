using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public float verticalInput { get; set; }
    public float horizontalInput { get; set; }

    public float verticalLook { get; set; }
    public float horizontalLook { get; set; }

    public bool sprint { get; set; }
    public bool crouch { get; set; }

    public delegate void OnJump(); public OnJump onJump;

    public delegate void OnInteract(); public OnInteract onInteract;

    public delegate void OnAimDown(); public OnAimDown onAimDown;
    public delegate void OnAimUp(); public OnAimUp onAimUp;

    public delegate void OnReload(); public OnReload onReload;
    public delegate void OnSwitchWeapons(); public OnSwitchWeapons onSwitchWeapons;

    public delegate void OnShoot(); public OnShoot onShoot;
    public delegate void OnShootDown(); public OnShootDown onShootDown;

    public delegate void OnMenuPressed(); public OnMenuPressed onMenuPressed;
    
    private void Update() {
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");

        verticalLook = Input.GetAxis("Mouse Y");
        horizontalLook = Input.GetAxis("Mouse X");

        sprint = Input.GetKey(KeyCode.LeftShift);
        crouch = Input.GetKey(KeyCode.LeftControl);

        if (Input.GetKeyDown(KeyCode.Q)) onAimDown?.Invoke();
        if (Input.GetKeyUp(KeyCode.Q)) onAimUp?.Invoke();
        if (Input.GetKeyDown(KeyCode.R)) onReload?.Invoke();
        // if (Input.GetKeyDown(KeyCode.Tab)) onSwitchWeapons?.Invoke();
        if (Input.GetKeyDown(KeyCode.Tab)) onMenuPressed?.Invoke();
        if (Input.GetKeyDown(KeyCode.Space)) onJump?.Invoke();
        if (Input.GetKeyDown(KeyCode.E)) onInteract?.Invoke();
        if (Input.GetMouseButton(0)) onShoot?.Invoke();
        if (Input.GetMouseButtonDown(0)) onShootDown?.Invoke();
    }
}
