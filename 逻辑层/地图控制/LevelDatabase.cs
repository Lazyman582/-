using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "LevelDatabase", menuName = "我与史诗/关卡跳转数据库")]
public class LevelDatabase : ScriptableObject
{
    [Serializable]
    public class LevelEntry
    {
        [Tooltip("本关卡的 Addressables 地址（默认为场景资源路径，如 Assets/关卡一.unity）")]
        public string sceneAddress;

        [Tooltip("备注用的显示名称")]
        public string displayName;

        [Tooltip("本关卡可以跳转到的场景地址列表")]
        public List<string> jumpableScenes = new List<string>();
    }

    [SerializeField] private List<LevelEntry> levels = new List<LevelEntry>();
    public List<LevelEntry> Levels => levels;

    private static LevelDatabase _instance;
    public static LevelDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // 依次尝试已知的 Resources 路径（资产挪位置后旧路径也能兼容）
                _instance = Resources.Load<LevelDatabase>("Data/Level/LevelDatabase");
                if (_instance == null)
                {
                    _instance = Resources.Load<LevelDatabase>("Data/LevelDatabase");
                }

#if UNITY_EDITOR
                // 编辑器兜底：全项目按类型搜索，资产放在任何位置都能找到
                if (_instance == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:LevelDatabase");
                    if (guids.Length > 0)
                    {
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDatabase>(
                            UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
#endif

                if (_instance == null)
                {
                    Debug.LogWarning("未找到 LevelDatabase 资产，请通过 Create → 我与史诗 → 关卡跳转数据库 创建");
                }
            }
            return _instance;
        }
    }

    /// <summary>按场景地址查找关卡条目</summary>
    public LevelEntry GetEntry(string sceneAddress)
    {
        return levels.Find(l => l.sceneAddress == sceneAddress);
    }

    /// <summary>查询从 fromAddress 关卡能否跳转到 toAddress 关卡</summary>
    public bool CanJump(string fromAddress, string toAddress)
    {
        var entry = GetEntry(fromAddress);
        return entry != null && entry.jumpableScenes.Contains(toAddress);
    }
}
