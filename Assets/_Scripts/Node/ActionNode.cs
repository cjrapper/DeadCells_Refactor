using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryBirds.AI.Node
{
    public class ActionNode : BehaviourNode
    {
        private Func<NodeState> action;

        public ActionNode(Func<NodeState> action)
        {
            this.action = action;
        }

        protected override NodeState Evaluate()
        {
            return action();
        }
    }
}
