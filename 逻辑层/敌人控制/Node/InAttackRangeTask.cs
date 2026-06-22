using BehaviorTree;
using UnityEngine;

public class InAttackRangeTask : Task
{
    protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
    {
        var player = blackboard.Get<GameObject>("player");
        var attackRange = blackboard.Get<float>("attackRange");

        if (player == null)
        {
            return Staus.Failure;
        }

        float distance = Vector2.Distance(agent.position, player.transform.position);
        return distance <= attackRange ? Staus.succese : Staus.Failure;
    }
}
