using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility
{
    public static void SetLayerOfObject(GameObject o, int layer, bool setChildren = true, bool onlyIfDefault = false) {
        if (!onlyIfDefault || o.layer == 0) o.layer = layer;
        
        if (!setChildren || o.transform.childCount <= 0) return;

        for(int i = 0; i < o.transform.childCount; i++) {
            SetLayerOfObject(o.transform.GetChild(i).gameObject, layer, setChildren, onlyIfDefault);
        }
    }
    public static void SetLayerOfObject(GameObject o, string layerName, bool setChildren = true, bool onlyIfDefault = false) {
        SetLayerOfObject(o, GetLayerByName(layerName), setChildren, onlyIfDefault);
    }
    public static int GetLayerByName(string layerName) => LayerMask.NameToLayer(layerName);
}
