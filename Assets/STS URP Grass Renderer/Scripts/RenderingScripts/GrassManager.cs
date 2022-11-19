using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class GrassManager : MonoBehaviour
{
    #region Singleton
    public static GrassManager instance;
    private void Awake() {
        instance = this;
    }
    #endregion

    public ComputeShader grassComputeShader;
    public Material grassMaterial;
    public GrassSettings grassSettings = new GrassSettings();
    
    public List<SourceVertex> vertices = new List<SourceVertex>();

    [System.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SourceVertex {
        public Vector3 position;
        public Vector3 normal;
    }
    private ComputeBuffer sourceVertexBuffer;
    private ComputeBuffer drawBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeShader instanceShader;
    private Material instanceMat;
    private int idGrassKernel;
    private int dispatchSize;
    private Bounds localBounds;

    private const int SOURCE_VERTEX_STRIDE = sizeof(float) * (3 + 3);
    private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 1) * 3);
    private const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;

    private int[] argsBufferReset = new int[] { 0, 1, 0, 0 };

    private bool initialized;

    #region Initialization
    private void Start() {
        #if UNITY_EDITOR
        GrassManager[] grassManagers = FindObjectsOfType<GrassManager>();
        if (grassManagers.Length > 1) {
            Debug.LogWarning("There are multiple /'GrassManager/' types in your scene! Only one is needed...");
        }

        if (!Application.isPlaying) {
            grassSettings.TrySetDefault();
        }
        #endif
    }


    public void OnEnable() {
        if (HasIssues()) return;
        if (initialized) {
            OnDisable();
        }

        if (vertices.Count <= 0) return;
        initialized = true;

        int numSourceVertices = vertices.Count;
        int maxBladeSegments = grassSettings.maxSegments;
        int maxBladeTriangles = (maxBladeSegments - 1) * 2 + 1;
        
        instanceShader = Instantiate(grassComputeShader);
        instanceMat = Instantiate(grassMaterial);

        sourceVertexBuffer = new ComputeBuffer(vertices.Count, SOURCE_VERTEX_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVertexBuffer.SetData(vertices);
        drawBuffer = new ComputeBuffer(numSourceVertices * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);

        idGrassKernel = instanceShader.FindKernel("Main");

        // Buffer stuff
        instanceShader.SetBuffer(idGrassKernel, "_SourceVertices", sourceVertexBuffer);
        instanceShader.SetBuffer(idGrassKernel, "_DrawTriangles", drawBuffer);
        instanceShader.SetBuffer(idGrassKernel, "_IndirectArgsBuffer", argsBuffer);
        instanceShader.SetInt("_NumSourceVertices", numSourceVertices);

        instanceMat.SetBuffer("_DrawTriangles", drawBuffer);

        // Variable stuff
        instanceShader.SetInt("_MaxBladeSegments", maxBladeSegments);
        instanceShader.SetFloat("_MaxBendAngle", grassSettings.maxBendAngle);
        instanceShader.SetFloat("_BladeCurvature", grassSettings.bladeCurvature);
        instanceShader.SetFloat("_BladeHeight", grassSettings.bladeHeight);
        instanceShader.SetFloat("_BladeHeightVariance", grassSettings.bladeHeightVariance);
        instanceShader.SetFloat("_BladeWidth", grassSettings.bladeWidth);
        instanceShader.SetFloat("_BladeWidthVariance", grassSettings.bladeWidthVariance);
        instanceShader.SetTexture(idGrassKernel, "_WindNoiseTexture", grassSettings.windNoise);
        instanceShader.SetFloat("_WindTimeMult", grassSettings.windSpeed);
        instanceShader.SetFloat("_WindPosMult", grassSettings.windScale);
        instanceShader.SetFloat("_WindAmplitude", grassSettings.windAmount);
        instanceShader.SetFloat("_ClipDist", grassSettings.clipDistance);
        instanceShader.SetFloat("_ClipBlend", grassSettings.clipBlend);

        instanceShader.GetKernelThreadGroupSizes(idGrassKernel, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)numSourceVertices / threadGroupSize);

        UpdateBounds();
    }

    public void OnDisable() {
        if (initialized) {
            if (sourceVertexBuffer != null) sourceVertexBuffer.Release();
            if (drawBuffer != null) drawBuffer.Release();
            if (argsBuffer != null) argsBuffer.Release();

            if (instanceShader != null) if (Application.isPlaying) Destroy(instanceShader); else DestroyImmediate(instanceShader);
            if (instanceMat != null) if (Application.isPlaying) Destroy(instanceMat); else DestroyImmediate(instanceMat);
        }
        initialized = false;
    }

    public void ForceRefresh() {
        OnDisable();
        OnEnable();
    }
    private void UpdateBounds() {
        Bounds b = new Bounds();
        for (int i = 0; i < vertices.Count; i++) {
            b.Encapsulate(vertices[i].position);
        }
        localBounds = b;
        localBounds.Expand(Mathf.Max(grassSettings.bladeHeight + grassSettings.bladeHeightVariance, grassSettings.bladeWidth + grassSettings.bladeWidthVariance));
    }

    public static GrassManager CreateManager() {
        GameObject o = new GameObject("GrassManager");
        instance = o.AddComponent<GrassManager>();
        return instance;
    }
    #endregion

    public void LateUpdate() {
        if (HasIssues() || vertices.Count <= 0) return;

        if (!Application.isPlaying) {
            ForceRefresh();
        }

        drawBuffer.SetCounterValue(0);
        argsBuffer.SetData(argsBufferReset);

        Bounds bounds = TransformBounds(localBounds);

        instanceShader.SetVector("_Time", new Vector4(0, Time.timeSinceLevelLoad, 0, 0));

        if (Application.isPlaying) UpdateCameraInfo(grassSettings.overrideLODCam == null ? Camera.main : grassSettings.overrideLODCam);
        else UpdateCameraInfo(grassSettings.overrideLODCam);

        instanceShader.Dispatch(idGrassKernel, dispatchSize, 1, 1);

        Graphics.DrawProceduralIndirect(instanceMat, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, ShadowCastingMode.Off, true, gameObject.layer);
    }
    public void UpdateCameraInfo(Camera cam) {
        Vector4 camPosData = cam == null ? Vector3.zero : cam.transform.position;
        instanceShader.SetVector("_CamPos", camPosData);
    }

    #region Helper Methods
    public Bounds TransformBounds(Bounds boundsOS) {
        var center = transform.TransformPoint(boundsOS.center);

        var extents = boundsOS.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        return new Bounds { center = center, extents = extents };
    }

    public bool HasIssues() => (grassComputeShader == null || grassMaterial == null || grassSettings.windNoise == null);

    public List<SourceVertex> GetVertices() => vertices;

    public void AddBlade(SourceVertex vertex) {
        vertices.Add(vertex);
    }
    public void AddBlades(List<SourceVertex> vs) {
        vertices.AddRange(vs);
        if (!Application.isPlaying) ForceRefresh();
    }

    public void RemoveBlades(List<SourceVertex> verts) {
        vertices = vertices.Except(verts).ToList();
        if (!Application.isPlaying) ForceRefresh();
    }

    public void ResetPoints() {
        vertices = new List<SourceVertex>();
    }
    #endregion
}