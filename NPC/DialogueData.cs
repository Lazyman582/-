using System;
using UnityEngine;

/// <summary>
/// 对话分支 — 选项文字 + 跳转目标节点索引
/// </summary>
[Serializable]
public class DialogueBranch
{
    [Tooltip("选项按钮文字")] public string text;
    [Tooltip("选择后跳到哪个节点")] public int nextNodeIndex;
}

/// <summary>
/// 对话节点 — 一句台词 + 可选分支
/// branches 为空 → 线性推进；非空 → 显示选项按钮
/// </summary>
[Serializable]
public class DialogueNode
{
    [Tooltip("NPC 说的台词")]
    [TextArea(2, 4)] public string text;

    [Tooltip("选项分支（空则按 E 继续到下个节点）")]
    public DialogueBranch[] branches;

    [Tooltip("勾选后，该节点无分支时按 E 直接结束对话")]
    public bool isEndNode;

    public bool HasBranches => branches != null && branches.Length > 0;
}

/// <summary>
/// 对话数据 — 节点数组驱动，右键 Create > NPC > Dialogue Data
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "NPC/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("基本信息")]
    [SerializeField] private string npcName = "???";
    [SerializeField] private DialogueNode[] nodes;

    public string NpcName => npcName;
    public DialogueNode[] Nodes => nodes;
    public int NodeCount => nodes?.Length ?? 0;
}
