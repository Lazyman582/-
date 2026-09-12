using System.Collections.Generic;
using UnityEngine;

// ============================================================
// AudioEntry — 数据库中的一条音频记录
// ============================================================
[System.Serializable]
public class AudioEntry
{
    [Tooltip("唯一标识，如 jump / attack_swing / boss_theme")]
    public string id;

    [Tooltip("音频文件")]
    public AudioClip clip;

    [Tooltip("播放音量 (0~1)，覆盖全局音量")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("音高变化范围 (±)，0 表示不变")]
    [Range(0f, 0.5f)]
    public float pitchVariation;

    public bool IsValid => clip != null && !string.IsNullOrEmpty(id);
}

// ============================================================
// AudioClipCatalog — 音频数据库 (ScriptableObject)
// 集中管理所有音频资产，提供增删改查
// ============================================================
[CreateAssetMenu(menuName = "Audio/Audio Catalog", fileName = "AudioCatalog")]
public class AudioClipCatalog : ScriptableObject
{
    [Header("背景音乐")]
    [SerializeField] private List<AudioEntry> _bgmClips = new();

    [Header("玩家音效")]
    [SerializeField] private List<AudioEntry> _playerSFX = new();

    [Header("敌人音效")]
    [SerializeField] private List<AudioEntry> _enemySFX = new();

    [Header("UI 音效")]
    [SerializeField] private List<AudioEntry> _uiSFX = new();

    [Header("环境音效")]
    [SerializeField] private List<AudioEntry> _ambientSFX = new();

    // ==================== 查询 ====================

    /// <summary>按分类和 ID 查找单个条目</summary>
    public AudioEntry Find(Category category, string id)
    {
        var list = GetList(category);
        return list?.Find(e => e.id == id);
    }

    /// <summary>按分类和 ID 获取 AudioClip（最常用）</summary>
    public AudioClip GetClip(Category category, string id)
    {
        return Find(category, id)?.clip;
    }

    /// <summary>获取指定分类下所有条目</summary>
    public List<AudioEntry> GetAll(Category category)
    {
        return new List<AudioEntry>(GetList(category));
    }

    /// <summary>按名称模糊搜索（忽略大小写）</summary>
    public List<AudioEntry> Search(string keyword)
    {
        var results = new List<AudioEntry>();
        if (string.IsNullOrEmpty(keyword)) return results;

        string lower = keyword.ToLower();
        foreach (Category cat in System.Enum.GetValues(typeof(Category)))
        {
            foreach (var entry in GetList(cat))
            {
                if (entry.id.ToLower().Contains(lower))
                    results.Add(entry);
            }
        }
        return results;
    }

    /// <summary>检查某个 ID 是否存在</summary>
    public bool Exists(Category category, string id)
    {
        return Find(category, id) != null;
    }

    // ==================== 增加 ====================

    /// <summary>添加或覆盖一条音频记录</summary>
    public void Add(Category category, AudioEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.id))
        {
            Debug.LogWarning("[AudioCatalog] 添加失败：entry 为空或 id 无效");
            return;
        }

        var list = GetList(category);
        var existing = list.Find(e => e.id == entry.id);
        if (existing != null)
        {
            existing.clip = entry.clip;
            existing.volume = entry.volume;
            existing.pitchVariation = entry.pitchVariation;
            Debug.Log($"[AudioCatalog] 已更新 [{category}] {entry.id}");
        }
        else
        {
            list.Add(entry);
            Debug.Log($"[AudioCatalog] 已添加 [{category}] {entry.id}");
        }
    }

    /// <summary>快捷添加（只传 Clip）</summary>
    public AudioEntry AddClip(Category category, string id, AudioClip clip, float volume = 1f)
    {
        var entry = new AudioEntry { id = id, clip = clip, volume = volume };
        Add(category, entry);
        return entry;
    }

    // ==================== 删除 ====================

    /// <summary>按 ID 删除一条记录</summary>
    public bool Remove(Category category, string id)
    {
        var list = GetList(category);
        int removed = list.RemoveAll(e => e.id == id);
        if (removed > 0)
        {
            Debug.Log($"[AudioCatalog] 已删除 [{category}] {id}");
            return true;
        }
        Debug.LogWarning($"[AudioCatalog] 删除失败：找不到 [{category}] {id}");
        return false;
    }

    /// <summary>清空指定分类</summary>
    public void ClearCategory(Category category)
    {
        GetList(category).Clear();
        Debug.Log($"[AudioCatalog] 已清空 [{category}]");
    }

    // ==================== 修改 ====================

    /// <summary>替换指定条目的音频文件</summary>
    public bool UpdateClip(Category category, string id, AudioClip newClip)
    {
        var entry = Find(category, id);
        if (entry == null) return false;
        entry.clip = newClip;
        return true;
    }

    /// <summary>更新指定条目的音量</summary>
    public bool UpdateVolume(Category category, string id, float volume)
    {
        var entry = Find(category, id);
        if (entry == null) return false;
        entry.volume = Mathf.Clamp01(volume);
        return true;
    }

    /// <summary>重命名条目 ID</summary>
    public bool Rename(Category category, string oldId, string newId)
    {
        if (string.IsNullOrEmpty(newId)) return false;
        if (Exists(category, newId))
        {
            Debug.LogWarning($"[AudioCatalog] 重命名失败：{newId} 已存在");
            return false;
        }
        var entry = Find(category, oldId);
        if (entry == null) return false;
        entry.id = newId;
        return true;
    }

    // ==================== 内部方法 ====================

    private List<AudioEntry> GetList(Category category)
    {
        return category switch
        {
            Category.BGM => _bgmClips,
            Category.PlayerSFX => _playerSFX,
            Category.EnemySFX => _enemySFX,
            Category.UISFX => _uiSFX,
            Category.Ambient => _ambientSFX,
            _ => _playerSFX,
        };
    }

    // ==================== 调试 ====================

    /// <summary>打印整个数据库内容到 Console</summary>
    [ContextMenu("Print Full Catalog")]
    public void PrintCatalog()
    {
        int total = 0;
        foreach (Category cat in System.Enum.GetValues(typeof(Category)))
        {
            var list = GetList(cat);
            Debug.Log($"=== [{cat}] ({list.Count} 条) ===");
            foreach (var e in list)
                Debug.Log($"  {e.id}: {(e.clip != null ? e.clip.name : "(空)")} vol={e.volume}");
            total += list.Count;
        }
        Debug.Log($"=== 总计 {total} 条音频记录 ===");
    }

    // ==================== 分类枚举 ====================
    public enum Category
    {
        BGM,
        PlayerSFX,
        EnemySFX,
        UISFX,
        Ambient
    }
}
