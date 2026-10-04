using System.Collections;
using UnityEngine;

/// <summary>
/// 开场过场控制器：玩家出生后播放入场动画（如 indoor），期间锁定输入，
/// 动画播完自动解锁。仅初次进入游戏触发（static 标记，关卡重载后不再播）。
/// 若要"永久只播一次"（含重启游戏后），把 static 标志换成 PlayerPrefs。
/// </summary>
public class IntroPlayController : MonoBehaviour
{
    [Header("开场动画")]
    [SerializeField] private string introAnimName = "indoor";
    [SerializeField] private float timeout = 25f;

    // static：关卡卸载重载后仍保留，从别的关卡返回时不重播
    private static bool _introPlayedOnce = false;

    private void Start()
    {
        if (_introPlayedOnce)
        {
            enabled = false;
            return;
        }
        StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        // 等两帧：确保 PersistentPlayer 完成出生、StateController 完成初始化
        yield return null;
        yield return null;

        var animator = CharacterMovement.Instance != null ? CharacterMovement.Instance.Animator : null;
        if (animator == null)
        {
            // 没有 Animator 就不锁输入了，避免无谓卡满超时
            Debug.LogWarning("IntroPlayController: 找不到玩家的 Animator，跳过开场动画");
            _introPlayedOnce = true;
            enabled = false;
            yield break;
        }

        // 锁定输入（UserInput.stop 会屏蔽移动/跳跃/攻击/闪避/下蹲）
        if (UserInput.Instance != null)
        {
            UserInput.Instance.stop = true;
        }

        // 播放入场动画
        animator.CrossFade(introAnimName, 0.1f);

        // 等动画播完（超时兜底：动画名写错时也能解锁，不会卡死）
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(introAnimName) && state.normalizedTime >= 1f)
            {
                break;
            }
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning($"IntroPlayController: 开场动画 {introAnimName} 播放超时，请检查动画名或动画机连线");
        }

        // 解锁，进入正常游戏
        if (UserInput.Instance != null)
        {
            UserInput.Instance.stop = false;
        }
        _introPlayedOnce = true;
        enabled = false;
    }
}
