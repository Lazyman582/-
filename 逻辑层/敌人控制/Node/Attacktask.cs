using BehaviorTree;
using UnityEngine;

public class Attacktask : Task
{
    private float cooldownTimer;
    private float attackTimer;
    private bool isAttacking;
    private bool enteredAttackState;
    private GameObject currentTarget;

    protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
    {
        var anim = blackboard.Get<EmeryAnimalContrller>("anim");
        float attackRange = blackboard.Get<float>("attackRange");
        float attackDuration = Mathf.Max(0.05f, blackboard.Get<float>("attackDuration"));
        float attackCooldown = blackboard.Get<float>("attackCooldown");
        float seeRadius = blackboard.Get<float>("seeRadius");
        LayerMask obstacleMask = blackboard.Get<LayerMask>("obstacleMask");
        LayerMask playerMask = blackboard.Get<LayerMask>("playerMask");

        if (isAttacking)
        {
            return UpdateAttack(agent, anim, attackDuration, attackCooldown);
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            if (anim != null)
            {
                anim.SetAttack(false);
                anim.SetMove(false);
            }

            if (cooldownTimer > 0f)
            {
                return Staus.Running;
            }

            cooldownTimer = 0f;
            currentTarget = null;
            return Staus.Failure;
        }

        GameObject player = FindVisiblePlayer(agent, blackboard, seeRadius, obstacleMask, playerMask);
        if (player == null)
        {
            ResetAttack(anim);
            blackboard.Remove("player");
            return Staus.Failure;
        }

        blackboard.Add("player", player);
        currentTarget = player;

        if (Vector2.Distance(agent.position, player.transform.position) > attackRange)
        {
            if (anim != null)
            {
                anim.SetAttack(false);
            }

            return Staus.Failure;
        }

        isAttacking = true;
        enteredAttackState = false;
        attackTimer = 0f;
        return UpdateAttack(agent, anim, attackDuration, attackCooldown);
    }

    private Staus UpdateAttack(Transform agent, EmeryAnimalContrller anim, float attackDuration, float attackCooldown)
    {
        attackTimer += Time.deltaTime;

        if (anim != null)
        {
            anim.SetMove(false);
            anim.SetAttack(true);

            if (currentTarget != null)
            {
                anim.FaceTarget(agent.position, currentTarget.transform.position);
            }

            if (anim.IsInAttackState())
            {
                enteredAttackState = true;
            }

            if (enteredAttackState && anim.IsAttackAnimationFinished())
            {
                BeginRecovery(anim, attackCooldown);
                return Staus.Running;
            }
        }
        else if (attackTimer >= attackDuration)
        {
            BeginRecovery(null, attackCooldown);
            return Staus.Running;
        }

        return Staus.Running;
    }

    private GameObject FindVisiblePlayer(Transform agent, Blackboard blackboard, float seeRadius, LayerMask obstacleMask, LayerMask playerMask)
    {
        GameObject player = blackboard.Get<GameObject>("player");
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player == null)
        {
            return null;
        }

        Vector2 direction = player.transform.position - agent.position;
        float distance = direction.magnitude;
        if (distance > seeRadius)
        {
            return null;
        }

        direction.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(agent.position, direction, seeRadius, obstacleMask | playerMask);
        if (hit.collider == null || !hit.collider.CompareTag("Player"))
        {
            return null;
        }

        return player;
    }

    private void BeginRecovery(EmeryAnimalContrller anim, float attackCooldown)
    {
        isAttacking = false;
        enteredAttackState = false;
        attackTimer = 0f;
        cooldownTimer = Mathf.Max(0f, attackCooldown);

        if (anim != null)
        {
            anim.SetAttack(false);
            anim.SetMove(false);
        }
    }

    private void ResetAttack(EmeryAnimalContrller anim)
    {
        isAttacking = false;
        enteredAttackState = false;
        attackTimer = 0f;
        currentTarget = null;

        if (anim != null)
        {
            anim.SetAttack(false);
            anim.SetMove(false);
        }
    }
}
