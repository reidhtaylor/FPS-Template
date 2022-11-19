using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHandler : MonoBehaviour
{
    public static UIHandler instance;
    private void Awake() {
        instance = this;
    }

    public Canvas canvas;
    public Camera cam;

    [Header("Crosshairs")]
    [SerializeField] private CrosshairData[] crosshairs;
    public Dictionary<string, CrosshairData> crosshairStorage = new Dictionary<string, CrosshairData>();
    [System.Serializable] public class CrosshairData {
        public CanvasGroup group;
        public RectTransform rect;
        [Space(5)]
        public Vector2 inaccurateSize;
        public Vector2 accurateSize;
    }
    private string equippedCH;

    [Header("Interact Selection")]
    public float interactUIDistance = 20;
    public RectTransform interactSelectionUI;
    public Image interactSelectionIcon;
    public TextMeshProUGUI interactSelectionType;
    public TextMeshProUGUI interactSelectionName;

    [Header("Weapon Panel")]
    public TextMeshProUGUI weaponPanelNameDisplay;
    public Image weaponPanelIconDisplay;
    [Space]
    public TextMeshProUGUI gunPanelMagDisplay;
    public TextMeshProUGUI gunPanelRoundCountDisplay;
    public Image gunPanelRoundIcon;

    private Weapon weaponOnPanel;

    [Header("Colors")]
    public Sprite energyRoundIcon;
    public Color energyRoundColor;
    public Sprite shellRoundIcon;
    public Color shellRoundColor;
    public Sprite thunderRoundIcon;
    public Color thunderRoundColor;

    [Header("Pause")]
    public GameObject pauseOverlay;

    private void Start() {
        crosshairStorage.Clear();
        foreach (CrosshairData o in crosshairs) crosshairStorage.Add(o.group.name, o);
        EquipCrosshair("Dot_CH");
        UpdateWeaponPanel(null);
    }

    private void Update() {
        
    }

    // Crosshair UI
    public void EquipCrosshair(string name) {
        foreach (CrosshairData o in crosshairs) o.group.gameObject.SetActive(false);
        crosshairStorage[name].group.gameObject.SetActive(true);
        equippedCH = name;
    }
    public void UpdateCrosshair(bool weaponReady, bool aiming, float accuracy) {
        if (aiming) crosshairStorage[equippedCH].group.alpha = Mathf.Lerp(crosshairStorage[equippedCH].group.alpha, 0, Time.deltaTime * 30);
        else crosshairStorage[equippedCH].group.alpha = Mathf.Lerp(crosshairStorage[equippedCH].group.alpha, weaponReady ? 1 : 0.15f, Time.deltaTime * 20);

        crosshairStorage[equippedCH].rect.sizeDelta = Vector2.Lerp(crosshairStorage[equippedCH].rect.sizeDelta, Vector2.one * Vector2.Lerp(crosshairStorage[equippedCH].inaccurateSize, crosshairStorage[equippedCH].accurateSize, accuracy), Time.deltaTime * 15);
    }
    // Crosshair UI

    // Interactable UI
    public void SelectInteractable(Item i) {
        UpdateInteractable(i);
        interactSelectionUI.gameObject.SetActive(true);
    }
    public void UpdateInteractable(Item i) {
        interactSelectionUI.transform.position = cam.WorldToScreenPoint(i.transform.position) + Vector3.up * i.interactCollider.bounds.size.magnitude * interactUIDistance + Vector3.right * i.interactCollider.bounds.size.magnitude * interactUIDistance;
        interactSelectionIcon.sprite = i.itemIcon;
        interactSelectionType.text = i.itemType.ToString();
        interactSelectionName.text = i.itemName;
    }
    public void DeselectInteractable() {
        interactSelectionUI.gameObject.SetActive(false);
    }
    // Interactable UI

    // Weapon Panel
    public void RefreshWeaponPanel() {
        if (weaponOnPanel == null) return;
        else UpdateWeaponPanel(weaponOnPanel);
    }
    public void UpdateWeaponPanel(Weapon w) {
        ClearWeaponPanel();
        if (w == null) {
            return;
        }

        weaponOnPanel = w;

        weaponPanelNameDisplay.text = w.itemName;

        Gun g = (Gun)w;
        if (g != null) {
            gunPanelRoundIcon.gameObject.SetActive(true);
            gunPanelRoundIcon.sprite = GetRoundIcon(g.roundType);
            gunPanelMagDisplay.text = g.roundsInMag.ToString("000");
            gunPanelRoundCountDisplay.text = PlayerInventory.CountRounds(g.roundType).ToString("000");
        }
        else {
            gunPanelMagDisplay.text = string.Empty;
            gunPanelRoundCountDisplay.text = string.Empty;
        }
    }
    public void ClearWeaponPanel() {
        weaponOnPanel = null;
        weaponPanelNameDisplay.text = string.Empty;

        gunPanelMagDisplay.text = string.Empty;
        gunPanelRoundCountDisplay.text = string.Empty;
        gunPanelRoundIcon.gameObject.SetActive(false);
    }
    // Weapon Panel

    // Colors
    public Color GetRoundColor(GUN_ROUND_TYPE t) {
        switch(t) {
            case GUN_ROUND_TYPE.Energy: return energyRoundColor;
            case GUN_ROUND_TYPE.Shell: return shellRoundColor;
            case GUN_ROUND_TYPE.Thunder: return thunderRoundColor;
        }
        return Color.white;
    }
    public Sprite GetRoundIcon(GUN_ROUND_TYPE t) {
        switch(t) {
            case GUN_ROUND_TYPE.Energy: return energyRoundIcon;
            case GUN_ROUND_TYPE.Shell: return shellRoundIcon;
            case GUN_ROUND_TYPE.Thunder: return thunderRoundIcon;
        }
        return null;
    }
    // Colors

    // Pause
    public void OnPause() {
        pauseOverlay.SetActive(true);
    }
    public void OnResume() {
        pauseOverlay.SetActive(false);
    }
    // Pause
}
