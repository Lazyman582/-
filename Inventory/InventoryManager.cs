using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 背包管理器 — 运行时单例，管理背包格子列表
/// 从 Resources/ItemConfigs 自动加载所有物品配置
/// </summary>
public class InventoryManager : Singleton<InventoryManager>
{
    /// <summary>背包内容变更时触发，供 UI 等模块订阅刷新</summary>
    public event Action OnInventoryChanged;

    /// <summary>当前背包所有格子</summary>
    public List<InventorySlot> slots = new List<InventorySlot>();

    /// <summary>背包最大格数（0 或负数表示无上限）</summary>
    [SerializeField] private int maxSlots = 20;

    /// <summary>背包最大格数</summary>
    public int MaxSlots
    {
        get => maxSlots;
        set => maxSlots = Mathf.Max(0, value);
    }

    /// <summary>背包是否已满</summary>
    public bool IsFull => maxSlots > 0 && slots.Count >= maxSlots;

    /// <summary>已加载的全部物品配置（从 Resources 读取）</summary>
    private Dictionary<string, ItemConfig> configDict;

    private void  Awake()
    {
        LoadAllConfigs();
  
    }

    /// <summary>
    /// 从 Resources/ItemConfigs 加载所有 ItemConfig，建立 ID → 配置 的字典
    /// </summary>
    private void LoadAllConfigs()
    {
        configDict = new Dictionary<string, ItemConfig>();
        ItemConfig[] configs = Resources.LoadAll<ItemConfig>("ItemConfigs");
        foreach (var cfg in configs)
        {
            if (configDict.ContainsKey(cfg.ItemId))
            {
                Debug.LogWarning($"[InventoryManager] 重复 ID: {cfg.ItemId}，跳过 {cfg.name}");
                continue;
            }
            configDict.Add(cfg.ItemId, cfg);
        }
        Debug.Log($"[InventoryManager] 加载了 {configDict.Count} 个物品配置");
    }

    /// <summary>
    /// 通过 ID 获取物品配置
    /// </summary>
    public ItemConfig GetConfig(string itemId)
    {
        configDict.TryGetValue(itemId, out var cfg);
        return cfg;
    }

    /// <summary>
    /// 获取所有物品配置
    /// </summary>
    public IEnumerable<ItemConfig> AllConfigs => configDict?.Values;

    // ==================== 背包操作 ====================

    /// <summary>
    /// 添加物品到背包（自动堆叠）
    /// </summary>
    /// <returns>实际成功添加的数量（背包满时可能小于 amount）</returns>
    public int AddItem(ItemConfig config, int amount = 1)
    {
        if (config == null || amount <= 0) return 0;

        int remaining = amount;

        // 1. 先尝试叠加到已有同类型格子
        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (slot.itemConfig == config && !slot.IsFull)
            {
                int canAdd = Mathf.Min(remaining, slot.RemainingSpace);
                slot.count += canAdd;
                remaining -= canAdd;
            }
        }

        // 2. 剩余的开新格子（受 maxSlots 限制）
        while (remaining > 0)
        {
            if (maxSlots > 0 && slots.Count >= maxSlots)
            {
                Debug.LogWarning($"[InventoryManager] 背包已满 ({maxSlots} 格)，无法放入剩余 {remaining} 个 {config.ItemName}");
                break;
            }
            int stack = Mathf.Min(remaining, config.MaxStack);
            slots.Add(new InventorySlot(config, stack));
            remaining -= stack;
        }

