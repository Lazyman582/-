using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 背包UI视图 — 订阅 InventoryManager.OnInventoryChanged 驱动刷新
/// 不负责面板开关或 timeScale，那些归 UIManager 管
/// </summary>
[DefaultExecutionOrder(-500)]
public class InventoryView : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("背包根面板，用于判断当前是否可见")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slot Container")]
    [Tooltip("包含 GridLayoutGroup 的父物体")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private int maxVisibleSlots = 20;

    [Header("Selection")]
    [SerializeField] private RectTransform selectionMarker;
    [SerializeField] private bool loopSelection = true;
    [SerializeField] private int columns = 5;

    [Header("Slot Prefab (动态生成用)")]
    [Tooltip("若不为空，启动时按 maxVisibleSlots 动态创建；若为空，从 slotContainer 子物体中收集")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode keyUp = KeyCode.W;
    [SerializeField] private KeyCode keyDown = KeyCode.S;
    [SerializeField] private KeyCode keyLeft = KeyCode.A;
    [SerializeField] private KeyCode keyRight = KeyCode.D;
    [SerializeField] private KeyCode keyUse = KeyCode.E;

    /// <summary>缓存的格子 UI 组件列表</summary>
    private List<SlotUI> slotUIs = new List<SlotUI>();

    /// <summary>选中格子变更时触发，供详情面板等模块订阅</summary>
    public event System.Action<InventorySlot> OnSelectionChanged;

    /// <summary>当前选中格子索引</summary>
    private int selectedIndex = 0;

    // ==================== 格子数据结构 ====================

    [System.Serializable]
    private class SlotUI
    {
        public RectTransform rect;
        public Image background;
        public Image icon;
        public Text countText;
        public GameObject emptyHint;

        public void SetItem(ItemConfig config, int count)
        {
            if (config != null && count > 0)
            {
                icon.sprite = config.Icon;
                icon.enabled = true;
                countText.text = count > 1 ? count.ToString() : "";
                if (emptyHint != null) emptyHint.SetActive(false);
            }
            else
            {
                Clear();
            }
        }

        public void Clear()
        {
            icon.sprite = null;
            icon.enabled = false;
            countText.text = "";
            if (emptyHint != null) emptyHint.SetActive(true);
        }
    }

    // ==================== Unity 生命周期 ====================

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindInventory();
    }

    private void Start()
    {
        BuildSlotCache();
        RefreshAllSlots();
        UpdateSelectionVisual();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindInventory();
    }

    // ==================== 绑定 / 解绑 ====================

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindInventory();
        BuildSlotCache();
        RefreshAllSlots();
        UpdateSelectionVisual();
    }

    private void BindInventory()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void UnbindInventory()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    // ==================== 格子缓存 ====================

    private void BuildSlotCache()
    {
        slotUIs.Clear();

        if (slotContainer == null)
        {
            Debug.LogError("[InventoryView] slotContainer 未赋值！");
            return;
        }

        // 动态创建
        if (slotPrefab != null && slotContainer.childCount == 0)
        {
            for (int i = 0; i < maxVisibleSlots; i++)
            {
                Instantiate(slotPrefab, slotContainer).name = $"Slot_{i}";
            }
        }

        // 收集子物体
        int count = 0;
        foreach (Transform child in slotContainer)
        {
            if (!child.gameObject.activeSelf) continue;
            if (count >= maxVisibleSlots) break;

            Image bg = child.GetComponent<Image>();
            Image itemIcon = child.Find("Icon")?.GetComponent<Image>();

            if (itemIcon == null)
            {
                Debug.LogWarning($"[InventoryView] 格子 {child.name} 缺少 Icon 子节点（需挂 Image），跳过");
                continue;
            }

            var slotUI = new SlotUI
            {
                rect = child.GetComponent<RectTransform>(),
                background = bg,
                icon = itemIcon,
                countText = child.GetComponentInChildren<Text>(),
                emptyHint = child.Find("Empty")?.gameObject
            };

            slotUIs.Add(slotUI);
            count++;
        }

        if (slotUIs.Count > 0 && selectedIndex >= slotUIs.Count)
        {
            selectedIndex = 0;
        }

        Debug.Log($"[InventoryView] 缓存了 {slotUIs.Count} 个格子");
    }

    // ==================== 数据 → UI ====================

    private void HandleInventoryChanged()
    {
        RefreshAllSlots();
    }

    private void RefreshAllSlots()
    {
        var dataSlots = InventoryManager.Instance != null
            ? InventoryManager.Instance.slots
            : null;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (dataSlots != null && i < dataSlots.Count)
            {
                var s = dataSlots[i];
                slotUIs[i].SetItem(s.itemConfig, s.count);
            }
            else
            {
                slotUIs[i].Clear();
            }
        }

        OnSelectionChanged?.Invoke(GetSelectedSlot());
    }

    // ==================== 输入 & 导航 ====================

    private void Update()
    {
        // 对话中不响应任何按键
        if (DialogueController.Instance != null && DialogueController.Instance.IsActive) return;
        // 面板不可见时不响应导航
        if (panelRoot != null && !panelRoot.activeInHierarchy) return;
        if (slotUIs.Count == 0) return;

        int row = selectedIndex / columns;
        int col = selectedIndex % columns;
        int totalRows = (slotUIs.Count + columns - 1) / columns;

        if (Input.GetKeyDown(keyUp))
        {
            int newRow = row - 1;
            if (newRow < 0) newRow = loopSelection ? totalRows - 1 : 0;
            selectedIndex = Mathf.Clamp(newRow * columns + col, 0, slotUIs.Count - 1);
            if (newRow * columns + col >= slotUIs.Count)
                selectedIndex = slotUIs.Count - 1;
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(keyDown))
        {
            int newRow = row + 1;
            if (newRow >= totalRows) newRow = loopSelection ? 0 : totalRows - 1;
            int target = newRow * columns + col;
            if (target >= slotUIs.Count)
                target = loopSelection ? col : slotUIs.Count - 1;
            selectedIndex = target;
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(keyLeft))
        {
            int newCol = col - 1;
            if (newCol < 0)
            {
                if (loopSelection) { newCol = columns - 1; row = row > 0 ? row - 1 : totalRows - 1; }
                else newCol = 0;
            }
            int target = row * columns + newCol;
            if (target >= slotUIs.Count || target < 0)
                target = slotUIs.Count - 1;
            selectedIndex = target;
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(keyRight))
        {
            int newCol = col + 1;
            if (newCol >= columns)
            {
                if (loopSelection) { newCol = 0; row = (row + 1) % totalRows; }
                else newCol = columns - 1;
            }
            int target = row * columns + newCol;
            if (target >= slotUIs.Count)
                target = loopSelection ? 0 : slotUIs.Count - 1;
            selectedIndex = target;
            UpdateSelectionVisual();
        }
        else if (Input.GetKeyDown(keyUse))
        {
            OnUseSelected();
        }
    }

    private void UpdateSelectionVisual()
    {
        if (selectionMarker == null || slotUIs.Count == 0) return;
        if (selectedIndex < 0 || selectedIndex >= slotUIs.Count) return;

        var targetRect = slotUIs[selectedIndex].rect;
        if (targetRect == null) return;

        selectionMarker.SetParent(targetRect, false);
        selectionMarker.anchorMin = Vector2.zero;
        selectionMarker.anchorMax = Vector2.one;
        selectionMarker.anchoredPosition = Vector2.zero;
        selectionMarker.sizeDelta = Vector2.zero;

        // 如果当前有拾取的格子，也刷新它的高亮
        if (pickedIndex.HasValue && pickedIndex.Value < slotUIs.Count)
            HighlightPicked(pickedIndex.Value);

        OnSelectionChanged?.Invoke(GetSelectedSlot());
    }

    // ==================== 物品使用 / 拾取 / 交换 ====================

    /// <summary>Enter 按下：拾取 → 交换 → 取消，三态循环</summary>
    private void OnUseSelected()
    {
        var manager = InventoryManager.Instance;
        if (manager == null) return;

        // 没拾取 → 尝试拾取当前格
        if (!pickedIndex.HasValue)
        {
            // 当前格为空 → 不拾取
            if (IsSelectedSlotEmpty())
                return;

            PickUp(selectedIndex);
            return;
        }

        // 已有拾取 → 交换或取消
        if (pickedIndex.Value == selectedIndex )
        {
            CancelPickUp();
        }
        else
        {
            manager.SwapOrMove(pickedIndex.Value, selectedIndex);
            CancelPickUp();
        }
    }

    /// <summary>当前选中格是否为空（无物品）</summary>
    private bool IsSelectedSlotEmpty()
    {
        var manager = InventoryManager.Instance;
        if (manager == null) return true;

        // 索引超出数据列表 → 空
        if (selectedIndex >= manager.slots.Count) return true;

        // 数据存在但物品为空或数量为0 → 空
        var slot = manager.slots[selectedIndex];
        return slot == null || slot.itemConfig == null || slot.count <= 0;
    }

    private int? pickedIndex = null;
    private Color?[] pickedOriginalColors = null; // 恢复用

    private void PickUp(int index)
    {
        pickedIndex = index;

        if (pickedOriginalColors == null || pickedOriginalColors.Length != slotUIs.Count)
            pickedOriginalColors = new Color?[slotUIs.Count];

        if (index < slotUIs.Count && slotUIs[index].background != null)
        {
            pickedOriginalColors[index] = slotUIs[index].background.color;
            slotUIs[index].background.color = Color.yellow; // 高亮
        }
    }

    private void CancelPickUp()
    {
        if (pickedIndex.HasValue)
            RestorePickedColor(pickedIndex.Value);

        pickedIndex = null;
    }

    private void HighlightPicked(int index)
    {
        // 选中框移开后保持高亮（不做额外操作，PickUp 时已设好颜色）
    }

    private void RestorePickedColor(int index)
    {
        if (index < 0 || index >= slotUIs.Count) return;
        if (pickedOriginalColors != null && pickedOriginalColors[index].HasValue)
        {
            var bg = slotUIs[index].background;
            if (bg != null) bg.color = pickedOriginalColors[index].Value;
            pickedOriginalColors[index] = null;
        }
    }

    // ==================== 公开方法 ====================

    public InventorySlot GetSelectedSlot()
    {
        var manager = InventoryManager.Instance;
        if (manager == null) return null;
        if (selectedIndex >= manager.slots.Count) return null;
        return manager.slots[selectedIndex];
    }

    public ItemConfig GetSelectedItem()
    {
        var manager = InventoryManager.Instance;
        if (manager == null) return null;
        if (selectedIndex >= manager.slots.Count) return null;
        return manager.slots[selectedIndex].itemConfig;
    }

    public void ForceRefresh()
    {
        RefreshAllSlots();
        UpdateSelectionVisual();
    }

    /// <summary>面板打开时调用，刷新显示</summary>
    public void OnPanelOpened()
    {
        RefreshAllSlots();
        UpdateSelectionVisual();
    }
}
