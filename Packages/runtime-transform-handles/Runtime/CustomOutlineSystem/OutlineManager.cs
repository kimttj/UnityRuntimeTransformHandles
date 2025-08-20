using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TransformHandles
{
  public class OutlineManager : MonoBehaviour
  {
    // シングルトンインスタンス
    private static OutlineManager _instance;
    public static OutlineManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = FindObjectOfType<OutlineManager>();
          if (_instance == null)
          {
            GameObject go = new GameObject("OutlineManager");
            _instance = go.AddComponent<OutlineManager>();
            DontDestroyOnLoad(go);
          }
        }
        return _instance;
      }
    }

    [Header("Global Outline Settings")]
    [SerializeField] private bool enableGlobalOutline = true;
    [SerializeField] private Color defaultOutlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float defaultOutlineWidth = 5f;

    [Header("Selection Outline Settings")]
    [SerializeField] private bool enableSelectionOutline = true;
    [SerializeField] private Color selectionOutlineColor = Color.red;

    private List<CustomOutline> allOutlines = new List<CustomOutline>();
    private List<CustomOutline> selectedOutlines = new List<CustomOutline>();

    void Awake()
    {
      // シングルトンの設定
      if (_instance == null)
      {
        _instance = this;
        DontDestroyOnLoad(gameObject);
        RegisterAllOutlines();
      }
      else if (_instance != this)
      {
        Destroy(gameObject);
      }
    }

    void Start()
    {
      UpdateAllOutlines();
    }

    void Update()
    {
      // 必要に応じて更新処理を追加
    }

    void OnDestroy()
    {
      // シングルトンインスタンスが破棄される時のクリーンアップ
      if (_instance == this)
      {
        // すべてのアウトラインをクリア
        ClearSelection();
        allOutlines.Clear();
        selectedOutlines.Clear();

        // インスタンスをnullに設定
        _instance = null;
      }
    }

    // グローバルアウトラインの有効/無効を切り替え
    public void ToggleGlobalOutline()
    {
      enableGlobalOutline = !enableGlobalOutline;
      UpdateAllOutlines();
    }

    // 選択アウトラインの有効/無効を切り替え
    public void ToggleSelectionOutline()
    {
      enableSelectionOutline = !enableSelectionOutline;
      UpdateSelectionOutlines();
    }

    // デフォルトアウトライン色を設定
    public void SetDefaultOutlineColor(Color color)
    {
      defaultOutlineColor = color;
      UpdateAllOutlines();
    }

    // デフォルトアウトライン幅を設定
    public void SetDefaultOutlineWidth(float width)
    {
      defaultOutlineWidth = Mathf.Clamp(width, 0f, 10f);
      UpdateAllOutlines();
    }

    // 選択アウトライン色を設定
    public void SetSelectionOutlineColor(Color color)
    {
      selectionOutlineColor = color;
      UpdateSelectionOutlines();
    }

    // アウトラインを登録
    public void RegisterOutline(CustomOutline outline)
    {
      if (outline == null || allOutlines.Contains(outline))
      {
        return;
      }

      allOutlines.Add(outline);

      // デフォルト設定を適用
      if (enableGlobalOutline)
      {
        outline.SetOutlineColor(defaultOutlineColor);
        outline.SetOutlineWidth(defaultOutlineWidth);
        outline.enabled = true;
      }
      else
      {
        outline.enabled = false;
      }
    }

    // アウトラインを登録解除
    public void UnregisterOutline(CustomOutline outline)
    {
      if (allOutlines.Contains(outline))
      {
        allOutlines.Remove(outline);
      }

      if (selectedOutlines.Contains(outline))
      {
        selectedOutlines.Remove(outline);
      }
    }

    // すべてのアウトラインを登録
    private void RegisterAllOutlines()
    {
      CustomOutline[] outlines = FindObjectsOfType<CustomOutline>();
      foreach (CustomOutline outline in outlines)
      {
        RegisterOutline(outline);
      }
    }

    // オブジェクトを選択
    public void SelectObject(GameObject obj)
    {
      CustomOutline outline = obj.GetComponent<CustomOutline>();
      if (outline != null)
      {
        SelectOutline(outline);
      }
    }

    // 複数のオブジェクトを選択
    public void SelectObjects(GameObject[] objects)
    {
      ClearSelection();

      foreach (GameObject obj in objects)
      {
        CustomOutline outline = obj.GetComponent<CustomOutline>();
        if (outline != null)
        {
          SelectOutline(outline);
        }
      }
    }

    // アウトラインを選択
    public void SelectOutline(CustomOutline outline)
    {
      if (outline == null) return;

      if (!selectedOutlines.Contains(outline))
      {
        selectedOutlines.Add(outline);
      }

      if (enableSelectionOutline)
      {
        outline.SetOutlineColor(selectionOutlineColor);
        outline.enabled = true;
      }
    }

    // 選択をクリア
    public void ClearSelection()
    {
      foreach (CustomOutline outline in selectedOutlines)
      {
        if (outline != null)
        {
          if (enableGlobalOutline)
          {
            outline.SetOutlineColor(defaultOutlineColor);
            outline.SetOutlineWidth(defaultOutlineWidth);
            outline.enabled = true;
          }
          else
          {
            outline.enabled = false;
          }
        }
      }

      selectedOutlines.Clear();
    }

    // すべてのアウトラインを更新
    private void UpdateAllOutlines()
    {
      foreach (CustomOutline outline in allOutlines)
      {
        if (outline != null && !selectedOutlines.Contains(outline))
        {
          if (enableGlobalOutline)
          {
            outline.SetOutlineColor(defaultOutlineColor);
            outline.SetOutlineWidth(defaultOutlineWidth);
            outline.enabled = true;
          }
          else
          {
            outline.enabled = false;
          }
        }
      }
    }

    // 選択されたアウトラインを更新
    private void UpdateSelectionOutlines()
    {
      foreach (CustomOutline outline in selectedOutlines)
      {
        if (outline != null)
        {
          if (enableSelectionOutline)
          {
            outline.SetOutlineColor(selectionOutlineColor);
            outline.enabled = true;
          }
          else
          {
            if (enableGlobalOutline)
            {
              outline.SetOutlineColor(defaultOutlineColor);
              outline.SetOutlineWidth(defaultOutlineWidth);
              outline.enabled = true;
            }
            else
            {
              outline.enabled = false;
            }
          }
        }
      }
    }

    // すべてのアウトラインを取得
    public List<CustomOutline> GetAllOutlines()
    {
      return new List<CustomOutline>(allOutlines);
    }

    // 選択されたアウトラインを取得
    public List<CustomOutline> GetSelectedOutlines()
    {
      return new List<CustomOutline>(selectedOutlines);
    }

    // エディタでの変更を反映
    void OnValidate()
    {
      if (Application.isPlaying)
      {
        UpdateAllOutlines();
        UpdateSelectionOutlines();
      }
    }
  }
}
