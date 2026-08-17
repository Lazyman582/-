using UnityEngine;

/// <summary>
/// 对话后动作基类 — 对话结束时执行，ScriptableObject 可复用
/// 派生类重写 Execute()
/// </summary>
public abstract class DialogueAction : ScriptableObject
{
    public abstract void Execute();
}
