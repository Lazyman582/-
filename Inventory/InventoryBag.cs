using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [已重构] 背包存档容器 — ScriptableObject 用于保存/加载背包数据
/// 运行时管理请用 InventoryManager（MonoBehaviour 单例）
/// </summary>
[CreateAssetMenu(fileName = "New InventoryBag", menuName = "Inventory/Inventory Bag (存档)")]
public class InventoryBag : ScriptableObject
{
    public List<InventorySlot> bagList = new List<InventorySlot>();
}
