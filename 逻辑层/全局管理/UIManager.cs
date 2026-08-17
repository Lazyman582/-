using UnityEngine;

/// <summary>
/// UI 全局管理器 — 处理面板开关、timeScale 等游戏级 UI 状态
/// 不涉及具体 UI 数据逻辑
/// </summary>
[DefaultExecutionOrder(-200)]
public class UIManager : MonoBehaviour
{
    [Header("背包面板")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private KeyCode inventoryKey = KeyCode.Tab;

    private bool isInventoryOpen = false;

    private InventoryView inventoryView;

    private void Awake()
    {
        if (inventoryPanel != null)
        {
            inventoryView = inventoryPanel.GetComponentInChildren<InventoryView>(includeInactive: true);
        }
    }

    private void Start()
    {
        // 游戏开始时关闭背包
        if (inventoryPanel != null)
            Time.timeScale = 1.0f;
            inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }
    }

    // ==================== 背包开关 ====================

    public void ToggleInventory()
    {
        if (isInventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        Time.timeScale = 0f;

        // 通知 View 刷新
        if (inventoryView != null) inventoryView.OnPanelOpened();
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
