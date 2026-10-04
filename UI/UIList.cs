using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单指示标选择系统：
/// W/S（或 ↑/↓）在列表项之间移动指示标，Enter/Space 确认当前选中项。
/// 确认时会调用选中项上的 Button.onClick——菜单项需要挂 Button 组件并在 OnClick 里接线。
/// </summary>
public class UIList : MonoBehaviour
{
    [Tooltip("列表项的父容器，其下所有激活状态的子物体按层级顺序成为列表项")]
    public Transform content;

    [Tooltip("指示标（小箭头/高亮图片）")]
    public RectTransform selectionMarker;

    [Tooltip("指示标相对选中项锚点的偏移。\n若指示标是 content 的子物体，可设为小偏移（如 -150, 0）；\n若指示标保持在原父物体下，保持默认值 (1497.92, -178.94) 即可维持旧对位")]
    public Vector2 markerOffset = new Vector2(1497.9218f, -178.9421f);

    [Tooltip("移动平滑度（预留）")]
    public float smoothFactor = 0.25f;

    [Tooltip("是否循环选择")]
    public bool loopSelection = true;

    [SerializeField]
    private List<RectTransform> items = new List<RectTransform>(); // 列表项

    [SerializeField]
    private int selectedIndex = 0; // 当前选中项序号

    /// <summary>当前选中项序号（供其他脚本读取）</summary>
    public int SelectedIndex => selectedIndex;

    void Start()
    {
        if (content == null)
        {
            Debug.LogError("UIList: Content 未赋值！");
            enabled = false;
            return;
        }

        if (selectionMarker == null)
        {
            Debug.LogError("UIList: SelectionMarker 未赋值！");
            enabled = false;
            return;
        }

        // 收集 content 下激活状态的子物体作为列表项
        foreach (Transform child in content)
        {
            if (child.gameObject.activeSelf)
            {
                items.Add(child.GetComponent<RectTransform>());
            }
        }

        if (items.Count == 0)
        {
            Debug.LogError("UIList: Content 下没有激活状态的子物体！");
            enabled = false;
            return;
        }

        selectedIndex = 0;
        UpdateSelectionVisual();
    }

    void Update()
    {
        // 移动指示标：W/S 或 ↑/↓
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            SelectPrevious();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            SelectNext();
        }

        // 确认当前选中项：Enter 或 Space
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmSelection();
        }
    }

    /// <summary>确认：触发选中项上的 Button.onClick</summary>
    private void ConfirmSelection()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count)
        {
            return;
        }

        var item = items[selectedIndex];
        // 优先取菜单项自身的 Button，其次取父物体上的（兼容按钮文字是子物体的结构）
        var button = item.GetComponent<Button>() ?? item.GetComponentInParent<Button>();

        if (button == null)
        {
            Debug.LogWarning($"UIList: 选中项 {item.name} 没有 Button 组件，无法确认。请给菜单项添加 Button 组件并接线 OnClick。");
            return;
        }

        if (!button.interactable)
        {
            Debug.LogWarning($"UIList: 选中项 {item.name} 的 Button 不可交互，已忽略确认。");
            return;
        }

        button.onClick.Invoke();
    }

    void SelectNext()
    {
        selectedIndex++;
        if (selectedIndex >= items.Count)
        {
            selectedIndex = loopSelection ? 0 : items.Count - 1; // 循环到第一个 / 停在最后一个
        }
        UpdateSelectionVisual();
    }

    void SelectPrevious()
    {
        selectedIndex--;
        if (selectedIndex < 0)
        {
            selectedIndex = loopSelection ? items.Count - 1 : 0; // 循环到最后一个 / 停在第一个
        }
        UpdateSelectionVisual();
    }

    void UpdateSelectionVisual()
    {
        // 指示标移到选中项位置（+ 可调偏移）
        selectionMarker.anchoredPosition = items[selectedIndex].anchoredPosition + markerOffset;
    }
}
