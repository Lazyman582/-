using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SimpleTextReveal : MonoBehaviour
{
    [Header("按顺序播放的文本")]
    [TextArea] public List<string> lines = new List<string>();

    [Header("播放设置")]
    public bool playOnStart = true;
    public float charFadeDuration = 0.2f;       // 单个字淡入时长
    public float charInterval = 0.05f;          // 字符之间的错开间隔

    [Header("行间隔")]
    public float lineInterval = 1f;             // 每行读完后的等待时间
    public bool autoNext = true;                // 自动播下一行；不勾则由外部调 Next()

    [Header("回调")]
    public UnityEvent<int, string> onLineComplete;  // 每句读完
    public UnityEvent onAllComplete;                // 全部读完

    Text txt;
    readonly List<float> alphas = new List<float>();
    readonly StringBuilder sb = new StringBuilder();
    string rgbHex = "FFFFFF";
    string currentRaw = "";     // 当前这一行的原始文本
    int index = -1;
    Sequence seq;
    Coroutine routine;
    bool waitingNext;

    void Awake()
    {
        txt = GetComponent<Text>();
        txt.supportRichText = true;
        Color c = txt.color;
        rgbHex = ((int)(c.r * 255)).ToString("X2")
               + ((int)(c.g * 255)).ToString("X2")
               + ((int)(c.b * 255)).ToString("X2");
    }

    void Start()
    {
        if (playOnStart && lines != null && lines.Count > 0) Play();
    }

    public void Play()
    {
        StopAll();
        routine = StartCoroutine(PlayRoutine());
    }

    public void StopAll()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        seq?.Kill();
        seq = null;
        waitingNext = false;
    }

    public void Next()
    {
        if (waitingNext) waitingNext = false;
    }

    /// <summary>外部调用，改变文字基础色（不含 alpha）。会立即重建文本。</summary>
    public void SetColor(Color c)
    {
        rgbHex = ((int)(c.r * 255)).ToString("X2")
               + ((int)(c.g * 255)).ToString("X2")
               + ((int)(c.b * 255)).ToString("X2");

        if (txt != null && currentRaw.Length > 0)
            txt.text = Build(currentRaw);
    }

    IEnumerator PlayRoutine()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            index = i;
            currentRaw = lines[i] ?? "";
            string raw = currentRaw;

            alphas.Clear();
            for (int k = 0; k < raw.Length; k++) alphas.Add(0f);
            txt.text = Build(raw);

            seq?.Kill();
            seq = DOTween.Sequence();
            for (int k = 0; k < raw.Length; k++)
            {
                int j = k;
                seq.Insert(j * charInterval,
                    DOTween.To(() => alphas[j],
                               v => { alphas[j] = v; txt.text = Build(raw); },
                               1f, charFadeDuration));
            }

            yield return seq.WaitForCompletion();

            onLineComplete?.Invoke(i, raw);

            if (i >= lines.Count - 1)
            {

                onAllComplete?.Invoke();
                yield break;
            }

            if (autoNext)
            {
                yield return new WaitForSeconds(lineInterval);
            }
            else
            {
                waitingNext = true;
                yield return new WaitUntil(() => !waitingNext);
            }
        }
    }

    string Build(string raw)
    {
        sb.Length = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c == ' ' || c == '\n' || c == '\r') { sb.Append(c); continue; }
            int a = Mathf.Clamp(Mathf.RoundToInt(alphas[i] * 255f), 0, 255);
            sb.Append("<color=#").Append(rgbHex).Append(a.ToString("X2")).Append('>')
              .Append(c).Append("</color>");
        }
        return sb.ToString();
    }
}
