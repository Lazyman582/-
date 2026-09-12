using System.Text;
using UnityEngine;

/// <summary>
/// 音频调试工具 — 运行时按 F2 显示/隐藏面板，实时查看所有音频播放状态。
/// 挂到 AudioManager 所在的 GameObject 上即可自动工作。
/// </summary>
public class AudioDebugger : MonoBehaviour
{
    [Header("显示设置")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F2;
    [SerializeField] private int _fontSize = 14;
    [SerializeField] private Color _bgColor = new(0, 0, 0, 0.75f);
    [SerializeField] private Color _playingColor = Color.green;
    [SerializeField] private Color _idleColor = Color.gray;
    [SerializeField] private Color _headerColor = Color.cyan;

    private bool _visible;
    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private bool _stylesInitialized;

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _visible = !_visible;
        }
    }

    private void OnGUI()
    {
        if (!_visible) return;
        InitStyles();
        DrawPanel();
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = _fontSize,
            normal = { textColor = Color.white }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            normal = { textColor = Color.white }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize + 2,
            fontStyle = FontStyle.Bold,
            normal = { textColor = _headerColor }
        };

        _stylesInitialized = true;
    }

    private void DrawPanel()
    {
        var mgr = AudioManager.Instance;
        if (mgr == null)
        {
            GUI.Box(new Rect(10, 10, 400, 60), "AudioManager 未初始化", _boxStyle);
            return;
        }

        // 构建内容
        var sb = new StringBuilder();
        DrawBGMStatus(sb, mgr);
        sb.AppendLine();
        DrawSFXPoolStatus(sb, mgr);
        sb.AppendLine();
        DrawLoopSourceStatus(sb, mgr);
        sb.AppendLine();
        DrawVolumeStatus(sb, mgr);
        sb.AppendLine();
        DrawCatalogQuickView(sb, mgr);

        string text = sb.ToString();
        var content = new GUIContent(text);
        float width = 420f;
        float height = _boxStyle.CalcHeight(content, width) + 20f;

        // 背景
        var bgRect = new Rect(10, 10, width, height);
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, _bgColor);
        bgTex.Apply();
        var oldBg = GUI.skin.box.normal.background;
        GUI.skin.box.normal.background = bgTex;
        GUI.Box(bgRect, text, _boxStyle);
        GUI.skin.box.normal.background = oldBg;
        Destroy(bgTex);
    }

    // ==================== 各模块绘制 ====================

    private void DrawBGMStatus(StringBuilder sb, AudioManager mgr)
    {
        sb.AppendLine($"<b>━━━ BGM 背景音乐 ━━━</b>");
        var src = mgr.BgmSource;
        if (src == null)
        {
            sb.AppendLine("  (无 BGM 源)");
            return;
        }

        string state = src.isPlaying ? "▶ 播放中" : "■ 已停止";
        Color stateColor = src.isPlaying ? _playingColor : _idleColor;
        string clipName = src.clip != null ? src.clip.name : "(无)";
        float progress = src.clip != null && src.clip.length > 0
            ? src.time / src.clip.length
            : 0f;
        string bar = ProgressBar(progress, 20);

        sb.AppendLine($"  {state}  [{clipName}]");
        sb.AppendLine($"  进度: {bar} {src.time:F1}s / {src.clip?.length ?? 0:F1}s");
        sb.AppendLine($"  循环: {src.loop}  音量: {mgr.BGMVolume:F2}");
    }

    private void DrawSFXPoolStatus(StringBuilder sb, AudioManager mgr)
    {
        sb.AppendLine($"<b>━━━ SFX 音效池 ({mgr.SfxSources?.Length ?? 0} 个源) ━━━</b>");
        if (mgr.SfxSources == null) return;

        for (int i = 0; i < mgr.SfxSources.Length; i++)
        {
            var src = mgr.SfxSources[i];
            string mark = src.isPlaying ? "▶" : "·";
            string info = src.isPlaying
                ? $"播放中 (time: {src.time:F2}s)"
                : "空闲";
            sb.AppendLine($"  [{i}] {mark} {info}");
        }
    }

    private void DrawLoopSourceStatus(StringBuilder sb, AudioManager mgr)
    {
        sb.AppendLine($"<b>━━━ 循环音效 (移动等) ━━━</b>");
        var src = mgr.MoveSource;
        if (src == null) { sb.AppendLine("  (无循环源)"); return; }

        string state = src.isPlaying ? "▶ 播放中" : "■ 已停止";
        string clipName = src.clip != null ? src.clip.name : "(无)";
        float progress = src.clip != null && src.clip.length > 0
            ? src.time % src.clip.length / src.clip.length
            : 0f;
        string bar = ProgressBar(progress, 20);

        sb.AppendLine($"  {state}  [{clipName}]");
        sb.AppendLine($"  循环进度: {bar}  loop={src.loop}");
    }

    private void DrawVolumeStatus(StringBuilder sb, AudioManager mgr)
    {
        sb.AppendLine($"<b>━━━ 音量 ━━━</b>");
        sb.AppendLine($"  BGM:  {ProgressBar(mgr.BGMVolume, 15)} {mgr.BGMVolume:F2}");
        sb.AppendLine($"  SFX:  {ProgressBar(mgr.SFXVolume, 15)} {mgr.SFXVolume:F2}");
    }

    private void DrawCatalogQuickView(StringBuilder sb, AudioManager mgr)
    {
        var cat = mgr.Catalog;
        if (cat == null)
        {
            sb.AppendLine($"<b>━━━ Catalog ━━━</b>");
            sb.AppendLine("  (未赋值)");
            return;
        }

        sb.AppendLine($"<b>━━━ Catalog: {cat.name} ━━━</b>");
        foreach (AudioClipCatalog.Category c in System.Enum.GetValues(typeof(AudioClipCatalog.Category)))
        {
            var entries = cat.GetAll(c);
            if (entries.Count == 0) continue;
            sb.AppendLine($"  [{c}] ({entries.Count} 条)");
            foreach (var e in entries)
                sb.AppendLine($"    · {e.id}: {(e.clip != null ? e.clip.name : "(空)")}");
        }
    }

    // ==================== 辅助 ====================
    private static string ProgressBar(float ratio, int width)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt(ratio * width), 0, width);
        return "[" + new string('█', filled) + new string('░', width - filled) + "]";
    }
}
