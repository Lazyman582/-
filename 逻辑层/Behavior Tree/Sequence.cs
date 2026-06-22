using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class Sequence : Composer
    {
        public Sequence(List<Node> nodes) : base(nodes)
        {
        }

        protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
        {
            bool isRunning = false;

            foreach (Node child in children)
            {
                Staus s = child.Evaluate(agent, blackboard);
                if (s == Staus.Failure)
                {
                    return Staus.Failure;
                }

                if (s == Staus.Running)
                {
                    isRunning = true;
                }
            }

            return isRunning ? Staus.Running : Staus.succese;
        }
    }
}
