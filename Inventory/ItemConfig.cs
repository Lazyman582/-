using UnityEngine;

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Consumable, // 消耗品（药水、食物等）
    Weapon,     // 武器
    Armor,      // 防具
    Quest,      // 任务物品
    Material    // 材料
}

/// <summary>
/// 消耗品效果类型
/// </summary>
public enum ConsumableEffectType
{
    None,       // 无效果（装饰品/任务物品类消耗品）
    HealHP,     // 恢复生命
    RestoreMP,  // 恢复法力
    AddCoin,    // 增加金币
}

/// <summary>
/// 物品配置 — ScriptableObject，纯静态配置，不包含运行时状态（如持有数量）
/// 右键 Create > Inventory > Item Config 创建
/// </summary>
[CreateAssetMenu(fileName = "NewItemConfig", menuName = "Inventory/Item Config")]
public class ItemConfig : ScriptableObject
{
    [Header("基本信息")]
    [SerializeField] private string itemId = "item_000";
    [SerializeField] private string itemName = "新物品";
    [SerializeField] private ItemType itemType = ItemType.Material;
    [SerializeField] private Sprite icon;
    [SerializeField] [TextArea] private string description = "物品描述";
    [SerializeField] private int maxStack = 99;

    [Header("消耗品效果（仅 ItemType = Consumable 时生效）")]
    [SerializeField] private ConsumableEffectType effectType = ConsumableEffectType.None;
    [SerializeField] private int effectValue = 0;

    // ——— 只读属性 ———
    public string ItemId => itemId;
    public string ItemName => itemName;
    public ItemType ItemType => itemType;
    public Sprite Icon => icon;
    public string Description => description;
    public int MaxStack => Mathf.Max(1, maxStack);
    public ConsumableEffectType EffectType => effectType;
    public int EffectValue => effectValue;
}
