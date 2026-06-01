using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIList : MonoBehaviour
{
    [Tooltip("列表项的父对象，所有子对象视为列表项")]
    public Transform content; // 列表项的父物体

    public RectTransform selectionMarker; // 小三角形指示标

    public float smoothFactor = 0.25f; // 移动平滑度

    public bool loopSelection = true; // 是否循环选择

    [SerializeField]
    private List<RectTransform> items = new List<RectTransform>(); // 存储所有列表项的引用
    [SerializeField]
    private int selectedIndex = 0; // 当前选中项的索引
    void Start()
    {
        if (content == null)
        {
            Debug.LogError("UIListController: Content is not assigned!");
            enabled = false;
            return;
        }

        if (selectionMarker == null)
        {
            Debug.LogError("UIListController: SelectionMarker is not assigned!");
            enabled = false;
            return;
        }

        // 读取所有子对象作为列表项
        foreach (Transform child in content)
        {
            // 只添加激活状态的 UI 元素
            if (child.gameObject.activeSelf)
                items.Add(child.GetComponent<RectTransform>());
        }

        // 确保至少有一个子对象
        if (items.Count == 0)
        {
            Debug.LogError("UIListController: No child items found under Content!");
            enabled = false;
            return;
        }


        selectedIndex = 0;
        UpdateSelectionVisual();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            SelectPrevious();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SelectNext();
        }

        
      
       
    }
    void SelectNext()
    {
        selectedIndex++;
        if (selectedIndex >= items.Count)
        {
            if (loopSelection) selectedIndex = 0; // 循环到第一个
            else selectedIndex = items.Count - 1; // 保持在最后一个
        }
        UpdateSelectionVisual();
    }
    void SelectPrevious()
    {
        selectedIndex--;
        if (selectedIndex < 0)
        {
            if (loopSelection) selectedIndex = items.Count - 1; // 循环到最后一个
            else selectedIndex = 0; // 保持在第一个
        }
        UpdateSelectionVisual();
    }
    void UpdateSelectionVisual()
    {
        // 瞬间将指示标移动到选中项位置
        Vector3 targetPos = items[selectedIndex].anchoredPosition;
        targetPos.x -= -350.9218f-1147f;
        targetPos.y -= -284.0579f+463f;
        selectionMarker.anchoredPosition = targetPos;
    }
    }

