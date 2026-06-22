using BehaviorTree;
using System.Collections.Generic;
using UnityEngine;

public class Emery : BehaviorTrees
{
    [SerializeField] private Transform[] waypoints = null;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDuration = 0.45f;
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private float seeRadius = 2f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private EmeryAnimalContrller animationController;

    protected override void Onsetup()
    {
        if (animationController == null)
        {
            animationController = GetComponent<EmeryAnimalContrller>();
        }

        Blackboard.Add("speed", speed);
        Blackboard.Add("attackRange", attackRange);
        Blackboard.Add("attackDuration", attackDuration);
        Blackboard.Add("attackCooldown", attackCooldown);
        Blackboard.Add("seeRadius", seeRadius);
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
}
