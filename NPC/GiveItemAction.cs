using UnityEngine;

/// <summary>
/// 给予物品 — 对话结束后往背包添加指定物品
/// Create > NPC > Give Item Action
/// </summary>
[CreateAssetMenu(fileName = "GiveItem", menuName = "NPC/Give Item Action")]
public class GiveItemAction : DialogueAction
{
    [SerializeField] private string itemId;
    [SerializeField] private int amount = 1;

    public override void Execute()
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[GiveItemAction] itemId 为空");
            return;
        }

        int added = InventoryManager.Instance.AddItemById(itemId, amount);
        Debug.Log($"[GiveItemAction] 给予 {itemId} x{added}");
    }
}
