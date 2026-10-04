using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSlideshow : MonoBehaviour
{
    [Header("按顺序放入所有图片")]
    [SerializeField] private Image[] slides;

    [Header("每张淡入时长（秒）")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("每张停留时长（秒）")]
    [SerializeField] private float holdDuration = 1.5f;

    private void Start()
    {
        StartCoroutine(PlayOnce());
    }

    private IEnumerator PlayOnce()
    {
        // 1. 全部隐藏
        foreach (var img in slides)
        {
            SetAlpha(img, 0f);
            img.gameObject.SetActive(false);
        }

        // 2. 从第一张到最后一张，依次淡入
        for (int i = 0; i < slides.Length; i++)
        {
            Image img = slides[i];

            img.gameObject.SetActive(true);
            SetAlpha(img, 0f);

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                SetAlpha(img, Mathf.Clamp01(t / fadeDuration));
                yield return null;
            }

            SetAlpha(img, 1f);

            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);
        }

        // 播放结束，最后一张保持显示
    }

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}