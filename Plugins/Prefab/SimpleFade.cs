using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 黑屏转场控制器（挂在全屏 Image 上，也就是黑屏.prefab 的用法）。
///
/// 标准用法（场景切换时）：
///   var fade = SimpleFade.Show();          // 找到/自动创建全屏黑屏层
///   yield return fade.FadeOut();           // 变黑
///   ...这里做异步加载...                    // 黑屏期间加载
///   yield return fade.FadeIn();            // 恢复透明（完成后自动隐藏）
///
/// 兼容旧用法：playOnEnable 勾选时，启用即自动播放一次完整转场（变黑→停0.3秒→恢复）。
/// </summary>
public class SimpleFade : MonoBehaviour
{
    [Header("变黑时长（秒）")]
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("恢复透明时长（秒）")]
    [SerializeField] private float fadeInDuration = 0.8f;

    [Header("启用时自动播放一次完整转场（旧行为）")]
    [SerializeField] private bool playOnEnable = false;

    private Image image;
    private static SimpleFade instance;

    public static SimpleFade Instance => instance;

    private void Awake()
    {
        image = GetComponent<Image>();
        instance = this;
    }

    private void OnEnable()
    {
        if (image == null) image = GetComponent<Image>();
        if (playOnEnable)
        {
            StartCoroutine(PlayOnce());
        }
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private IEnumerator PlayOnce()
    {
        yield return FadeOut(fadeOutDuration);
        yield return new WaitForSeconds(0.3f);
        yield return FadeIn(fadeInDuration);
    }

    /// <summary>
    /// 找到黑屏层（场景里已放置的优先），没有就自动创建一个全屏黑色覆盖层。
    /// 返回的实例标记为 DontDestroyOnLoad，切换场景过程中不会消失。
    /// </summary>
    public static SimpleFade Show()
    {
        if (instance == null)
        {
            var existing = FindObjectsOfType<SimpleFade>(true);
            if (existing.Length > 0)
            {
                instance = existing[0];
            }
        }

        if (instance == null)
        {
            var go = new GameObject("黑屏转场");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            instance = go.AddComponent<SimpleFade>();
        }

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(instance.transform.root.gameObject);
        }

        instance.transform.SetAsLastSibling();
        instance.gameObject.SetActive(true);
        return instance;
    }

    /// <summary>变黑（alpha 0 → 1），使用默认时长</summary>
    public IEnumerator FadeOut()
    {
        yield return FadeOut(fadeOutDuration);
    }

    /// <summary>变黑（alpha 0 → 1），使用真实时间，不受 TimeScale 影响</summary>
    public IEnumerator FadeOut(float duration)
    {
        if (image == null) image = GetComponent<Image>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            var c = image.color;
            c.a = Mathf.Clamp01(t);
            image.color = c;
            yield return null;
        }
    }

    /// <summary>恢复透明（alpha 1 → 0），完成后自动隐藏整个黑屏层</summary>
    public IEnumerator FadeIn()
    {
        yield return FadeIn(fadeInDuration);
    }

    /// <summary>恢复透明（alpha 1 → 0），使用真实时间，完成后自动隐藏</summary>
    public IEnumerator FadeIn(float duration)
    {
        if (image == null) image = GetComponent<Image>();
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            var c = image.color;
            c.a = Mathf.Clamp01(t);
            image.color = c;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