        if (amount - remaining > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        return amount - remaining;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    /// <returns>实际移除的数量（背包中不足时移除所有）</returns>
    public int RemoveItem(ItemConfig config, int amount = 1)
    {
        if (config == null || amount <= 0) return 0;

        int remaining = amount;

        // 从后往前遍历，方便删除空格子
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (remaining <= 0) break;
            if (slots[i].itemConfig == config)
            {
                int toRemove = Mathf.Min(remaining, slots[i].count);
                slots[i].count -= toRemove;
                remaining -= toRemove;

                if (slots[i].count <= 0)
                    slots.RemoveAt(i);
            }
        }

        int removed = amount - remaining;
        if (removed > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        return removed;
    }

    /// <summary>
    /// 通过 ID 添加物品
    /// </summary>
    public int AddItemById(string itemId, int amount = 1)
    {
        var cfg = GetConfig(itemId);
        if (cfg == null)
        {
            Debug.LogWarning($"[InventoryManager] 未找到物品配置: {itemId}");
            return 0;
        }
        return AddItem(cfg, amount);
    }

    /// <summary>
    /// 查询某物品在背包中的总数量
    /// </summary>
    public int GetItemCount(ItemConfig config)
    {
        if (config == null) return 0;
        return slots.Where(s => s.itemConfig == config).Sum(s => s.count);
    }

    /// <summary>
    /// 通过 ID 查询数量
    /// </summary>
    public int GetItemCountById(string itemId)
    {
        var cfg = GetConfig(itemId);
        return cfg != null ? GetItemCount(cfg) : 0;
    }

    /// <summary>
    /// 检查是否持有足够数量的物品
    /// </summary>
    public bool HasItem(ItemConfig config, int amount = 1)
    {
        return GetItemCount(config) >= amount;
    }

    /// <summary>背包总格数</summary>
    public int SlotCount => slots.Count;

    /// <summary>背包不重复物品种类数</summary>
    public int UniqueItemCount => slots.Count(s => s.count > 0);

    /// <summary>手动通知 UI 刷新（存档恢复后调用）</summary>
    public void NotifyChanged() => OnInventoryChanged?.Invoke();

    // ==================== 交换 & 移动 ====================

    /// <summary>
    /// 交换或移动两个位置的物品。任一位置超出列表视为空格子。
    /// </summary>
    public void SwapOrMove(int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0 || indexA == indexB) return;

        // 确保两个位置都在列表范围内（空格子用 null 填充）
        int max = Mathf.Max(indexA, indexB);
        while (slots.Count <= max)
            slots.Add(new InventorySlot(null, 0));

        // 交换
        var temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        // 清理尾部空槽
        while (slots.Count > 0 && slots[slots.Count - 1].itemConfig == null)
            slots.RemoveAt(slots.Count - 1);

        OnInventoryChanged?.Invoke();
    }

    // ==================== 物品使用 ====================

    /// <summary>
    /// 使用指定格子的物品 — 施加效果并扣除数量
    /// </summary>
    public void UseItem(InventorySlot slot)
    {
        if (slot == null || slot.itemConfig == null || slot.count <= 0) return;

        var cfg = slot.itemConfig;
        Debug.Log($"[InventoryManager] 使用物品: {cfg.ItemName} ({cfg.ItemType})");

        switch (cfg.ItemType)
        {
            case ItemType.Consumable:
                ApplyConsumableEffects(cfg);
                RemoveItem(cfg, 1);
                break;
            default:
                Debug.Log($"[InventoryManager] {cfg.ItemType} 类型暂无使用效果");
                break;
        }
    }

    private void ApplyConsumableEffects(ItemConfig cfg)
    {
        if (cfg.EffectType == ConsumableEffectType.None || cfg.EffectValue <= 0) return;

        var player = PersistentPlayer.Instance != null
            ? PersistentPlayer.Instance.CharacterData
            : FindObjectOfType<CharacterData>();

        switch (cfg.EffectType)
        {
            case ConsumableEffectType.HealHP:
                if (player != null)
                {
                    player.Recover(cfg.EffectValue);
                    Debug.Log($"[InventoryManager] 恢复 {cfg.EffectValue} HP");
                }
                break;

            case ConsumableEffectType.RestoreMP:
                // TODO: 接入法力系统后实现
                Debug.LogWarning($"[InventoryManager] 法力系统尚未实现，{cfg.ItemName} 无效");
                break;

            case ConsumableEffectType.AddCoin:
                if (player != null)
                {
                    player.Coin += cfg.EffectValue;
                    Debug.Log($"[InventoryManager] 获得 {cfg.EffectValue} 金币");
                }
                break;
        }
    }

    /// <summary>
    /// 输出当前背包内容到控制台（供调试使用）
    /// </summary>
    public string DumpInventory()
    {
        if (slots.Count == 0) return "背包为空";

        var lines = new List<string>
        {
            $"===== 背包内容 ({SlotCount} 格, {UniqueItemCount} 种) ====="
        };
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            lines.Add($"  [{i}] {s.itemConfig.ItemName} ({s.itemConfig.ItemId}) x{s.count}/{s.itemConfig.MaxStack}");
        }
        return string.Join("\n", lines);
    }



   
}
