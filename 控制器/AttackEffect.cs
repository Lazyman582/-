using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackEffects : MonoBehaviour
{
    public GameObject slashEffect;  // 刀光特效

    // 在动画事件中调用这个方法
    public void ShowSlashEffect()
    {
        slashEffect.SetActive(true);

        // 如果特效有自己的动画，重置它
        Animator effectAnim = slashEffect.GetComponent<Animator>();
        if (effectAnim != null)
            effectAnim.Play("Slash", 0, 0);
    }

    // 在动画结束时调用
    public void HideSlashEffect()
    {
        slashEffect.SetActive(false);
    }
}