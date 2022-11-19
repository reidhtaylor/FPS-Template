using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rounds : Item
{
    [Header("Round Type")]
    public GUN_ROUND_TYPE roundType;
    public int amount = 30;

    public override void Interact() {
        PlayerInventory.AddRounds(roundType, amount);
        Destroy(gameObject);
    }
}
