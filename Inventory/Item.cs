using UnityEngine;

/// <summary>
/// [已废弃] 旧版物品类，请使用 ItemConfig（配置）+ InventorySlot（运行时数量）替代
/// 保留此文件仅为兼容旧资产，新代码请用 ItemConfig
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/New Item (旧版)")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite ItemSprite;
    public int itemHeld = 1;
    [TextArea]
    public string itemInfo;
}
