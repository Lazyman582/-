using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SceneSaveTool : EditorWindow
{
    private const string CONFIG_PATH = "Assets/Editor/SceneSetupConfig.asset";
    private static bool s_IsRestoring = false;

    // ==================== 静态初始化 ====================

    static SceneSaveTool()
    {
        // 切场景前自动保存当前叠加状态
        EditorSceneManager.sceneClosing += OnSceneClosing;

        // 编辑器启动后延迟恢复（等所有场景加载完毕）
        EditorApplication.delayCall += () =>
        {
            var cfg = LoadConfig();
            if (cfg != null && cfg.autoRestoreOnLoad)
            {
                RestoreFromPaths(cfg.lastAutoSaveScenePaths, cfg.lastAutoSaveActiveIndex, silent: true);
            }
        };
    }

    private static void OnSceneClosing(Scene scene, bool removingScene)
    {
        // 正在恢复中时不触发自动保存，避免覆盖
        if (s_IsRestoring) return;
        AutoSave();
    }

    // ==================== 自动保存 ====================

    /// <summary>
    /// 自动保存当前的多场景叠加状态（无提示）
    /// </summary>
    private static void AutoSave()
    {
        var (paths, activeIndex) = GetCurrentScenePaths();
        if (paths == null || paths.Length == 0) return;

        var cfg = LoadOrCreateConfig();
        cfg.lastAutoSaveScenePaths = paths;
        cfg.lastAutoSaveActiveIndex = activeIndex;
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 获取当前所有打开的场景路径及激活场景索引
    /// </summary>
    private static (string[] paths, int activeIndex) GetCurrentScenePaths()
    {
        int count = EditorSceneManager.sceneCount;
        if (count == 0) return (null, -1);

        var paths = new string[count];
        int activeIndex = -1;
        Scene activeScene = EditorSceneManager.GetActiveScene();

        for (int i = 0; i < count; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            paths[i] = scene.path;
            if (scene == activeScene) activeIndex = i;
        }

        return (paths, activeIndex);
    }

    /// <summary>
    /// 根据路径列表恢复场景叠加
    /// </summary>
    private static void RestoreFromPaths(string[] paths, int activeIndex, bool silent = false)
    {
        // 播放模式下禁止恢复叠加（EditorSceneManager.OpenScene 不能在 Play 中调用）
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!silent) EditorUtility.DisplayDialog("提示", "播放模式下无法恢复场景叠加，请先退出 Play 模式", "确定");
            return;
        }

        if (paths == null || paths.Length == 0)
        {
            if (!silent) EditorUtility.DisplayDialog("提示", "没有可恢复的场景叠加", "确定");
            return;
        }

        // 验证路径有效性
        var validPaths = paths.Where(p => !string.IsNullOrEmpty(p) && System.IO.File.Exists(p)).ToArray();
        if (validPaths.Length == 0)
        {
            if (!silent) EditorUtility.DisplayDialog("提示", "保存的场景文件已不存在", "确定");
            return;
        }

        s_IsRestoring = true;
        try
        {
            // 第一步：先打开第一个场景（替换当前所有场景）
            EditorSceneManager.OpenScene(validPaths[0], OpenSceneMode.Single);

            // 第二步：叠加其余场景
            for (int i = 1; i < validPaths.Length; i++)
            {
                EditorSceneManager.OpenScene(validPaths[i], OpenSceneMode.Additive);
            }

            // 第三步：设置激活场景
            if (activeIndex >= 0 && activeIndex < validPaths.Length)
            {
                var targetScene = EditorSceneManager.GetSceneByPath(validPaths[activeIndex]);
                if (targetScene.isLoaded)
                    EditorSceneManager.SetActiveScene(targetScene);
            }

            if (!silent)
                EditorUtility.DisplayDialog("恢复成功", $"已加载 {validPaths.Length} 个场景", "确定");
            else
                Debug.Log($"[场景叠加] 已自动恢复 {validPaths.Length} 个场景");
        }
        catch (System.Exception e)
        {
            if (!silent)
                EditorUtility.DisplayDialog("恢复失败", e.Message, "确定");
            else
                Debug.LogError($"[场景叠加] 自动恢复失败：{e.Message}");
        }
        finally
        {
            s_IsRestoring = false;
        }
    }

    /// <summary>
    /// 获取或创建 ScriptableObject 配置
    /// </summary>
    private static SceneSetupConfig LoadOrCreateConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<SceneSetupConfig>(CONFIG_PATH);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<SceneSetupConfig>();
            AssetDatabase.CreateAsset(cfg, CONFIG_PATH);
            Debug.Log($"[场景叠加] 已创建配置文件：{CONFIG_PATH}");
        }
        return cfg;
    }

    private static SceneSetupConfig LoadConfig()
    {
        return AssetDatabase.LoadAssetAtPath<SceneSetupConfig>(CONFIG_PATH);
    }

    // ==================== 菜单命令 ====================

    [MenuItem("Tools/场景叠加/保存为预设...", false, 100)]
    static void SaveAsPreset()
    {
        var (paths, activeIndex) = GetCurrentScenePaths();
        if (paths == null || paths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "当前没有打开任何场景", "确定");
            return;
        }

        // 弹出命名对话框
        var window = GetWindow<SceneSaveTool>("场景叠加管理");
        window._pendingSaveAsPreset = true;
        window.Show();
    }

    [MenuItem("Tools/场景叠加/快速恢复上次叠加", false, 101)]
    static void QuickRestore()
    {
        var cfg = LoadConfig();
        if (cfg == null || cfg.lastAutoSaveScenePaths == null || cfg.lastAutoSaveScenePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有可恢复的自动保存。\n\n请先打开多个场景叠加，然后切换场景时会自动保存。", "确定");
            return;
        }
        RestoreFromPaths(cfg.lastAutoSaveScenePaths, cfg.lastAutoSaveActiveIndex);
    }

    [MenuItem("Tools/场景叠加/自动保存当前叠加", false, 102)]
    static void ManualSave()
    {
        AutoSave();
        var cfg = LoadConfig();
        int count = cfg?.lastAutoSaveScenePaths?.Length ?? 0;
        EditorUtility.DisplayDialog("保存成功", $"当前 {count} 个场景的叠加状态已保存", "确定");
    }

    [MenuItem("Tools/场景叠加/开启自动恢复 _F5", false, 103)]
    static void ToggleAutoRestore()
    {
        var cfg = LoadOrCreateConfig();
        cfg.autoRestoreOnLoad = !cfg.autoRestoreOnLoad;
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        Menu.SetChecked("Tools/场景叠加/开启自动恢复 _F5", cfg.autoRestoreOnLoad);
        var status = cfg.autoRestoreOnLoad ? "已开启" : "已关闭";
        EditorUtility.DisplayDialog("自动恢复", $"编辑器启动自动恢复：{status}", "确定");
    }

    [MenuItem("Tools/场景叠加/开启自动恢复 _F5", true)]
    static bool ToggleAutoRestoreValidate()
    {
        var cfg = LoadConfig();
        Menu.SetChecked("Tools/场景叠加/开启自动恢复 _F5", cfg != null && cfg.autoRestoreOnLoad);
        return true;
    }

    [MenuItem("Tools/场景叠加/打开管理窗口", false, 200)]
    static void OpenWindow()
    {
        var window = GetWindow<SceneSaveTool>("场景叠加管理");
        window.Show();
    }

    // ==================== EditorWindow UI ====================

    private string _newPresetName = "";
    private Vector2 _scrollPos;
    private bool _pendingSaveAsPreset = false;

    void OnGUI()
    {
        var cfg = LoadOrCreateConfig();

        // ---- 标题 ----
        EditorGUILayout.LabelField("场景叠加管理", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // ---- 当前状态 ----
        var (currentPaths, _) = GetCurrentScenePaths();
        EditorGUILayout.LabelField($"当前打开场景：{currentPaths?.Length ?? 0} 个");
        if (currentPaths != null)
        {
            foreach (var p in currentPaths)
                EditorGUILayout.LabelField("  • " + System.IO.Path.GetFileNameWithoutExtension(p),
                    EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(10);

        // ---- 快速操作按钮 ----
        EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存当前叠加", GUILayout.Height(30))) ManualSave();
        if (GUILayout.Button("恢复上次叠加", GUILayout.Height(30))) QuickRestore();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 自动恢复开关
        EditorGUILayout.BeginHorizontal();
        cfg.autoRestoreOnLoad = EditorGUILayout.Toggle("编辑器启动时自动恢复", cfg.autoRestoreOnLoad);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // ---- 命名预设 ----
        EditorGUILayout.LabelField("命名预设列表", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _newPresetName = EditorGUILayout.TextField("新预设名称", _newPresetName);
        if (GUILayout.Button("+ 保存为新预设", GUILayout.Width(120), GUILayout.Height(25)))
        {
            SaveAsNewPreset(cfg);
        }
        EditorGUILayout.EndHorizontal();

        // 处理从菜单触发的"保存为预设"
        if (_pendingSaveAsPreset)
        {
            _pendingSaveAsPreset = false;
            EditorGUI.FocusTextInControl("NewPresetNameField");
            GUI.FocusControl("NewPresetNameField");
        }

        EditorGUILayout.Space(5);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        if (cfg.presets.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "还没有保存的预设。\n\n使用技巧：\n1. 打开多个场景叠加好你的工作布局\n2. 在上方输入名称，点击保存为新预设之后可以一键切换各种叠加组合",
                MessageType.Info);
        }

        for (int i = cfg.presets.Count - 1; i >= 0; i--)
        {
            var preset = cfg.presets[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"📁 {preset.name}",
                EditorStyles.boldLabel, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField($"{preset.scenePaths?.Length ?? 0} 个场景",
                EditorStyles.miniLabel, GUILayout.Width(60));

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("加载", GUILayout.Width(50), GUILayout.Height(20)))
            {
                RestoreFromPaths(preset.scenePaths, preset.activeSceneIndex);
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("删除", GUILayout.Width(40), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"删除预设 \"{preset.name}\"？", "删除", "取消"))
                {
                    cfg.presets.RemoveAt(i);
                    EditorUtility.SetDirty(cfg);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 显示预设内的场景列表
            if (preset.scenePaths != null)
            {
                foreach (var p in preset.scenePaths)
                    EditorGUILayout.LabelField("    " + System.IO.Path.GetFileNameWithoutExtension(p),
                        EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndScrollView();

        // 应用修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.Space(10);

        // ---- 提示 ----
        EditorGUILayout.HelpBox(
            "工作流程：\n" +
            "1. 在 Hierarchy 中右键 → Open Scene Additive 叠加场景\n" +
            "2. 叠加好后保存为预设（可保存多组，如\"战斗开发\"、\"UI编辑\"）\n" +
            "3. 切场景时自动保存当前叠加状态\n" +
            "4. 用 Tools → 场景叠加 → 快速恢复 一键恢复\n" +
            "5. 后续就能在不同预设间秒切",
            MessageType.Info);
    }

    private void SaveAsNewPreset(SceneSetupConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(_newPresetName))
        {
            EditorUtility.DisplayDialog("提示", "请输入预设名称", "确定");
            return;
        }

        var (paths, activeIndex) = GetCurrentScenePaths();
        if (paths == null || paths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "当前没有打开任何场景", "确定");
            return;
        }

        // 检查是否重名
        if (cfg.presets.Exists(p => p.name == _newPresetName))
        {
            if (!EditorUtility.DisplayDialog("名称重复",
                $"已存在名为 \"{_newPresetName}\" 的预设，是否覆盖？", "覆盖", "取消"))
                return;

            cfg.presets.RemoveAll(p => p.name == _newPresetName);
        }

        cfg.presets.Add(new SceneSetupPreset
        {
            name = _newPresetName,
            scenePaths = paths,
            activeSceneIndex = activeIndex
        });

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();

        _newPresetName = "";
        EditorUtility.DisplayDialog("保存成功",
            $"预设 \"{_newPresetName}\" 已保存（{paths.Length} 个场景）", "确定");

        Repaint();
    }
}
