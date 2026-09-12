using System.Text;
using UnityEngine;

/// <summary>
/// GM 调试命令 — 挂载到场景中任意 GameObject
/// F1: 输出所有 ItemConfig 配置表到 Console
/// F2: 输出当前背包内容到 Console
/// F3: 添加测试物品到背包
/// </summary>
public class GMCommand : MonoBehaviour
{
#if UNITY_EDITOR
    private StringBuilder sb = new StringBuilder();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DumpItemConfigTable();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DumpInventory();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            AddTestItem();
        }
    }

    /// <summary>
    /// 输出物品配置表
    /// </summary>
    [ContextMenu("输出物品配置表")]
    private void DumpItemConfigTable()
    {
        var manager = InventoryManager.Instance;
        var configs = manager.AllConfigs;

        sb.Clear();
        sb.AppendLine("===========================================");
        sb.AppendLine("          物品配置表 (ItemConfig)");
        sb.AppendLine("===========================================");
        sb.AppendLine($"{"ID",-16} {"名称",-12} {"类型",-12} {"堆叠",-6} 描述");
        sb.AppendLine("-------------------------------------------------------------------");

        int count = 0;
        if (configs != null)
        {
            foreach (var cfg in configs)
            {
                sb.AppendLine($"{cfg.ItemId,-16} {cfg.ItemName,-12} {cfg.ItemType,-12} {cfg.MaxStack,-6} {cfg.Description}");
                count++;
            }
        }

        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"共 {count} 个物品配置");
        sb.AppendLine("===========================================");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 输出当前背包内容
    /// </summary>
    [ContextMenu("输出背包内容")]
    private void DumpInventory()
    {
        var manager = InventoryManager.Instance;
        Debug.Log(manager.DumpInventory());
    }

    /// <summary>
    /// 添加测试物品到背包
    /// </summary>
    [ContextMenu("添加测试物品")]
    private void AddTestItem()
    {
        var manager = InventoryManager.Instance;
        var configs = manager.AllConfigs;

        if (configs == null)
        {
            Debug.LogWarning("[GMCommand] 没有找到任何 ItemConfig，请先在 Resources/ItemConfigs 里创建配置！");
            return;
        }

        int addedCount = 0;
        foreach (var cfg in configs)
        {
            int added = manager.AddItem(cfg, 3);
            if (added > 0) addedCount++;
        }

        if (addedCount > 0)
        {
            Debug.Log($"[GMCommand] 已为每种物品添加 3 个到背包 ({addedCount} 种)");
            DumpInventory();
        }
        else
        {
            Debug.Log("[GMCommand] 添加失败，检查是否有 ItemConfig");
        }
    }
#endif
}
