using UnityEngine;

/// <summary>
/// 对话测试器 — 挂在任意物体上，屏幕左上角显示当前对话状态
/// </summary>
public class DialogueTester : MonoBehaviour
{
    [SerializeField] private bool showGUI = true;

    private DialogueController ctrl;
    private GUIStyle style;
    private bool styleReady;

    private void Start()
    {
        ctrl = DialogueController.Instance;
    }

    private void OnGUI()
    {
        if (!showGUI || ctrl == null || !ctrl.IsActive) return;

        InitStyle();

        string info =
            $"──── 对话测试器 ────\n" +
            $"NPC: {ctrl.NpcName}\n" +
            $"当前节点索引:{ctrl.CurrentNodeIndex}\n"+
            $"节点: {ctrl.CurrentNodeIndex+1} / {ctrl.TotalNodes - 1}\n" +
            $"有分支: {(ctrl.CurrentHasBranches ? "是" : "否")}\n" +
            $"结束节点: {(ctrl.IsCurrentEndNode ? "是" : "否")}\n" +
            $"台词: {ctrl.CurrentText}";

        if (ctrl.CurrentHasBranches)
        {
            var branches = ctrl.CurrentBranches;
            info += "\n\n── 选项 ──";
            for (int i = 0; i < branches.Length; i++)
                info += $"\n  [{i}] {branches[i].text} → 节点 {branches[i].nextNodeIndex}";
        }
        else
        {
            info += "\n\n按 E 继续";
        }

        GUI.Box(new Rect(10, 10, 360, 200 + (ctrl.CurrentHasBranches ? ctrl.CurrentBranches.Length * 22 : 0)), info, style);
    }

    private void InitStyle()
    {
        if (styleReady) return;
        style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };
        styleReady = true;
    }
}
