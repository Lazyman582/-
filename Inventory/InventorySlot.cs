using System;

/// <summary>
/// 背包格子 — 运行时数据容器，记录物品配置引用 + 当前持有数量
/// 可被 JSON 序列化，用于存档系统
/// </summary>
[Serializable]
public class InventorySlot
{
    public ItemConfig itemConfig;
    public int count;

    public InventorySlot(ItemConfig config, int initialCount = 1)
    {
        itemConfig = config;
        count = Math.Max(0, initialCount);
    }

    /// <summary>该格子是否已满（达到堆叠上限）</summary>
    public bool IsFull => itemConfig != null && count >= itemConfig.MaxStack;

    /// <summary>还能放入多少个</summary>
    public int RemainingSpace => itemConfig != null ? itemConfig.MaxStack - count : 0;
}
