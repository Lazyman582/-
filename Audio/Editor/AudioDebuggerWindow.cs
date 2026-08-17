using UnityEditor;
using UnityEngine;

/// <summary>
/// Audio Debugger Editor 窗口 — 在 Unity 编辑器中通过 Tools → Audio Debugger 打开。
/// 可在非 Play Mode 下浏览 Catalog 内容。
/// </summary>
public class AudioDebuggerWindow : EditorWindow
{
    private Vector2 _scrollPos;

    [MenuItem("Tools/Audio Debugger")]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioDebuggerWindow>("Audio Debugger");
        window.minSize = new Vector2(350, 400);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Audio Debugger", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Play Mode: {Application.isPlaying}");
        EditorGUILayout.LabelField($"AudioManager: {(AudioManager.Instance != null ? "✓ 已初始化" : "✗ 未初始化")}");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Catalog 浏览", EditorStyles.boldLabel);

        // 从 Resources 或 AssetDatabase 加载 Catalog
        var catalog = FindCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox(
                "未找到 AudioCatalog。请在 Project 中创建：\n右键 → Create → Audio → Audio Catalog",
                MessageType.Info);

            if (GUILayout.Button("创建 AudioCatalog"))
            {
                var newCat = CreateInstance<AudioClipCatalog>();
                AssetDatabase.CreateAsset(newCat, "Assets/Audio/Data/AudioCatalog.asset");
                AssetDatabase.SaveAssets();
                Selection.activeObject = newCat;
            }
            return;
        }

        if (GUILayout.Button("在 Inspector 中打开 Catalog"))
            Selection.activeObject = catalog;

        if (GUILayout.Button("Print Catalog to Console"))
            catalog.PrintCatalog();

        EditorGUILayout.Space();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        foreach (AudioClipCatalog.Category cat in System.Enum.GetValues(typeof(AudioClipCatalog.Category)))
        {
            var entries = catalog.GetAll(cat);
            if (entries.Count == 0) continue;

            EditorGUILayout.LabelField($"━━━ [{cat}] ({entries.Count} 条) ━━━", EditorStyles.miniBoldLabel);

            foreach (var entry in entries)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.id, GUILayout.Width(120));
                EditorGUILayout.ObjectField(entry.clip, typeof(AudioClip), false, GUILayout.Width(180));
                EditorGUILayout.LabelField($"vol:{entry.volume:F1}", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    private void Update()
    {
        if (Application.isPlaying)
            Repaint();
    }

    private static AudioClipCatalog FindCatalog()
    {
        // 优先从 AudioManager 拿
        if (AudioManager.Instance != null && AudioManager.Instance.Catalog != null)
            return AudioManager.Instance.Catalog;

        // 从 AssetDatabase 中查找
        var guids = AssetDatabase.FindAssets("t:AudioClipCatalog");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClipCatalog>(path);
        }

        return null;
    }
}
