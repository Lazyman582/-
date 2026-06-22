using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class Selector : Composer
    {
        public Selector(List<Node> nodes) : base(nodes)
        {
        }

        protected override Staus OnEvaluate(Transform agent, Blackboard blackboard)
        {
            foreach (Node child in children)
            {
                Staus status = child.Evaluate(agent, blackboard);
                if (status == Staus.succese)
                {
                    return Staus.succese;
                }

                if (status == Staus.Running)
                {
                    return Staus.Running;
                }
            }

            return Staus.Failure;
        }
    }
}
