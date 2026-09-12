using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频管理器 — 单例，使用 AudioMixer 管理路由和音量。
/// 音频资产统一从 AudioClipCatalog（ScriptableObject 数据库）中查询。
///
/// 前置步骤：
///   1. 右键 → Create → Audio → Audio Catalog，命名为 "AudioCatalog"
///   2. 在 Catalog 中添加各分类的音频条目（id + Clip）
///   3. 将 Catalog 拖入 AudioManager 的 _catalog 字段
///   4. 创建 MainMixer，设置 Group 和 Exposed Parameters，拖入 _mixer
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ==================== 单例 ====================
    public static AudioManager Instance { get; private set; }

    // ==================== 数据库 ====================
    [Header("音频数据库")]
    [Tooltip("将 AudioCatalog (ScriptableObject) 拖入此处")]
    [SerializeField] private AudioClipCatalog _catalog;

    // ==================== AudioMixer ====================
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private string _masterParam = "MasterVolume";
    [SerializeField] private string _bgmParam = "BGMVolume";
    [SerializeField] private string _sfxParam = "SFXVolume";
    [SerializeField] private string _bgmGroupName = "BGM";
    [SerializeField] private string _sfxGroupName = "SFX";

    // ==================== 音量 ====================
    [Header("默认音量")]
    [SerializeField] private float _bgmVolume = 0.8f;
    [SerializeField] private float _sfxVolume = 1f;

    // ==================== 内部 AudioSource ====================
    [Header("SFX 池")]
    [SerializeField] private int _sfxPoolSize = 4;

    private AudioSource _bgmSource;
    private AudioSource[] _sfxSources;
    private int _nextSfxIndex;

    // 持续性音效（循环，可单独启停）
    private AudioSource _moveSource;
    private AudioSource _currentLoopSource; // 当前正在播放的持续性音效（移动等）

    // ==================== 调试查询属性 ====================
    public AudioClipCatalog Catalog => _catalog;
    public AudioSource BgmSource => _bgmSource;
    public AudioSource[] SfxSources => _sfxSources;
    public AudioSource MoveSource => _moveSource;
    public float BGMVolume => _bgmVolume;
    public float SFXVolume => _sfxVolume;

    // ==================== 初始化 ====================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // BGM
        _bgmSource = GetComponent<AudioSource>();
        if (_bgmSource == null)
            _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        RouteToMixerGroup(_bgmSource, _bgmGroupName);

        // SFX 池
        _sfxSources = new AudioSource[_sfxPoolSize];
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            _sfxSources[i] = gameObject.AddComponent<AudioSource>();
            _sfxSources[i].loop = false;
            _sfxSources[i].playOnAwake = false;
            RouteToMixerGroup(_sfxSources[i], _sfxGroupName);
        }
        _nextSfxIndex = 0;

        // 移动音效专用
        _moveSource = gameObject.AddComponent<AudioSource>();
        _moveSource.loop = true;
        _moveSource.playOnAwake = false;
        RouteToMixerGroup(_moveSource, _sfxGroupName);
    }

    private void Start()
    {
        float savedBGM = PlayerPrefs.GetFloat("BGM_Volume", _bgmVolume);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Volume", _sfxVolume);
        float savedMaster = PlayerPrefs.GetFloat("Master_Volume", 1f);
        ApplyVolume(_bgmParam, savedBGM);
        ApplyVolume(_sfxParam, savedSFX);
        ApplyVolume(_masterParam, savedMaster);
        _bgmVolume = savedBGM;
        _sfxVolume = savedSFX;

        Debug.Log("[AudioManager] 初始化完成 — Catalog + Mixer 方案已就绪");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==================== Mixer 路由 ====================
    private void RouteToMixerGroup(AudioSource source, string groupName)
    {
        if (_mixer == null) return;
        var groups = _mixer.FindMatchingGroups(groupName);
        if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
    }

    private void ApplyVolume(string paramName, float linearValue)
    {
        if (_mixer == null) return;
        _mixer.SetFloat(paramName, LinearToDB(linearValue));
    }

    public static float LinearToDB(float linear)
    {
        float c = Mathf.Clamp01(linear);
        if (c <= 0.0001f) return -80f;
        return Mathf.Log10(c) * 20f;
    }

    public static float DBToLinear(float dB)
    {
        if (dB <= -80f) return 0f;
        return Mathf.Pow(10f, dB / 20f);
    }

    // ==================== BGM ====================
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSource.isPlaying && _bgmSource.clip == clip) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
        Debug.Log($"[AudioManager] BGM: {clip.name}");
    }

    /// <summary>从 Catalog 按 ID 播放 BGM</summary>
    public void PlayBGMById(string id) => PlayBGM(_catalog.GetClip(AudioClipCatalog.Category.BGM, id));

    /// <summary>从 Resources/Music/ 加载并播放 BGM</summary>
    public void PlayBGMByName(string musicName)
    {
        var clip = Resources.Load<AudioClip>("Music/" + musicName);
        if (clip == null) { Debug.LogError($"[AudioManager] 找不到: Resources/Music/{musicName}"); return; }
        PlayBGM(clip);
    }

    public void PlayTitleBGM()  => PlayBGMById("title");
    public void PlayLevelBGM()  => PlayBGMById("level");
    public void StopBGM()       { if (_bgmSource.isPlaying) _bgmSource.Stop(); }
    public void PauseBGM()      { if (_bgmSource.isPlaying) _bgmSource.Pause(); }
    public void ResumeBGM()     { if (!_bgmSource.isPlaying && _bgmSource.clip != null) _bgmSource.Play(); }

    public void SetBGMVolume(float v)
    {
        _bgmVolume = Mathf.Clamp01(v);
        ApplyVolume(_bgmParam, _bgmVolume);
        PlayerPrefs.SetFloat("BGM_Volume", _bgmVolume);
    }
    public float GetBGMVolume() => _bgmVolume;

    // ==================== SFX 核心 ====================
    /// <summary>播放一次性音效。clip 为 null 时静默跳过。</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        var src = _sfxSources[_nextSfxIndex];
        _nextSfxIndex = (_nextSfxIndex + 1) % _sfxSources.Length;
        src.PlayOneShot(clip);
    }

    /// <summary>从 Catalog 按分类 + ID 播放一次性 SFX</summary>
    public void PlaySFXById(AudioClipCatalog.Category category, string id)
    {
        // 中央守卫：UI 打开（背包/对话等）时屏蔽玩家游戏音效，UI 音效不受影响
        if (category == AudioClipCatalog.Category.PlayerSFX && UIManager.IsUIBlockingInput)
            return;

        var entry = _catalog?.Find(category, id);
        if (entry == null) { Debug.LogWarning($"[AudioManager] Catalog 中找不到 [{category}] {id}"); return; }
        PlaySFX(entry.clip);
    }

    // ==================== 持续性 SFX（可启停的循环音效） ====================
    /// <summary>启动一个循环音效（如移动），用专用源播放，不影响 SFX 池</summary>
    public void PlayLoopSFX(AudioClip clip)
    {
        // 中央守卫：UI 打开时不允许启动玩家循环音效
        if (UIManager.IsUIBlockingInput) return;
        if (clip == null || _moveSource == null) return;
        if (_moveSource.isPlaying && _moveSource.clip == clip) return;
        _moveSource.clip = clip;
        _moveSource.Play();
        _currentLoopSource = _moveSource;
    }

    public void PlayMoveSFX()  => PlayLoopSFX(_catalog?.GetClip(AudioClipCatalog.Category.PlayerSFX, "move"));
    public void StopMoveSFX()  { if (_moveSource != null && _moveSource.isPlaying) _moveSource.Stop(); }

    /// <summary>停止当前所有循环音效</summary>
    public void StopAllLoops()
    {
        if (_moveSource != null && _moveSource.isPlaying) _moveSource.Stop();
        _currentLoopSource = null;
    }

    // ==================== SFX 快捷方法（从 Catalog 查询） ====================
    public void PlayJumpSFX()      => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "jump");
    public void PlayLandSFX()      => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "land");
    public void PlayAttackSwing()  => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "attack_swing");
    public void PlayAttackFinisher() =>PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "attack_finisher");
    public void PlayAttackHit()    => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "attack_hit");
    public void PlayDodgeSFX()     => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "dodge");
    public void PlayCrouchSFX()    => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "crouch");
    public void PlayHurtSFX()      => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "hurt");
    public void PlayDeathSFX()     => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "death");
    public void PlayInteractSFX()  => PlaySFXById(AudioClipCatalog.Category.PlayerSFX, "interact");
    public void PlayButtonClick()  => PlaySFXById(AudioClipCatalog.Category.UISFX, "button_click");
    public void PlayButtonHover()  => PlaySFXById(AudioClipCatalog.Category.UISFX, "button_hover");

    public void SetSFXVolume(float v)
    {
        _sfxVolume = Mathf.Clamp01(v);
        ApplyVolume(_sfxParam, _sfxVolume);
        PlayerPrefs.SetFloat("SFX_Volume", _sfxVolume);
    }
    public float GetSFXVolume() => _sfxVolume;

    // ==================== Master ====================
    public void SetMasterVolume(float v)
    {
        v = Mathf.Clamp01(v);
        ApplyVolume(_masterParam, v);
        PlayerPrefs.SetFloat("Master_Volume", v);
    }

    // ==================== Snapshot ====================
    public void TransitionToSnapshot(string name, float time = 0.1f)
    {
        if (_mixer == null) return;
        var snap = _mixer.FindSnapshot(name);
        if (snap != null) snap.TransitionTo(time);
    }
}
