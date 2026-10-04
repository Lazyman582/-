using System.Collections;
using UnityEngine;

/// <summary>
/// 开场动画控制器 — 挂在 MainUI 场景的 GameStart 物体上。
/// MainUI 场景内已包含开场动画（carton Canvas：黑底 + 逐帧动画 + SimpleTextReveal 文字）。
///
/// 流程：
///   1. 初次进入 → 隐藏主菜单，开场动画（carton）播放逐字台词
///   2. 全部台词播放完成（onAllComplete）→ 最后一句停留 3~4 秒
///   3. 黑屏过渡 → 隐藏开场动画、显示主菜单 → 画面淡入
///
/// 非初次进入（且 alwaysPlayOpening 未勾选）→ 开场动画直接隐藏，主菜单照常显示。
/// </summary>
public class OpeningIntroController : MonoBehaviour
{
    [Header("场景引用")]
    [Tooltip("开场动画容器（MainUI 里的 carton Canvas）")]
    [SerializeField] private GameObject introContainer;
    [Tooltip("主菜单 Canvas（开场时隐藏，结束后显示）")]
    [SerializeField] private GameObject mainMenuCanvas;
    [Tooltip("开场文字组件（带 onAllComplete 回调的 SimpleTextReveal）")]
    [SerializeField] private SimpleTextReveal reveal;

    [Header("流程")]
    [Tooltip("最后一句台词停留时间（秒），建议 3~4")]
    public float lastLineHoldDuration = 3.5f;
    [Tooltip("勾选后每次启动都播放开场（开发时建议勾选）；取消勾选则只有首次进入播放")]
    public bool alwaysPlayOpening = true;

    [Header("背景音乐")]
    [Tooltip("开场动画开始时播放的 BGM id（留空不播）")]
    public string openingBgmId;
    [Tooltip("台词全部播完后播放的 BGM id（留空不播）")]
    public string closingBgmId = "开局";

    private bool _playIntro;

    private void Awake()
    {
        // 判断本次是否播放开场（必须在 reveal 的 Start 之前决定并隐藏好 UI）
        bool hasSeen = PlayerPrefs.GetInt("SeenOpening", 0) == 1;
        _playIntro = alwaysPlayOpening || !hasSeen;

        if (_playIntro)
        {
            // 开场动画盖住主菜单：carton 提到最前 + 主菜单隐藏
            var canvas = introContainer != null ? introContainer.GetComponent<Canvas>() : null;
            if (canvas != null) canvas.sortingOrder = 10;

            if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        }
        else if (introContainer != null)
        {
            // 不播开场：直接隐藏容器（inactive 后 reveal 的 Start 不会执行，不会自动播放）
            introContainer.SetActive(false);
        }
    }

    private void Start()
    {
        if (!_playIntro) return;

        if (reveal != null)
        {
            reveal.onAllComplete.AddListener(OnAllComplete);
        }
        else
        {
            Debug.LogWarning("[OpeningIntro] reveal 未赋值，请把开场文字组件拖入");
        }

        if (!string.IsNullOrEmpty(openingBgmId))
        {
            AudioManager.Instance?.PlayBGMById(openingBgmId);
        }

        StartCoroutine(FinishRoutineWhenReady());
    }

    private void OnAllComplete()
    {
        Debug.Log("[OpeningIntro] 全部台词播放完成");

        // 台词播完后，开始播放收尾 BGM（如"开局"），与黑屏过渡并行
        if (!string.IsNullOrEmpty(closingBgmId))
        {
            AudioManager.Instance?.PlayBGMById(closingBgmId);
        }

        StopAllCoroutines();
        StartCoroutine(FinishIntro());
    }

    /// <summary>兜底：如果 reveal 未配置/onAllComplete 永远不来，60 秒后强制进入主菜单</summary>
    private IEnumerator FinishRoutineWhenReady()
    {
        yield return new WaitForSecondsRealtime(300f);
        Debug.LogWarning("[OpeningIntro] 超时兜底触发，强制进入主菜单");
        yield return StartCoroutine(FinishIntro());
    }

    // ==================== 结束流程 ====================

    private IEnumerator FinishIntro()
    {
        // 1. 最后一句停留 3~4 秒
        yield return new WaitForSecondsRealtime(lastLineHoldDuration);

        // 2. 黑屏
        var fade = SimpleFade.Show();
        yield return fade.FadeOut();

        // 3. 标记已看过开场
        PlayerPrefs.SetInt("SeenOpening", 1);
        PlayerPrefs.Save();

        // 4. 隐藏开场动画，显示主菜单
        if (introContainer != null) introContainer.SetActive(false);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);

        // 5. 等一帧让主菜单就绪，然后淡入
        yield return null;
        yield return fade.FadeIn();

        Debug.Log("[OpeningIntro] 开场动画结束，主菜单已显示");
    }
}
