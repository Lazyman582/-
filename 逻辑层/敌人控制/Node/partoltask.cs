using BehaviorTree;
using UnityEngine;

public class Partoltask : Task
{
    private int currentindex;
    private readonly Transform[] waypoints;
    private float speed = 10;

    public Partoltask(Transform[] waypoints)
    {
        this.waypoints = waypoints;
        currentindex = 0;
    }

    protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return Staus.Failure;
        }

        var currentWaypoint = waypoints[currentindex];
        bool arriver = Vector2.Distance(agent.position, currentWaypoint.position) < 0.1f;
        if (arriver)
        {
            ++currentindex;
            currentindex %= waypoints.Length;
            currentWaypoint = waypoints[currentindex];
        }

        var anim = blackboard.Get<EmeryAnimalContrller>("anim");
        var blackboardSpeed = blackboard.Get<float>("speed");
        if (blackboardSpeed > 0f)
        {
            speed = blackboardSpeed;
        }

        agent.position = Vector2.MoveTowards(agent.position, currentWaypoint.position, Time.deltaTime * speed);

        if (anim != null)
        {
            anim.SetMove(true);
            anim.FaceTarget(agent.position, currentWaypoint.position);
            anim.SetAttack(false);
        }

        return Staus.Running;
    }
}
