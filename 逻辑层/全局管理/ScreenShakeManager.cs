using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 屏幕抖动管理器 — 单例，生成抖动向量供相机等做坐标偏移
/// </summary>
public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager Instance { get; private set; }

    [Header("全局设置")]
    [Tooltip("全局震动强度，所有抖动的总乘数")]
    [SerializeField] private float globalIntensity = 1f;

    /// <summary>本帧的抖动偏移向量（多个抖动叠加的结果）</summary>
    public Vector3 ShakeOffset { get; private set; }

    /// <summary>正在进行的抖动列表，多个抖动可同时存在</summary>
    private readonly List<ShakeInfo> shakes = new();

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

    void Update()
    {
        if (shakes.Count == 0)
        {
            ShakeOffset = Vector3.zero;
            return;
        }

        Vector3 offset = Vector3.zero;

        for (int i = shakes.Count - 1; i >= 0; i--)
        {
            var shake = shakes[i];
            shake.t += TimeManager.GameplayDT;

            float progress = shake.t / shake.duration;
            if (progress >= 1f)
            {
                shakes.RemoveAt(i); // 时间到，移除该抖动
                continue;
            }

            // 正弦波 + 线性衰减：progress 越大振幅越小
            float decay = 1f - progress;
            float wave = Mathf.Sin(progress * Mathf.PI * 2f * shake.times);
            offset += shake.direction.normalized * wave * shake.intensity * decay;
        }

        ShakeOffset = offset * globalIntensity;
    }

    /// <summary>触发一次抖动</summary>
    public void Shake(float intensity, int times, float duration, Vector3 direction)
    {
        shakes.Add(new ShakeInfo
        {
            intensity = intensity,
            times = times,
            duration = duration,
            direction = direction,
            t = 0f
        });
    }

    /// <summary>清空所有正在进行的抖动</summary>
    public void StopAllShakes()
    {
        shakes.Clear();
        ShakeOffset = Vector3.zero;
    }
}

/// <summary>
/// 单次抖动的信息
/// </summary>
[System.Serializable]
public class ShakeInfo
{
    [Tooltip("抖动强度（振幅）")] public float intensity;
    [Tooltip("抖动次数（震荡次数）")] public int times;
    [Tooltip("持续时间（秒）")] public float duration;
    [Tooltip("抖动方向")] public Vector3 direction;
    [Tooltip("已进行的时间（由管理器累加）")] public float t;
}
