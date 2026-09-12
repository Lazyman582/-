using UnityEngine;

/// <summary>
/// 对话控制器 — 节点驱动，支持分支跳转
/// </summary>
public class DialogueController : Singleton<DialogueController>
{
    public event System.Action OnStateChanged;
    public event System.Action OnDialogueEnded;

    public bool IsActive          { get; private set; }
    public string NpcName         { get; private set; }
    public string CurrentText     { get; private set; }
    public bool CurrentHasBranches { get; private set; }
    public DialogueBranch[] CurrentBranches { get; private set; }
    public bool IsCurrentEndNode  { get; private set; }
    public int CurrentNodeIndex   { get; private set; }
    public int TotalNodes         { get; private set; }

    private DialogueData data;
    private int dialogueStartFrame;

    /// <summary>开始一段对话</summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null || dialogueData.NodeCount == 0 || IsActive) return;
        Debug.Log($"[DialogueController] Start dialogue: {dialogueData.NpcName}, {dialogueData.NodeCount} nodes");

        
        if (StateController.Instance != null)
            StateController.Instance.ChangeState(CharacterStateEnum.Idle);
        
        if (UserInput.Instance != null)
            UserInput.Instance.stop = true;

        CurrentNodeIndex = 0;
        data = dialogueData;
        NpcName = data.NpcName;
        TotalNodes = data.NodeCount;
        

        IsActive = true;
        dialogueStartFrame = Time.frameCount;
        ShowNode(0);
    }

    /// <summary>E 键：推进到下一个节点</summary>
    public void Advance()
    {
        if (!IsActive || CurrentHasBranches) return;

        if (Time.frameCount == dialogueStartFrame) return;

        if (IsCurrentEndNode)
        {
            Debug.Log("[DialogueController] Advance -> EndDialogue (end node)");
            EndDialogue();
            return;
        }

        int next = CurrentNodeIndex + 1;
        if (next >= TotalNodes)
        {
            Debug.Log("[DialogueController] Advance -> EndDialogue (no more nodes)");
            EndDialogue();
        }
        else
        {
            Debug.Log($"[DialogueController] Advance -> node {next}");
            ShowNode(next);
        }
    }

    /// <summary>选择某个分支</summary>
    public void SelectChoice(int branchIndex)
    {
        if (!IsActive || !CurrentHasBranches) return;
        if (CurrentBranches == null || branchIndex < 0 || branchIndex >= CurrentBranches.Length) return;

        int target = CurrentBranches[branchIndex].nextNodeIndex;

        // target < 0 表示直接结束对话
        if (target < 0 || target >= TotalNodes)
            EndDialogue();
        else
            ShowNode(target);
    }

    public void EndDialogue()
    {
        // 解锁玩家
        if (UserInput.Instance != null)
            UserInput.Instance.stop = false;

        IsActive = false;
        data = null;
        CurrentText = null;
        CurrentBranches = null;
        CurrentHasBranches = false;
        IsCurrentEndNode = false;
        OnStateChanged?.Invoke();
        OnDialogueEnded?.Invoke();
    }

    private void ShowNode(int index)
    {
        CurrentNodeIndex = index;
        var node = data.Nodes[index];

        CurrentText = node.text;
        
        CurrentHasBranches = node.HasBranches;
        CurrentBranches = node.HasBranches ? node.branches : null;
        IsCurrentEndNode = node.isEndNode;

        OnStateChanged?.Invoke();
    }
}
