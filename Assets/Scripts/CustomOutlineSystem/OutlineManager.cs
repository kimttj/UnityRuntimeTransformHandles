using UnityEngine;
using System.Collections.Generic;

public class OutlineManager : MonoBehaviour
{
  [Header("Global Settings")]
  [SerializeField] private Color defaultOutlineColor = Color.yellow;
  [SerializeField, Range(0f, 10f)] private float defaultOutlineWidth = 2f;
  [SerializeField] private bool enableGlobalOutline = true;

  [Header("Selection Settings")]
  [SerializeField] private Color selectionOutlineColor = Color.cyan;
  [SerializeField] private bool enableSelectionOutline = true;

  private static OutlineManager instance;
  private List<CustomOutline> allOutlines = new List<CustomOutline>();
  private List<CustomOutline> selectedOutlines = new List<CustomOutline>();

  public static OutlineManager Instance
  {
    get
    {
      if (instance == null)
      {
        instance = FindObjectOfType<OutlineManager>();
        if (instance == null)
        {
          GameObject go = new GameObject("OutlineManager");
          instance = go.AddComponent<OutlineManager>();
        }
      }
      return instance;
    }
  }

  // プロパティ
  public Color DefaultOutlineColor
  {
    get { return defaultOutlineColor; }
    set
    {
      defaultOutlineColor = value;
      UpdateAllOutlines();
    }
  }

  public float DefaultOutlineWidth
  {
    get { return defaultOutlineWidth; }
    set
    {
      defaultOutlineWidth = Mathf.Clamp(value, 0f, 10f);
      UpdateAllOutlines();
    }
  }

  public bool EnableGlobalOutline
  {
    get { return enableGlobalOutline; }
    set
    {
      enableGlobalOutline = value;
      UpdateAllOutlines();
    }
  }

  public Color SelectionOutlineColor
  {
    get { return selectionOutlineColor; }
    set
    {
      selectionOutlineColor = value;
      UpdateSelectedOutlines();
    }
  }

  public bool EnableSelectionOutline
  {
    get { return enableSelectionOutline; }
    set
    {
      enableSelectionOutline = value;
      UpdateSelectedOutlines();
    }
  }

  void Awake()
  {
    if (instance == null)
    {
      instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else if (instance != this)
    {
      Destroy(gameObject);
    }
  }

  void Start()
  {
    // シーン内のすべてのCustomOutlineを登録
    RegisterAllOutlines();
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
  private void UpdateSelectedOutlines()
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

  // パブリックメソッド
  public void SetDefaultOutlineColor(Color color)
  {
    DefaultOutlineColor = color;
  }

  public void SetDefaultOutlineWidth(float width)
  {
    DefaultOutlineWidth = width;
  }

  public void SetSelectionOutlineColor(Color color)
  {
    SelectionOutlineColor = color;
  }

  public void ToggleGlobalOutline()
  {
    EnableGlobalOutline = !EnableGlobalOutline;
  }

  public void ToggleSelectionOutline()
  {
    EnableSelectionOutline = !EnableSelectionOutline;
  }

  public List<CustomOutline> GetAllOutlines()
  {
    return new List<CustomOutline>(allOutlines);
  }

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
      UpdateSelectedOutlines();
    }
  }
}
