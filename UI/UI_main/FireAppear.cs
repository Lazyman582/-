using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FireAppear : MonoBehaviour
{
    [Header("引用")]
    public Image darkOverlay;      // 全局暗色遮罩（可空）
    public RectTransform fire;     // 火焰本体
    public Image glow;             // 光晕（可空）

    [Header("出现")]
    public float popDuration;      // 弹出时长
    public float glowPeak = 0.8f;          // 光晕峰值透明度
    public float glowSustain = 0.3f;       // 光晕余辉透明度

    [Header("消失")]
    [Tooltip("火焰消失动画时长（秒）")]
    public float disappearDuration = 1.2f;

    private Tween _shakeTween;     // 循环震动的 tween 引用，Disappear 时需要停掉
    
    void Awake()
    {
        if (fire != null)
        {
            fire.localScale = Vector3.zero;
            var img = fire.GetComponent<Image>();
            if (img != null) img.color = new Color(1, 1, 1, 0);
        }
        if (glow != null)
        {
            glow.color = new Color(1, 0.8f, 0.4f, 0);
            glow.rectTransform.localScale = Vector3.one * 0.5f;
        }
    }

    public void Play()
    {
        if (fire == null) return;

        var fireImg = fire.GetComponent<Image>();
        Sequence s = DOTween.Sequence();

        // 弹性弹出 + 淡入
        s.Append(fire.DOScale(1f, popDuration).SetEase(Ease.OutBack));
        if (fireImg != null)
            s.Join(fireImg.DOFade(1f, popDuration * 0.5f));

        // 光晕一闪
        if (glow != null)
        {
            s.Join(glow.DOFade(glowPeak, 0.1f));
            s.Join(glow.rectTransform.DOScale(1.5f, 0.4f).SetEase(Ease.OutQuad));
            s.Append(glow.DOFade(glowSustain, 0.3f));
        }

        // 背景微压暗
        if (darkOverlay != null)
            s.Join(darkOverlay.DOFade(0.85f, popDuration));

        // 火焰微震（保存引用，Disappear 时停掉）
        s.OnComplete(() =>
        {
            _shakeTween = fire.DOShakeScale(0.5f, 0.05f).SetLoops(-1, LoopType.Restart);
        });
        AudioManager.Instance?.PlayFire();
    }

    /// <summary>
    /// 火焰消失：停掉循环震动，缩小 + 淡出，光晕熄灭，暗色遮罩恢复透明。
    /// 由 FireLightText 在指定台词行调用（disappearLineIndex）。
    /// </summary>
    public void Disappear()
    {
        // 停掉无限循环的震动，否则 Disappear 后火还会抖
        _shakeTween?.Kill();
        _shakeTween = null;

        Sequence s = DOTween.Sequence();

        // 火焰缩小 + 淡出
        if (fire != null)
        {
            var fireImg = fire.GetComponent<Image>();
            s.Append(fire.DOScale(0f, disappearDuration).SetEase(Ease.InBack));
            if (fireImg != null)
                s.Join(fireImg.DOFade(0f, disappearDuration));
        }

        // 光晕熄灭
        if (glow != null)
        {
            s.Join(glow.DOFade(0f, disappearDuration));
            s.Join(glow.rectTransform.DOScale(0.5f, disappearDuration));
        }

        // 暗色遮罩恢复透明
        if (darkOverlay != null)
            s.Join(darkOverlay.DOFade(0f, disappearDuration));
    }
}
