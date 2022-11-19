using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private void Awake() {
        instance = this;
    }

    public void PlayerInitialized() {
        PlayerController.instance.input.onMenuPressed += MenuPressed;
    }

    // Rounds
    [Header("Rounds")]
    public GunShot energyRound;
    public GunShot shellRound;
    public GunShot thunderRound;

    public GunShot GetRoundPrefab(GUN_ROUND_TYPE t) {
        switch (t) {
            case GUN_ROUND_TYPE.Energy: return energyRound;
            case GUN_ROUND_TYPE.Shell: return shellRound;
            case GUN_ROUND_TYPE.Thunder: return thunderRound;
        }
        return energyRound;
    }
    // Rounds

    // Pause
    [Header("Pause")]
    public bool paused;

    public void MenuPressed() {
        if (paused) Resume();
        else Pause();
    }
    public void Pause() {
        Time.timeScale = 0;
        UIHandler.instance.OnPause();
        Cursor.lockState = CursorLockMode.None;

        paused = true;
    }
    public void Resume() {
        Time.timeScale = 1;
        UIHandler.instance.OnResume();
        Cursor.lockState = CursorLockMode.Locked;

        paused = false;
    }
    // Pause
}
