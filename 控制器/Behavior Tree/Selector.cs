using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BehaviorTree
{
    public class Selector : Composer
    {
        protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
        {
            bool isRunning = false;
            bool failed = children.All(child => {

                Staus staus = child.Evaluate(agent, blackboard);
                if (staus == Staus.Running) isRunning = true;

                return staus == Staus.Failure;
            });






            return isRunning ? Staus.Running : failed ? Staus.succese : Staus.Failure;
        }
    }
}

