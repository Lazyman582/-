using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;

/// <summary>
/// 单个场景叠加预设
/// </summary>
[Serializable]
public class SceneSetupPreset
{
    public string name;

    // SceneSetup 是 Unity 的 Serializable 类，但直接序列化数组可能有问题
    // 我们用路径列表来保存场景信息
    public string[] scenePaths;
    public int activeSceneIndex;
}

/// <summary>
/// 存储场景叠加配置的 ScriptableObject
/// </summary>
public class SceneSetupConfig : ScriptableObject
{
    [Header("自动保存")]
    [Tooltip("上次自动保存的场景叠加（切场景时自动保存）")]
    public string[] lastAutoSaveScenePaths;
    public int lastAutoSaveActiveIndex;

    [Header("编辑器行为")]
    [Tooltip("编辑器启动时自动恢复上次的叠加状态")]
    public bool autoRestoreOnLoad = false;

    [Header("命名预设")]
    public List<SceneSetupPreset> presets = new List<SceneSetupPreset>();
}
