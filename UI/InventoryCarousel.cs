using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包滚动栏 — 接入 InventoryManager 数据的轮播选择器
/// 滑动时同时过渡位置、大小、透明度
/// </summary>
[DefaultExecutionOrder(-450)]
public class InventoryCarousel : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private RectTransform centerSlot;

    [Header("Preview Prefab")]
    [SerializeField] private GameObject previewPrefab;

    [Header("Data")]
    [Tooltip("只读取背包前 N 个格子")]
    [SerializeField] private int maxSlots = 6;

    [Header("Layout")]
    [SerializeField] private float verticalSpacing = 120f;
    [SerializeField] private float previewScale = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float previewAlpha = 0.4f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;

    [Header("Input")]
    float scroll;
    [SerializeField] private KeyCode keyUse = KeyCode.KeypadEnter;

    /// <summary>选中物品变更时触发，供详情面板订阅</summary>
    public event System.Action<InventorySlot> OnSelectionChanged;

    // ==================== 内部结构 ====================

    private struct Slot
    {
        public RectTransform rect;
        public Image[] images;   // 所有 Image（调透明度用）
        public Image icon;       // Icon 子节点（物品图）
        public Text countText;   // 数量文本
    }

    private Slot top, center, bottom;
    private float topY, centerY, bottomY;
    private bool animating;
    private int currentIndex;

    // ==================== 初始化 ====================

    private void Awake()
    {
        if (centerSlot == null)
        {
            enabled = false;
            return;
        }

        centerY = centerSlot.anchoredPosition.y;
        topY    = centerY + verticalSpacing;
        bottomY = centerY - verticalSpacing;

        center = BuildSlot(centerSlot);

        if (previewPrefab != null)
        {
            var parent = centerSlot.parent;
            top    = CreateSlot(parent, topY,    "TopPreview");
            bottom = CreateSlot(parent, bottomY, "BottomPreview");
        }
    }

    private void Start()
    {
        LoadContent();
        UpdateSelectionEvent();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        // 数据变了，夹紧索引后刷新内容
        var data = GetData();
        if (data.Count == 0) { currentIndex = 0; }
        else if (currentIndex >= data.Count) { currentIndex = data.Count - 1; }

        LoadContent();
        UpdateSelectionEvent();
    }

    private Slot BuildSlot(RectTransform rect)
    {
        return new Slot
        {
            rect      = rect,
            images    = rect.GetComponentsInChildren<Image>(),
            icon      = rect.Find("Icon")?.GetComponent<Image>(),
            countText = rect.GetComponentInChildren<Text>()
        };
    }

    private Slot CreateSlot(Transform parent, float y, string name)
    {
        var go = Instantiate(previewPrefab, parent);
        go.name = name;

        var self = go.GetComponent<InventoryCarousel>();
        if (self != null) Destroy(self);

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(centerSlot.anchoredPosition.x, y);
        rect.localScale = Vector3.one * previewScale;

        var slot = BuildSlot(rect);
        SetAlpha(slot.images, previewAlpha);
        return slot;
    }

    // ==================== 输入 ====================

    private void Update()
    {
        
        scroll = Input.GetAxis("Mouse ScrollWheel");
        if (animating) return;
        // 对话中不响应
        if (DialogueController.Instance != null && DialogueController.Instance.IsActive) return;
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (scroll > 0)
                Scroll(1);
            else if (scroll < 0)
                Scroll(-1);
          
        }
        if (Input.GetKeyDown(keyUse)) {

            UseCurrentItem();
        
        }
    }

    // ==================== 滑动动画 ====================

    private void Scroll(int direction)
    {
        var data = GetData();
        if (data == null) return;

        // 先更新索引（空背包时保持 index=0，动画照样播）
        if (data.Count > 0)
            currentIndex = (currentIndex - direction + data.Count) % data.Count;

        StartCoroutine(AnimateScroll(direction));
    }

    private IEnumerator AnimateScroll(int direction)
    {
        animating = true;

        GetTargets(direction,
            out float topToY,    out float topToScale,    out float topToAlpha,
            out float centerToY, out float centerToScale, out float centerToAlpha,
            out float bottomToY, out float bottomToScale, out float bottomToAlpha);

        float topY0    = top.rect.anchoredPosition.y;
        float centerY0 = center.rect.anchoredPosition.y;
        float bottomY0 = bottom.rect.anchoredPosition.y;

        float topS0    = top.rect.localScale.x;
        float centerS0 = center.rect.localScale.x;
        float bottomS0 = bottom.rect.localScale.x;

        float topA0    = top.images.Length > 0 ? top.images[0].color.a : previewAlpha;
        float centerA0 = 1f;
        float bottomA0 = bottom.images.Length > 0 ? bottom.images[0].color.a : previewAlpha;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            SetY(top.rect,    Mathf.Lerp(topY0,    topToY,    t));
            SetY(center.rect, Mathf.Lerp(centerY0, centerToY, t));
            SetY(bottom.rect, Mathf.Lerp(bottomY0, bottomToY, t));

            top.rect.localScale    = Vector3.one * Mathf.Lerp(topS0,    topToScale,    t);
            center.rect.localScale = Vector3.one * Mathf.Lerp(centerS0, centerToScale, t);
            bottom.rect.localScale = Vector3.one * Mathf.Lerp(bottomS0, bottomToScale, t);

            SetAlpha(top.images,    Mathf.Lerp(topA0,    topToAlpha,    t));
            SetAlpha(center.images, Mathf.Lerp(centerA0, centerToAlpha, t));
            SetAlpha(bottom.images, Mathf.Lerp(bottomA0, bottomToAlpha, t));

            yield return null;
        }

        SnapToRest();
        LoadContent();
        UpdateSelectionEvent();

        animating = false;
    }

    private void GetTargets(int dir,
        out float tY, out float tS, out float tA,
        out float cY, out float cS, out float cA,
        out float bY, out float bS, out float bA)
    {
        if (dir == 1) // S: top→center→bottom→消失
        {
            tY = centerY;   tS = 1f;            tA = 1f;
            cY = bottomY;   cS = previewScale;  cA = previewAlpha;
            bY = bottomY - verticalSpacing;  bS = 0f;  bA = 0f;
        }
        else // W: bottom→center→top→消失
        {
            tY = topY + verticalSpacing;  tS = 0f;  tA = 0f;
            cY = topY;      cS = previewScale;  cA = previewAlpha;
            bY = centerY;   bS = 1f;            bA = 1f;
        }
    }

    private void SnapToRest()
    {
        SetY(top.rect,    topY);
        SetY(center.rect, centerY);
        SetY(bottom.rect, bottomY);

        top.rect.localScale    = Vector3.one * previewScale;
        center.rect.localScale = Vector3.one;
        bottom.rect.localScale = Vector3.one * previewScale;

        SetAlpha(top.images,    previewAlpha);
        SetAlpha(center.images, 1f);
        SetAlpha(bottom.images, previewAlpha);
    }

    // ==================== 数据加载 ====================

    /// <summary>只取背包前 maxSlots 个格子</summary>
    private System.Collections.Generic.List<InventorySlot> GetData()
    {
        var all = InventoryManager.Instance?.slots;
        if (all == null || all.Count == 0)
            return new System.Collections.Generic.List<InventorySlot>();

        var result = new System.Collections.Generic.List<InventorySlot>();
        for (int i = 0; i < all.Count && i < maxSlots; i++) {

            if (all[i].itemConfig == null) {

                continue;

            }

            result.Add(all[i]);
        }
        return result;
    }

    private void LoadContent()
    {
        var data = GetData();

        LoadSlot(top,    data, currentIndex - 1);
        LoadSlot(center, data, currentIndex);
        LoadSlot(bottom, data, currentIndex + 1);
        
    }

    private void LoadSlot(Slot slot, System.Collections.Generic.List<InventorySlot> data, int index)
    {
        if (data.Count > 0)
            index = (index % data.Count + data.Count) % data.Count;

        // 三条件全满足才算"有物品"：索引有效 + 配置非空 + 数量 > 0
        bool hasItem = data.Count > 0
                       && index >= 0 && index < data.Count
                       && data[index].itemConfig != null
                       && data[index].count > 0;

        var itemSlot = hasItem ? data[index] : null;
    
        if (slot.icon != null)
        {
           
            slot.icon.sprite = itemSlot?.itemConfig?.Icon;
            slot.icon.enabled = hasItem;
        }
        if (slot.countText != null)
        {
            slot.countText.text = hasItem && itemSlot.count > 1
                ? itemSlot.count.ToString() : "";
        }
    }

    private void UpdateSelectionEvent()
    {
        OnSelectionChanged?.Invoke(GetSelectedSlot());
    }

    private void UseCurrentItem()
    {
        var slot = GetSelectedSlot();
        if (slot == null) return;
        InventoryManager.Instance?.UseItem(slot);
    }

    /// <summary>获取当前选中（中心格）的 InventorySlot</summary>
    public InventorySlot GetSelectedSlot()
    {
        var data = GetData();
        if (data == null) return null ;
        if (data.Count == 0 || currentIndex < 0 || currentIndex >= data.Count)
            return null;
        return data[currentIndex];

    }

    /// <summary>强制刷新显示</summary>
    public void ForceRefresh()
    {
        LoadContent();
        UpdateSelectionEvent();
    }

    // ==================== 工具方法 ====================

    private static void SetY(RectTransform rect, float y)
    {
        var pos = rect.anchoredPosition;
        pos.y = y;
        rect.anchoredPosition = pos;
    }

    private static void SetAlpha(Image[] images, float alpha)
    {
        if (images == null) return;
        foreach (var img in images)
        {
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}
