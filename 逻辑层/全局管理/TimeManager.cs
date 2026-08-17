using System.Collections;
using UnityEngine;

/// <summary>
/// 时间管理器 — 单例，保存各系统的时间流速，支持帧冻结
/// 各系统用对应的 DeltaTime 取时间，即可受各自流速影响
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("各系统时间流速")]
    [Tooltip("游戏逻辑时间流速（角色移动、物理等）")]
    [SerializeField] private float gameplayTimeScale = 1f;
    [Tooltip("动画时间流速")]
    [SerializeField] private float animationTimeScale = 1f;
    [Tooltip("UI 时间流速")]
    [SerializeField] private float uiTimeScale = 1f;

    /// <summary>游戏逻辑帧间隔（受 gameplayTimeScale 影响）</summary>
    public float GameplayDeltaTime => Time.deltaTime * gameplayTimeScale;
    /// <summary>动画帧间隔（受 animationTimeScale 影响）</summary>
    public float AnimationDeltaTime => Time.deltaTime * animationTimeScale;
    /// <summary>UI 帧间隔（受 uiTimeScale 影响）</summary>
    public float UiDeltaTime => Time.deltaTime * uiTimeScale;

    // 静态安全入口：TimeManager 还没初始化时退回 Time.deltaTime，脚本可直接用
    public static float GameplayDT => Instance != null ? Instance.GameplayDeltaTime : Time.deltaTime;
    public static float AnimationDT => Instance != null ? Instance.AnimationDeltaTime : Time.deltaTime;
    public static float UiDT => Instance != null ? Instance.UiDeltaTime : Time.deltaTime;

    private Coroutine freezeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 帧冻结 — 把游戏逻辑和动画流速压到 freezeScale（默认 0），
    /// 持续 duration 秒后恢复。UI 不受影响。
    /// </summary>
    public void Freeze(float duration, float freezeScale = 0f)
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);
        freezeRoutine = StartCoroutine(FreezeRoutine(duration, freezeScale));
    }

    /// <summary>立即结束冻结，恢复正常流速</summary>
    public void StopFreeze()
    {
        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
            freezeRoutine = null;
            RestoreScales();
        }
    }

    private float savedGameplayScale;
    private float savedAnimationScale;

    private IEnumerator FreezeRoutine(float duration, float freezeScale)
    {
        savedGameplayScale = gameplayTimeScale;
        savedAnimationScale = animationTimeScale;

        gameplayTimeScale = freezeScale;
        animationTimeScale = freezeScale;

        // 冻结计时用真实时间，不能用受影响的 Time.deltaTime
        yield return new WaitForSecondsRealtime(duration);

        RestoreScales();
        freezeRoutine = null;
    }

    private void RestoreScales()
    {
        gameplayTimeScale = savedGameplayScale;
        animationTimeScale = savedAnimationScale;
    }
}
