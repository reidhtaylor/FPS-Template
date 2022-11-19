using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementState { Idle, Walking, Running, Air, Crouching, Sliding }

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    private void Awake() {
        instance = this;
    }

    public PlayerInput input;
    public PlayerWeaponHandler weaponHandler;
    public PlayerCameraController cameraController;
    [Space]
    public Rigidbody rb;
    public CapsuleCollider col;
    
    [Header("Speed")]
    public float walkSpeed = 2;
    public float runSpeed = 3.5f;
    public float jumpHeight = 10;

    [Header("Crouching")]
    public Vector4 crouchColliderData;
    public float crouchMoveSpeed = 1.7f;

    [Header("Sliding")]
    public Vector4 slideColliderData;
    private float slidePower;

    [Header("On Slope")]
    public bool onSlope;
    public float isSlopeAngle = 20;
    private bool exitingSlope;

    public MovementState movementState = MovementState.Idle;

    public bool grounded { get; set; } private RaycastHit groundHit;
    public bool crouching { get; set; }
    public bool sliding { get; set; }

    private Vector4 normalColliderData;
    private Vector4 targetColliderData;

    public float verticalMagnitude { get; set; }
    public float horizontalMagnitude { get; set; }

    private void OnEnable() {
        input.onJump += OnJump;
    }
    private void OnDisable() {
        input.onJump -= OnJump;
    }

    private void Start() {
        normalColliderData = col.center;
        normalColliderData.w = col.height;
        targetColliderData = normalColliderData;

        GameManager.instance.PlayerInitialized();
    }

    private void FixedUpdate() {
        HandleState();

        // Set collider values smoothly
        col.center = Vector3.Lerp(col.center, targetColliderData, Time.deltaTime * 20);
        col.height = Mathf.Lerp(col.height, targetColliderData.w, Time.deltaTime * 20);

        // If not wanting to crouch anymore -> Cancel Crouch
        if (crouching && movementState != MovementState.Crouching) CancelCrouch();
        // If not wanting to slide anymore -> Cancel Slide
        if (sliding && movementState != MovementState.Sliding) CancelSlide();

        // Calculate move vector
        Vector3 movement = GetMoveDirection();
        movement *= crouching ? crouchMoveSpeed : walkSpeed;
        movement.y = rb.velocity.y;

        // If running and not crouching or sliding and moving forward and left or right -> run
        if (input.sprint && !crouching && !sliding && input.verticalInput > 0 && Mathf.Abs(input.horizontalInput) <= 0.3f) movement += transform.forward * (runSpeed - walkSpeed);
        
        // Remove control while sliding
        if (!sliding) rb.velocity = movement;
        else rb.AddForce(Vector3.ProjectOnPlane(transform.forward, groundHit.normal) * slidePower, ForceMode.Force);

        // Slow sliding
        if (slidePower > 0) slidePower -= Time.deltaTime * 100;

        // Movemnet magnitude
        verticalMagnitude = input.verticalInput == 0 ? 0 : Vector3.Project(rb.velocity, transform.forward).magnitude * (Mathf.Abs(input.verticalInput) / input.verticalInput);
        horizontalMagnitude = input.horizontalInput == 0 ? 0 : Vector3.Project(rb.velocity, transform.right).magnitude * (Mathf.Abs(input.horizontalInput) / input.horizontalInput);

        // Check if on ground
        grounded = Physics.Raycast(transform.position + transform.up, Vector3.down, out groundHit, 1.1f);
        onSlope = IsSlope();

        rb.useGravity = !onSlope;

        if (onSlope && rb.velocity.y > 0 && !exitingSlope) {
            rb.AddForce(Vector3.down * 80, ForceMode.Force);
        }
        if (!onSlope && exitingSlope) exitingSlope = false;
    }

    private void HandleState() {
        bool hasInput = Mathf.Abs(input.horizontalInput) > 0 || Mathf.Abs(input.verticalInput) > 0;

        if (grounded && input.crouch) {
            if (!crouching) {
                if (!input.sprint) {
                    OnCrouch();
                    movementState = MovementState.Crouching;
                }
            }
            else movementState = MovementState.Crouching;

            if (!sliding) {
                if (input.sprint && !crouching) {
                    OnSlide();
                    movementState = MovementState.Sliding;
                }
            }
            else movementState = MovementState.Sliding;
        }

        else if (grounded && Mathf.Abs(input.horizontalInput) <= 0.3f && input.verticalInput > 0 && input.sprint) {
            movementState = MovementState.Running;
        }

        else if (grounded && hasInput) {
            movementState = MovementState.Walking;
        }

        else if (grounded) {
            movementState = MovementState.Idle;
        }

        else {
            movementState = MovementState.Air;
        }
    }

    private Vector3 GetMoveDirection() {
        if (onSlope) return (Vector3.ProjectOnPlane(transform.right, groundHit.normal) * input.horizontalInput) + (Vector3.ProjectOnPlane(transform.forward, groundHit.normal) * input.verticalInput);
        else return (transform.right * input.horizontalInput) + (transform.forward * input.verticalInput);
    }

    private void OnJump() {
        if (!grounded) return;
        if (crouching) CancelCrouch();
        rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);

        exitingSlope = true;
    }

    private void OnCrouch() {
        crouching = true;
        targetColliderData = crouchColliderData;
    }
    private void CancelCrouch() {
        crouching = false;
        targetColliderData = normalColliderData;
    }

    private void OnSlide() {
        sliding = true;
        targetColliderData = slideColliderData;

        slidePower = 20;
    }
    private void CancelSlide() {
        sliding = false;
        targetColliderData = normalColliderData;
    }

    private bool IsSlope() {
        return Mathf.Abs(Vector3.Angle(transform.up, groundHit.normal)) >= isSlopeAngle;
    }
    private Vector3 GetSlopeUphillDirection() {
        return -Vector3.Cross(Vector3.Cross(transform.up, groundHit.normal), groundHit.normal);
    }
}
