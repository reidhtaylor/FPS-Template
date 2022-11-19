using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public PlayerController player;
    public PlayerInput input;
    [Space]
    public Vector2 sensitivity = new Vector2(1, 1);
    public Vector2 clampLook = new Vector2(-60, 60);

    public Camera cam { get; set; }

    [HideInInspector] public Ray ray = new Ray();
    [HideInInspector] public RaycastHit hit;

    private float xRot;
    private float yRot;

    public float xDelta { get; set; }
    public float yDelta { get; set; }

    public float sensitivityMultiplier { get; set; } = 1;

    private float shakeTimer;
    private float shakeTime = 1;
    private float shakeAmp;
    private float shakeFreq;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;

        cam = GetComponent<Camera>();
    }

    private void Update() {
        if (shakeTimer > 0) {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0) shakeTimer = 0;
        }

        transform.localRotation = Quaternion.Euler(xRot, 0, Mathf.PerlinNoise(Time.time * shakeFreq, Time.time * shakeFreq) * shakeAmp * (shakeTimer / shakeTime));

        xDelta = input.verticalLook * sensitivity.y * Time.deltaTime * sensitivityMultiplier;
        xRot += xDelta;
        xRot = Mathf.Clamp(xRot, clampLook.x, clampLook.y);

        yDelta = input.horizontalLook * sensitivity.x * Time.deltaTime * sensitivityMultiplier;
        yRot += yDelta;
        if (yRot >= 360) yRot -= 360;
        player.transform.rotation = Quaternion.Euler(0, yRot, 0);

        ray.origin = cam.transform.position;
        ray.direction = cam.transform.forward;
        Physics.Raycast(ray, out hit);
    }

    public void SetFOV(float fov) {
        cam.fieldOfView = fov;
    }

    public void Shake(float t, float amp = 2, float freq = 13) {
        shakeTime = shakeTimer = t;
        shakeAmp = amp;
        shakeFreq = freq;
    }

    public Vector3 GetRayPoint() => hit.collider == null ? ray.origin + ray.direction * 200 : hit.point;
}
