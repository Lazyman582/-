using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 物品详情视图 — 订阅 InventoryView.OnSelectionChanged 驱动刷新
/// 参照 HP_view 的订阅模式：OnEnable 绑定 / OnDisable 解绑
/// </summary>
[DefaultExecutionOrder(-400)]
public class ItemDetailView : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text typeText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text countText;
    [SerializeField] private Text effectText;  // 消耗品效果描述
    [SerializeField] private GameObject emptyHint; // 未选中任何物品时的提示

    private InventoryView inventoryView;

    // ==================== Unity 生命周期 ====================

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindInventoryView();
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindInventoryView();
    }

    // ==================== 绑定 / 解绑 ====================

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindInventoryView();
        RefreshDisplay();
    }

    private void BindInventoryView()
    {
        UnbindInventoryView();

        inventoryView = FindObjectOfType<InventoryView>();
        if (inventoryView != null)
        {
            inventoryView.OnSelectionChanged -= HandleSelectionChanged;
            inventoryView.OnSelectionChanged += HandleSelectionChanged;
        }
    }

    private void UnbindInventoryView()
    {
        if (inventoryView != null)
        {
            inventoryView.OnSelectionChanged -= HandleSelectionChanged;
            inventoryView = null;
        }
    }

    // ==================== 数据 → UI ====================

    private void HandleSelectionChanged(InventorySlot slot)
    {
        RefreshDisplay();
    }

    /// <summary>
    /// 从 InventoryView 拉取当前选中格子，刷新详情面板
    /// </summary>
    private void RefreshDisplay()
    {
        if (inventoryView == null)
            inventoryView = FindObjectOfType<InventoryView>();

        var slot = inventoryView?.GetSelectedSlot();

        if (slot != null && slot.itemConfig != null && slot.count > 0)
        {
            var cfg = slot.itemConfig;

            if (iconImage != null)
            {
                iconImage.sprite = cfg.Icon;
                iconImage.enabled = true;
            }
            if (nameText != null)    nameText.text = cfg.ItemName;
            if (typeText != null)   typeText.text = TypeLabel(cfg.ItemType);
            if (descriptionText != null) descriptionText.text = cfg.Description;
            if (countText != null)  countText.text = $"持有: {slot.count}";
            if (effectText != null) effectText.text = EffectLabel(cfg);

            if (emptyHint != null) emptyHint.SetActive(false);
        }
        else
        {
            // 空格子或背包为空
            if (iconImage != null)  iconImage.enabled = false;
            if (nameText != null)   nameText.text = "";
            if (typeText != null)   typeText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (countText != null)  countText.text = "";
            if (effectText != null) effectText.text = "";

            if (emptyHint != null) emptyHint.SetActive(true);
        }
    }

    private static string TypeLabel(ItemType type)
    {
        return type switch
        {
            ItemType.Consumable => "消耗品",
            ItemType.Weapon     => "武器",
            ItemType.Armor      => "防具",
            ItemType.Quest      => "任务物品",
            ItemType.Material   => "材料",
            _                   => type.ToString()
        };
    }

    private static string EffectLabel(ItemConfig cfg)
    {
        if (cfg.ItemType != ItemType.Consumable ||
            cfg.EffectType == ConsumableEffectType.None ||
            cfg.EffectValue <= 0)
            return "";

        string name = cfg.EffectType switch
        {
            ConsumableEffectType.HealHP    => "恢复生命",
            ConsumableEffectType.RestoreMP => "恢复法力",
            ConsumableEffectType.AddCoin   => "获得金币",
            _                              => cfg.EffectType.ToString()
        };

        return $"效果: {name} +{cfg.EffectValue}";
    }
}
