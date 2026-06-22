using BehaviorTree;
using UnityEngine;

public class ChaseTask : Task
{
    protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
    {
        var player = blackboard.Get<GameObject>("player");
        var speed = blackboard.Get<float>("speed");
        var anim = blackboard.Get<EmeryAnimalContrller>("anim");

        if (player == null)
        {
            if (anim != null)
            {
                anim.SetMove(false);
                anim.SetAttack(false);
            }

            return Staus.Failure;
        }

        var position = Vector2.MoveTowards(agent.position, player.transform.position, Time.deltaTime * speed);
        agent.position = new Vector3(position.x, agent.position.y, agent.position.z);

        if (anim != null)
        {
            anim.SetMove(true);
            anim.SetAttack(false);
            anim.FaceTarget(agent.position, player.transform.position);
        }

        return Staus.Running;
    }
}
