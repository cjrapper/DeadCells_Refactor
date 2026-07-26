using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.AI.Node
{
    public class ConditionNode : BehaviourNode
    {
        private Func<bool> condition;

        public ConditionNode(Func<bool> condition)
        {
            this.condition = condition;
        }

        protected override NodeState Evaluate()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }
}
