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

    /// <summary>是否有 UI 正在拦截玩家输入（背包等打开时，游戏操作应被屏蔽）</summary>
    public static bool IsUIBlockingInput { get;  set; }

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
        IsUIBlockingInput = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        Time.timeScale = 0f;

        // 屏蔽玩家操作，防止背包打开时按键触发移动/攻击等状态
        if (UserInput.Instance != null)
            UserInput.Instance.stop = true;

        // 立即停掉循环音效（移动音效等）
        AudioManager.Instance?.StopAllLoops();

        // 通知 View 刷新
        if (inventoryView != null) inventoryView.OnPanelOpened();
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        IsUIBlockingInput = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        Time.timeScale = 1f;

        // 恢复玩家操作
        if (UserInput.Instance != null)
            UserInput.Instance.stop = false;
    }
}
