using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TransformHandles
{
  [DisallowMultipleComponent]
  public class CustomOutline : MonoBehaviour
  {
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 5f;

    public Color OutlineColor
    {
      get => outlineColor;
      set { outlineColor = value; needsUpdate = true; }
    }
    public float OutlineWidth
    {
      get => outlineWidth;
      set { outlineWidth = Mathf.Clamp(value, 0f, 10f); needsUpdate = true; }
    }

    // ===== 内部 =====
    private static readonly HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    [Serializable] private class ListVector3 { public List<Vector3> data; }

    [Header("Optional")]
    [SerializeField, Tooltip("有効: エディタで平滑ノーマルを事前計算。無効: 実行時に計算。")]
    private bool precomputeOutline = false;

    [SerializeField, HideInInspector] private List<Mesh> bakeKeys = new();
    [SerializeField, HideInInspector] private List<ListVector3> bakeValues = new();

    private Renderer[] renderers;
    private Material outlineMaskMaterial; // Shader "Custom/CustomOutlineMask"
    private Material outlineFillMaterial; // Shader "Custom/CustomOutlineFill"
    private bool needsUpdate;

    // ---------- Unity lifecycle ----------
    private void Awake()
    {
      renderers = GetComponentsInChildren<Renderer>();
      CreateOutlineMaterials();      // マテリアル生成
      LoadSmoothNormals();           // UV4 セット & サブメッシュ統合
      needsUpdate = true;
    }

    private void OnEnable()
    {
      if (renderers == null) return;
      foreach (var r in renderers)
      {
        if (!r) continue;
        var mats = r.sharedMaterials.ToList();
        mats.Add(outlineMaskMaterial);
        mats.Add(outlineFillMaterial);
        r.materials = mats.ToArray();
      }
    }

    private void Update()
    {
      if (!needsUpdate) return;
      needsUpdate = false;
      UpdateMaterialProperties();    // OutlineAll 固定の設定を流す
    }

    private void OnValidate()
    {
      needsUpdate = true;

      if ((!precomputeOutline && bakeKeys.Count != 0) || bakeKeys.Count != bakeValues.Count)
      {
        bakeKeys.Clear();
        bakeValues.Clear();
      }
      if (precomputeOutline && bakeKeys.Count == 0) Bake();
    }

    private void OnDisable()
    {
      if (renderers == null) return;
      foreach (var r in renderers)
      {
        if (!r) continue;
        var mats = r.sharedMaterials.ToList();
        mats.Remove(outlineMaskMaterial);
        mats.Remove(outlineFillMaterial);
        r.materials = mats.ToArray();
      }
    }

    private void OnDestroy()
    {
      if (outlineMaskMaterial) Destroy(outlineMaskMaterial);
      if (outlineFillMaterial) Destroy(outlineFillMaterial);
    }

    // ---------- Setup ----------
    private void CreateOutlineMaterials()
    {
      var maskShader = Shader.Find("Custom/CustomOutlineMask");
      var fillShader = Shader.Find("Custom/CustomOutlineFill");
      if (maskShader == null || fillShader == null)
      {
        Debug.LogError("CustomOutline: Outline shaders not found (Custom/CustomOutlineMask, Custom/CustomOutlineFill).");
        return;
      }
      outlineMaskMaterial = new Material(maskShader) { name = "OutlineMask (Instance)" };
      outlineFillMaterial = new Material(fillShader) { name = "OutlineFill (Instance)" };
    }

    private void Bake()
    {
      var baked = new HashSet<Mesh>();
      foreach (var mf in GetComponentsInChildren<MeshFilter>())
      {
        if (!mf || mf.sharedMesh == null) continue;
        if (!baked.Add(mf.sharedMesh)) continue;

        var smooth = SmoothNormals(mf.sharedMesh);
        bakeKeys.Add(mf.sharedMesh);
        bakeValues.Add(new ListVector3 { data = smooth });
      }
    }

    // UV4 へスムーズノーマル。最後に「全三角形の統合サブメッシュ」を1つ追加
    private void LoadSmoothNormals()
    {
      foreach (var mf in GetComponentsInChildren<MeshFilter>())
      {
        var mesh = mf ? mf.sharedMesh : null;
        if (mesh == null) continue;
        if (!registeredMeshes.Add(mesh)) continue;

        var idx = bakeKeys.IndexOf(mesh);
        var smooth = (idx >= 0) ? bakeValues[idx].data : SmoothNormals(mesh);
        mesh.SetUVs(3, smooth); // UV4

        var r = mf.GetComponent<Renderer>();
        if (r != null) CombineSubmeshes(mesh, r.sharedMaterials);
      }

      foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
      {
        var mesh = smr ? smr.sharedMesh : null;
        if (mesh == null) continue;
        if (!registeredMeshes.Add(mesh)) continue;

        mesh.uv4 = new Vector2[mesh.vertexCount];
        CombineSubmeshes(mesh, smr.sharedMaterials);
      }
    }

    private List<Vector3> SmoothNormals(Mesh mesh)
    {
      var groups = mesh.vertices.Select((v, i) => new KeyValuePair<Vector3, int>(v, i))
                                .GroupBy(p => p.Key);
      var smooth = new List<Vector3>(mesh.normals);
      foreach (var g in groups)
      {
        if (g.Count() == 1) continue;
        var sum = Vector3.zero;
        foreach (var p in g) sum += smooth[p.Value];
        sum.Normalize();
        foreach (var p in g) smooth[p.Value] = sum;
      }
      return smooth;
    }

    private void CombineSubmeshes(Mesh mesh, Material[] materials)
    {
      if (mesh.subMeshCount == 1) return;
      if (mesh.subMeshCount > materials.Length) return;

      mesh.subMeshCount++;
      mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    /// OutlineAll 固定：Mask/Fill ともに ZTest=Always、幅と色を適用
    private void UpdateMaterialProperties()
    {
      if (!outlineMaskMaterial || !outlineFillMaterial) return;

      outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
      outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);

      outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
      outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
    }

  }
}
