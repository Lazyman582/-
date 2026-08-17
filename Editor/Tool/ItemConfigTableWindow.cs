using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器窗口 — 物品配置表
/// Window > 物品配置表 打开，以表格形式查看所有 ItemConfig
/// </summary>
public class ItemConfigTableWindow : EditorWindow
{
    private ItemConfig[] configs;
    private Vector2 scrollPos;
    private bool refreshed;

    [MenuItem("Window/物品配置表")]
    public static void ShowWindow()
    {
        var window = GetWindow<ItemConfigTableWindow>("物品配置表");
        window.minSize = new Vector2(600, 300);
        window.Refresh();
    }

    private void OnEnable()
    {
        if (!refreshed) Refresh();
    }

    private void Refresh()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemConfig");
        configs = new ItemConfig[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            configs[i] = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
        }
        refreshed = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        // 工具栏
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"共 {configs.Length} 个物品配置", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("刷新", GUILayout.Width(60)))
        {
            Refresh();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 表头
        EditorGUILayout.BeginHorizontal();
        DrawHeader("ID", 120);
        DrawHeader("名称", 100);
        DrawHeader("类型", 80);
        DrawHeader("堆叠上限", 60);
        DrawHeader("图标", 60);
        DrawHeader("描述", 200);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 分隔线
        Rect lastRect = GUILayoutUtility.GetLastRect();
        Rect lineRect = new Rect(lastRect.x, lastRect.yMax + 1, position.width - 10, 1);
        EditorGUI.DrawRect(lineRect, Color.gray);

        // 内容（可滚动）
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var cfg in configs)
        {
            if (cfg == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 0;

            // ID — 点击定位到资产
            if (GUILayout.Button(cfg.ItemId, EditorStyles.linkLabel, GUILayout.Width(120)))
            {
                EditorGUIUtility.PingObject(cfg);
                Selection.activeObject = cfg;
            }

            EditorGUILayout.LabelField(cfg.ItemName, GUILayout.Width(100));
            EditorGUILayout.LabelField(cfg.ItemType.ToString(), GUILayout.Width(80));
            EditorGUILayout.LabelField(cfg.MaxStack.ToString(), GUILayout.Width(60));

            // 图标预览
            if (cfg.Icon != null)
                EditorGUILayout.ObjectField(cfg.Icon, typeof(Sprite), false, GUILayout.Width(60), GUILayout.Height(40));
            else
                EditorGUILayout.LabelField("(无)", GUILayout.Width(60));

            EditorGUILayout.LabelField(cfg.Description, GUILayout.Width(200));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 行分隔线
            Rect rowRect = GUILayoutUtility.GetLastRect();
            Rect sepRect = new Rect(rowRect.x, rowRect.yMax, position.width - 10, 1);
            EditorGUI.DrawRect(sepRect, new Color(0.3f, 0.3f, 0.3f, 0.3f));
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader(string text, float width)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        EditorGUILayout.LabelField(text, style, GUILayout.Width(width));
    }
}
