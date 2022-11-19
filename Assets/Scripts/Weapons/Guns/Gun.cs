using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Weapon
{
    private PlayerController controller;
    private PlayerInput input;
    private PlayerWeaponHandler weaponHandler;

    [Header("Unique Data")]
    public GameObject ironsight;
    public Transform scopeHolder;
    [Space]
    public Transform firepoint;
    public ParticleSystem muzzleFlash;
    
    [Header("Gun Values")]
    public bool auto = true;
    public float firerate = 15;
    public float shootSpeed = 50;
    [Space]
    [Range(0, 1f)]
    public float accuracy = 0.9f;
    public float maxDistance = 200;
    [Space]
    public GUN_ROUND_TYPE roundType;
    public int magCapacity = 30;
    public int shotsPerRound = 1;
    [Space]
    public float reloadTime = 2;
    public bool rechamberAnimation = false;
    [Space]
    [Range(0, 0.3f)]
    public float recoilStrength = 0.05f;
    public float recoilControl = 1f;
    public float recoilAngle = 20f;
    public float maxRecoil = 0.1f;
    [Range(0, 1f)]
    public float recoilAimControl = 0.7f;
    [Space]
    public float recoilShakeLength = 0.1f;
    public float recoilShakeStrength = 2f;
    [Space]
    public float swayStrength = 0.1f;
    public float swaySpeed = 2;
    public float swayRotation = 20;
    [Space]
    public string crosshair = "Dot_CH";

    public int roundsInMag { get; set; }
    public bool reloading { get; set; }
    public float reloadingTimer { get; set; }

    private bool gunUp;
    private float gunUpTime = 1;
    private float gunUpTimer;
    private float lastShotStamp;

    private float recoil = 0;

    private bool aiming;
    private float aimSpeed = 60;

    private PlayerCameraController camController;
    private Coroutine fovCo;
    private float originalFOV;

    private Ray camRay = new Ray();
    private RaycastHit camHit;
    private Ray ray = new Ray();
    private RaycastHit hit;

    [HideInInspector] public List<GunShot> shots = new List<GunShot>();
    private Vector3 shotDir;

    private Scope equippedScope;

    private float horizontalSway, verticalSway;

    public override void Interact() {
        base.Interact();
    }
    public override void OnEquip() {
        // Setup references
        controller = PlayerController.instance;
        input = controller.GetComponent<PlayerInput>();
        weaponHandler = controller.weaponHandler;
        camController = controller.cameraController;

        // Setup callbacks
        input.onAimDown += OnAimDown;
        input.onAimUp += OnAimUp;
        input.onReload += Reload;
        if (auto) input.onShoot += OnShoot;
        else input.onShootDown += OnShoot;

        // Initialize FOV
        originalFOV = camController.cam.fieldOfView;

        // Set layer of gun to "Player"
        Utility.SetLayerOfObject(gameObject, "Player", true, true);

        // Call equip scope callback
        if (equippedScope != null) equippedScope.OnWeaponEquip();

        // Pull weapon
        anim.SetTrigger("PullOut");

        // Setup UI
        UIHandler.instance.EquipCrosshair(crosshair);
        UIHandler.instance.UpdateWeaponPanel(this);
    }
    public override void OnUnequip() {
        base.OnUnequip();

        // Remove callbacks
        input.onAimDown -= OnAimDown;
        input.onAimUp -= OnAimUp;
        input.onReload -= Reload;
        input.onShoot -= OnShoot;
        input.onShootDown -= OnShoot;

        // Set Cameras fov to original fov
        camController.SetFOV(originalFOV);

        // Reset layer to default
        Utility.SetLayerOfObject(gameObject, "Default", true, false);

        // Remove all additive weapon records
        if (weaponHandler.holderPositionRecord.ContainsKey(this)) weaponHandler.holderPositionRecord.Remove(this);
        if (weaponHandler.holderRotationRecord.ContainsKey(this)) weaponHandler.holderRotationRecord.Remove(this);

        // Call unequip scope callback
        if (equippedScope != null) equippedScope.OnWeaponUnequip();

        // if reloading: stop
        if (reloading) {
            reloading = false;
            reloadingTimer = 0;
        }

        // Reset crosshair
        UIHandler.instance.EquipCrosshair("Dot_CH");
    }

    public override void Update() {
        if (!inUse) return;

        // Refresh scope
        if (equippedScope != null) equippedScope.OnUpdate();

        // Calculate gun up timer
        if (gunUpTimer > 0) {
            gunUpTimer -= Time.deltaTime;
            if (gunUpTimer <= 0) {
                // Stop gun up
                gunUpTimer = 0;
                gunUp = false;
                anim.SetBool("GunUp", false);
            }
        }

        // Calculate reloading timer
        if (reloadingTimer > 0) {
            reloadingTimer -= Time.deltaTime;
            if (reloadingTimer <= 0) {
                // Stop reloading
                reloadingTimer = 0;
                reloading = false;

                // Fill mag and remove rounds from inventory
                int neededRounds = magCapacity - roundsInMag;
                if (PlayerInventory.HasRounds(roundType, neededRounds)) {
                    roundsInMag += neededRounds;
                    PlayerInventory.UseRounds(roundType, neededRounds);
                }
                else {
                    roundsInMag += PlayerInventory.CountRounds(roundType);
                    PlayerInventory.UseRounds(roundType, PlayerInventory.CountRounds(roundType));
                }

                // Refresh UI
                UIHandler.instance.UpdateWeaponPanel(this);
            }
        }

        // Delegate update onto shot bullets
        for (int i = 0; i < shots.Count; i++) {
            shots[i].OnUpdate(this);
        }

        // Calculate recoil timer
        if (recoil > 0) {
            recoil -= Time.deltaTime * recoilControl;
            if (recoil <= 0) recoil = 0;
        }
        
        // Smooth sway values
        horizontalSway = Mathf.Lerp(horizontalSway, camController.yDelta, Time.deltaTime * swaySpeed);
        verticalSway = Mathf.Lerp(verticalSway, camController.xDelta, Time.deltaTime * swaySpeed);

        // Set addivite pos and rot to weapon
        weaponHandler.holderPositionRecord[this] = (-Vector3.forward * recoil * (aiming ? 1 - recoilAimControl : 1)) + (-Vector3.right * horizontalSway * swayStrength * 0.1f) + (Vector3.up * verticalSway * swayStrength * 0.1f);
        weaponHandler.holderRotationRecord[this] = -Vector3.right * recoil * recoilAngle * (aiming ? 1 - recoilAimControl : 1) + (Vector3.up * -horizontalSway * swayRotation * swayStrength) + (Vector3.right * -verticalSway * swayRotation * swayStrength);

        // Refresh/Animate crosshair
        UIHandler.instance.UpdateCrosshair(
            (controller.movementState != MovementState.Running || gunUp), 
            aiming, 
            (controller.movementState == MovementState.Air ? 0 : (1 - (Mathf.Abs(controller.horizontalMagnitude) + Mathf.Abs(controller.verticalMagnitude)) / 2 * 0.35f))
        );
    }

    private void OnAimDown() {
        if (reloading) return;
        if (fovCo != null) StopCoroutine(fovCo);

        aiming = true;
        anim.SetBool("Aiming", true);
        fovCo = StartCoroutine(LerpFOV(equippedScope != null ? equippedScope.aimFOV : 60));

        if (equippedScope != null) equippedScope.OnAim();
    }
    private void OnAimUp() {
        if (fovCo != null) StopCoroutine(fovCo);

        aiming = false;
        anim.SetBool("Aiming", false);
        fovCo = StartCoroutine(LerpFOV(originalFOV));

        if (equippedScope != null) equippedScope.OnAimUp();
    }
    private IEnumerator LerpFOV(float to) {
        float start = camController.cam.fieldOfView;
        while (camController.cam.fieldOfView != to) {
            camController.SetFOV(Mathf.Lerp(camController.cam.fieldOfView, to, Time.deltaTime * aimSpeed));
            yield return null;
        }
        camController.SetFOV(to);

        fovCo = null;
    }

    private void OnShoot() {
        if (reloading) return;
        
        if (roundsInMag <= 0) {
            Reload();
            return;
        }

        if (Time.time >= lastShotStamp + (1 / firerate)) {
            GunUp();

            recoil = Mathf.Min(maxRecoil, recoil + recoilStrength);
            camController.Shake(recoilShakeLength, recoilShakeStrength);

            muzzleFlash.Play();

            for (int i = 0; i < shotsPerRound; i++) {
                ShootRay();

                GunShot gs = Instantiate(GameManager.instance.GetRoundPrefab(roundType));
                gs.Setup(firepoint.position, hit.collider != null ? hit.point : ray.origin + ray.direction * maxDistance, shootSpeed);
                shots.Add(gs);
            }

            lastShotStamp = Time.time;
            roundsInMag--;
            
            UIHandler.instance.UpdateWeaponPanel(this);
            if (rechamberAnimation) anim.SetTrigger("Rechamber");

            // if (roundsInMag <= 0) Reload();
        }
    }
    private void ShootRay() {
        camRay.origin = camController.cam.transform.position;
        camRay.direction = camController.cam.transform.forward;
        Physics.Raycast(camRay, out camHit);
        
        shotDir = (camHit.collider != null ? camHit.point : camRay.origin + camRay.direction * maxDistance);
        ray.origin = firepoint.position;
        ray.direction = (shotDir - firepoint.position) + (Vector3.Cross(shotDir, firepoint.up) * Random.Range(-0.2f, 0.2f) * (1 - accuracy)) + (Vector3.Cross(shotDir, firepoint.right) * Random.Range(-0.2f, 0.2f) * (1 - accuracy));;
        Physics.Raycast(ray, out hit, maxDistance);
    }

    private void Reload() {
        if (reloading || roundsInMag >= magCapacity || PlayerInventory.CountRounds(roundType) <= 0) return;
        
        OnAimUp();
        
        anim.SetTrigger("Reload");
        reloadingTimer = reloadTime;
        reloading = true;

        GunUp();
    }

    private void GunUp() {
        gunUp = true;
        gunUpTimer = gunUpTime;
        anim.SetBool("GunUp", true);
    }

    // Scope
    public void EquipScope(Scope s) {
        UnequipScope();

        ironsight.SetActive(false);
        s.transform.SetParent(scopeHolder);
        s.transform.localPosition = Vector3.zero;
        s.transform.localEulerAngles = Vector3.zero;
        equippedScope = s;
        equippedScope.OnEquip();
        equippedScope.OnWeaponEquip();
    }
    public void UnequipScope() {
        if (equippedScope == null) return;

        equippedScope.transform.SetParent(null);
        RaycastHit hit = weaponHandler.GetDropPoint();
        if (hit.collider != null) {
            equippedScope.transform.position = hit.point + hit.normal * equippedScope.dropOffset;
            equippedScope.transform.rotation = Quaternion.LookRotation(PlayerController.instance.transform.right);
            equippedScope.transform.localEulerAngles = new Vector3(equippedScope.transform.localEulerAngles.x, equippedScope.transform.localEulerAngles.y, 0);
        }
        else {
            Destroy(equippedScope.gameObject);
        }

        ironsight.SetActive(true);
        equippedScope.OnWeaponUnequip();
        equippedScope.OnUnequip();
        equippedScope = null;
    }
    // Scope
}
