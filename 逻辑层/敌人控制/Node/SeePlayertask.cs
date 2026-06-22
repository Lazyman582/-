using BehaviorTree;
using UnityEngine;

public class SeePlayertask : Task
{
    protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
    {
        var anim = blackboard.Get<EmeryAnimalContrller>("anim");
        var player = GameObject.FindGameObjectWithTag("Player");
        float seeRadius = blackboard.Get<float>("seeRadius");
        LayerMask obstacleMask = blackboard.Get<LayerMask>("obstacleMask");
        LayerMask playerMask = blackboard.Get<LayerMask>("playerMask");

        if (player == null)
        {
            blackboard.Remove("player");

            if (anim != null)
            {
                anim.SetAttack(false);
            }

            return Staus.Failure;
        }

        Vector2 direction = player.transform.position - agent.position;
        float distance = direction.magnitude;

        if (distance > seeRadius)
        {
            blackboard.Remove("player");
            return Staus.Failure;
        }

        direction.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(agent.position, direction, seeRadius, obstacleMask | playerMask);
        if (hit.collider == null || !hit.collider.CompareTag("Player"))
        {
            blackboard.Remove("player");
            return Staus.Failure;
        }

        blackboard.Add("player", player);
        return Staus.succese;
    }
}
