using BehaviorTree;
using System.Collections.Generic;
using UnityEngine;

public class Emery : BehaviorTrees
{
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private Transform[] waypoints = null;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private EmeryAnimalContrller animationController;

    public EnemyConfig Config => enemyConfig;

    protected override void Onsetup()
    {
        if (animationController == null)
        {
            animationController = GetComponent<EmeryAnimalContrller>();
        }

        Blackboard.Add("speed", GetMoveSpeed());
        Blackboard.Add("attackRange", GetAttackRange());
        Blackboard.Add("attackDuration", GetAttackDuration());
        Blackboard.Add("attackCooldown", GetAttackCooldown());
        Blackboard.Add("seeRadius", GetSeeRadius());
        Blackboard.Add("obstacleMask", obstacleMask);
        Blackboard.Add("playerMask", playerMask);
        Blackboard.Add("anim", animationController);

        var partoltask = new Partoltask(waypoints);
        var attacktask = new Attacktask();
        var seePlayertask = new SeePlayertask();
        var chaseTask = new ChaseTask();
        Sequence chaseSequence = new Sequence(new List<Node>
        {
            seePlayertask,
            chaseTask
        });

        Root = new Selector(new List<Node>
        {
            attacktask,
            chaseSequence,
            partoltask
        });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float seeRadius = GetSeeRadius();
        Gizmos.DrawWireSphere(transform.position, seeRadius);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Vector2 direction = player.transform.position - transform.position;
        float distance = direction.magnitude;

        if (distance > seeRadius)
        {
            return;
        }

        direction.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, seeRadius, obstacleMask | playerMask);
        if (hit.collider == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(direction * seeRadius));
            return;
        }

        Gizmos.color = hit.collider.CompareTag("Player") ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, hit.point);
       
    }

    private float GetMoveSpeed()
    {
        return enemyConfig != null ? enemyConfig.MoveSpeed : 10f;
    }

    private float GetAttackRange()
    {
        return enemyConfig != null ? enemyConfig.AttackRange : 1.5f;
    }

    private float GetAttackDuration()
    {
        return enemyConfig != null ? enemyConfig.AttackDuration : 0.45f;
    }

    private float GetAttackCooldown()
    {
        return enemyConfig != null ? enemyConfig.AttackCooldown : 0.25f;
    }

    private float GetSeeRadius()
    {
        return enemyConfig != null ? enemyConfig.SeeRadius : 2f;
    }
}
