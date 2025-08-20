using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class CustomOutline : MonoBehaviour
{
  [Header("Outline Settings")]
  [SerializeField] private Color outlineColor = Color.white;
  [SerializeField, Range(0f, 10f)] private float outlineWidth = 2f;

  private Renderer[] renderers;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  // プロパティ
  public Color OutlineColor
  {
    get { return outlineColor; }
    set
    {
      outlineColor = value;
      UpdateMaterialProperties();
    }
  }

  public float OutlineWidth
  {
    get { return outlineWidth; }
    set
    {
      outlineWidth = Mathf.Clamp(value, 0f, 10f);
      UpdateMaterialProperties();
    }
  }

  void Awake()
  {
    // レンダラーを取得
    renderers = GetComponentsInChildren<Renderer>();
    if (renderers.Length == 0)
    {
      Debug.LogWarning("CustomOutline: No renderers found on " + gameObject.name);
      return;
    }

    // アウトラインマテリアルを作成
    CreateOutlineMaterials();

    // スムーズノーマルの処理
    ProcessSmoothNormals();
  }

  void OnEnable()
  {
    ApplyOutline();
  }

  void OnDisable()
  {
    RemoveOutline();
  }

  void OnDestroy()
  {
    CleanupMaterials();
  }

  void OnValidate()
  {
    if (Application.isPlaying)
    {
      UpdateMaterialProperties();
    }
  }

  private void CreateOutlineMaterials()
  {
    // シェーダーをロード
    Shader maskShader = Shader.Find("Custom/CustomOutlineMask");
    Shader fillShader = Shader.Find("Custom/CustomOutlineFill");

    if (maskShader == null || fillShader == null)
    {
      Debug.LogError("CustomOutline: Outline shaders not found!");
      return;
    }

    // アウトラインマテリアルを作成
    outlineMaskMaterial = new Material(maskShader);
    outlineFillMaterial = new Material(fillShader);

    outlineMaskMaterial.name = "Outline Mask (Instance)";
    outlineFillMaterial.name = "Outline Fill (Instance)";

    // プロパティを設定
    UpdateMaterialProperties();
  }

  private void ProcessSmoothNormals()
  {
    // メッシュフィルターを取得
    MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

    foreach (MeshFilter meshFilter in meshFilters)
    {
      if (meshFilter.mesh == null) continue;

      Mesh mesh = meshFilter.mesh;

      // メッシュが読み取り可能かチェック
      if (!mesh.isReadable)
      {
        Debug.LogWarning($"CustomOutline: Mesh '{mesh.name}' is not readable. Set 'Read/Write Enabled' in import settings.");
        continue;
      }

      // スムーズノーマルを計算
      Vector3[] smoothNormals = CalculateSmoothNormals(mesh);

      // メッシュにスムーズノーマルを設定
      mesh.SetUVs(3, smoothNormals);
    }
  }

  private Vector3[] CalculateSmoothNormals(Mesh mesh)
  {
    // 頂点を位置でグループ化
    var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index))
                              .GroupBy(pair => pair.Key);

    // ノーマルを新しいリストにコピー
    var smoothNormals = new List<Vector3>(mesh.normals);

    // グループ化された頂点のノーマルを平均化
    foreach (var group in groups)
    {
      // 単一頂点はスキップ
      if (group.Count() == 1)
      {
        continue;
      }

      // 平均ノーマルを計算
      var smoothNormal = Vector3.zero;

      foreach (var pair in group)
      {
        smoothNormal += smoothNormals[pair.Value];
      }

      smoothNormal.Normalize();

      // 各頂点にスムーズノーマルを割り当て
      foreach (var pair in group)
      {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals.ToArray();
  }

  private void UpdateMaterialProperties()
  {
    if (outlineFillMaterial != null)
    {
      outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
      outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
    }
  }

  private void ApplyOutline()
  {
    if (outlineMaskMaterial == null || outlineFillMaterial == null) return;

    foreach (var renderer in renderers)
    {
      if (renderer == null) continue;

      // 既存のマテリアルリストを取得
      var materials = new List<Material>(renderer.sharedMaterials);

      // アウトラインマテリアルが既に含まれていない場合のみ追加
      if (!materials.Contains(outlineMaskMaterial))
      {
        materials.Add(outlineMaskMaterial);
      }
      if (!materials.Contains(outlineFillMaterial))
      {
        materials.Add(outlineFillMaterial);
      }

      // マテリアルリストを適用
      renderer.materials = materials.ToArray();
    }
  }

  private void RemoveOutline()
  {
    foreach (var renderer in renderers)
    {
      if (renderer == null) continue;

      // 既存のマテリアルリストを取得
      var materials = new List<Material>(renderer.sharedMaterials);

      // アウトラインマテリアルを削除
      materials.Remove(outlineMaskMaterial);
      materials.Remove(outlineFillMaterial);

      // マテリアルリストを適用
      renderer.materials = materials.ToArray();
    }
  }

  private void CleanupMaterials()
  {
    if (outlineMaskMaterial != null)
    {
      if (Application.isPlaying)
      {
        Destroy(outlineMaskMaterial);
      }
      else
      {
        DestroyImmediate(outlineMaskMaterial);
      }
    }

    if (outlineFillMaterial != null)
    {
      if (Application.isPlaying)
      {
        Destroy(outlineFillMaterial);
      }
      else
      {
        DestroyImmediate(outlineFillMaterial);
      }
    }
  }

  // パブリックメソッド
  public void SetOutlineColor(Color color)
  {
    OutlineColor = color;
  }

  public void SetOutlineWidth(float width)
  {
    OutlineWidth = width;
  }
}

