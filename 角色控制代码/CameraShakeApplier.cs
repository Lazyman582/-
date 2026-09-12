using UnityEngine;

/// <summary>
/// 屏幕抖动应用器 — 挂在 Main Camera 上。
/// CinemachineBrain 每帧覆盖相机位置，所以必须在其之后（LateUpdate + 高执行顺序）
/// 把 ScreenShakeManager 的抖动偏移叠加到相机上。
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraShakeApplier : MonoBehaviour
{
    private void LateUpdate()
    {
        if (ScreenShakeManager.Instance == null) return;

        transform.position += ScreenShakeManager.Instance.ShakeOffset;
    }
}
