using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class Composer : Node
    {
        protected Composer(List<Node> nodes)
        {
            if (nodes != null)
            {
                children.AddRange(nodes);
            }
        }
    }
}
