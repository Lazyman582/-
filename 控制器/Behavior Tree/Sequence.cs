using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlasticGui.LaunchDiffParameters;

namespace BehaviorTree
{
    public class Sequence : Composer
    {
        protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
        {
            bool isRunning = false;
            bool success =false;

            foreach (Node child in children) {

                Staus s = child.Evaluate(agent, blackboard);
            if(s==Staus.Failure)return Staus.Failure;
            if(s==Staus.Running) isRunning = true;
            
            }

            children.All(child => child.Evaluate(agent, blackboard) == Staus.succese);
            

            return isRunning ? Staus.Running : success ? Staus.succese : Staus.Failure;
        }
    }
}
