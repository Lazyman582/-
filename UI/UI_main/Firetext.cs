using UnityEngine;
using DG.Tweening;

public class FireLightText : MonoBehaviour
{
    public SimpleTextReveal reveal;
    public FireAppear fire;

    [Tooltip("在第几句（从 0 开始）文字完成时点燃火焰")]
    public int triggerLineIndex;

    [Header("文字颜色")]
    public Color darkColor = new Color(0.35f, 0.35f, 0.4f);   // 黑暗色
    public Color litColor = new Color(1f, 0.95f, 0.85f);     // 点亮色

    [Header("点亮动画")]
    public float lightUpDuration = 0.6f;

    [Header("火焰熄灭")]
    [Tooltip("在第几句（从 0 开始）文字完成时让火焰消失并恢复文字颜色。-1 表示永不消失")]
    public int disappearLineIndex = 10;

    [Header("颜色恢复")]
    [Tooltip("火焰消失后文字恢复到的颜色（一般用纯白）")]
    public Color restoreColor = Color.white;
    [Tooltip("颜色恢复渐变时长（秒）")]
    public float restoreDuration = 1f;

    private Tween _flickerTween; // 点亮后的闪烁循环，恢复颜色时需要停掉

    void Start()
    {
        if (reveal == null) return;

        reveal.SetColor(darkColor);   // 一开始是暗的

        reveal.onLineComplete.AddListener(OnLineDone);
        reveal.Play();
    }

    void OnLineDone(int index, string text)
    {
        // 点燃句：火焰出现 + 文字点亮
        if (index == triggerLineIndex)
        {
            fire?.Play();
            LightUp();
        }

        if (index == triggerLineIndex) {

            AudioManager.Instance?.PlayFireBGM();
        
        }

        // 熄灭句：火焰消失 + 文字颜色调回
        if (index == disappearLineIndex)
        {
            fire?.Disappear();
            RestoreColor();
             AudioManager.Instance?.StopBGM();
        }
    }

    void LightUp()
    {
        // 点亮渐变：暗色 → 暖色
        DOTween.To(() => 0f, t =>
        {
            reveal.SetColor(Color.Lerp(darkColor, litColor, t));
        }, 1f, lightUpDuration).SetEase(Ease.OutQuad);

        // 点亮后轻微闪烁（火焰感），保存引用以便恢复颜色时停掉
        DOVirtual.DelayedCall(lightUpDuration, () =>
        {
            _flickerTween = DOTween.To(() => 1f, k =>
            {
                reveal.SetColor(litColor * Mathf.Lerp(0.92f, 1f, k));
            }, 0f, 0.6f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        });

    }

    
    void RestoreColor()
    {
        _flickerTween?.Kill();
        _flickerTween = null;

        DOTween.To(() => 0f, t =>
        {
            reveal.SetColor(Color.Lerp(litColor, restoreColor, t));
        }, 1f, restoreDuration).SetEase(Ease.OutQuad);
    }
}
