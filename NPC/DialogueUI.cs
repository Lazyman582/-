using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueUI : Singleton<DialogueUI>
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("文本")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text lineText;

    [Header("线性推进")]
    [SerializeField] private GameObject continueHint; // "按 E 继续"

    [Header("选项按钮")]
    [SerializeField] private GameObject choiceGroup;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Text[] choiceTexts;

    private DialogueController ctrl;
    private bool eventSubscribed;

    private void Start()
    {
        ctrl = DialogueController.Instance;
        if (ctrl != null)
            ctrl.OnStateChanged += Refresh;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int idx = i;
            choiceButtons[i].onClick.AddListener(() => ctrl?.SelectChoice(idx));
        }

        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TrySubscribeInteract();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeInteract();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeInteract();
        if (ctrl != null)
            ctrl.OnStateChanged -= Refresh;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribeInteract();
    }

    private void TrySubscribeInteract()
    {
        if (eventSubscribed) return;

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnInteractRequested.Subscribe(HandleInteract, priority: 0);
            eventSubscribed = true;
            Debug.Log("[DialogueUI] Subscribe OnInteractRequested OK (priority: 0)");
        }
        else
        {
            Debug.LogWarning("[DialogueUI] EventManager.Instance is null, waiting for scene load retry...");
        }
    }

    private void UnsubscribeInteract()
    {
        if (!eventSubscribed) return;

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnInteractRequested.Unsubscribe(HandleInteract);
        }
        eventSubscribed = false;
    }

    private void HandleInteract()
    {
        if (ctrl == null || !ctrl.IsActive) return;
        if (ctrl.CurrentHasBranches) return;
        Debug.Log("[DialogueUI] HandleInteract -> Advance");
        ctrl.Advance();
    }

    private void Refresh()
    {
        if (ctrl == null) return;

        bool visible = ctrl.IsActive;

        if (panel != null) panel.SetActive(visible);
        if (!visible) return;

        // 名称 + 台词
        if (nameText != null) nameText.text = ctrl.NpcName;
        if (lineText != null) lineText.text = ctrl.CurrentText;

        // 继续提示：无分支 且 不是结束节点 时显示
        if (continueHint != null)
            continueHint.SetActive(!ctrl.CurrentHasBranches && !ctrl.IsCurrentEndNode);

        // 选项按钮：有分支时显示
        if (choiceGroup != null)
            choiceGroup.SetActive(ctrl.CurrentHasBranches);

        if (ctrl.CurrentHasBranches)
            RefreshChoices();
    }

    private void RefreshChoices()
    {
        var branches = ctrl.CurrentBranches;
        int count = branches?.Length ?? 0;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool show = i < count;
            choiceButtons[i].gameObject.SetActive(show);
            if (show && choiceTexts.Length > i && choiceTexts[i] != null)
                choiceTexts[i].text = branches[i].text;
        }
    }
}
