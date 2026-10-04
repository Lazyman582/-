using BehaviorTree;
using UnityEngine;

/// <summary>
/// 死亡分支：blackboard 里的 isDead 为 true 时接管行为树。
/// 放在根 Selector 的最前面——死亡期间每帧返回 Running，
/// 攻击/追击/巡逻分支全部不再求值，动画由这里统一驱动。
/// </summary>
public class DeathTask : Task
{
    protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
    {
        if (!blackboard.Get<bool>("isDead"))
        {
            return Staus.Failure;
        }

        var anim = blackboard.Get<EmeryAnimalContrller>("anim");
        if (anim != null)
        {
            anim.SetDeath(true);
            anim.SetMove(false);
            anim.SetAttack(false);
        }

        return Staus.Running;
    }
}
