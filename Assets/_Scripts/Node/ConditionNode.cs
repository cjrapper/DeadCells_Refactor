using System;
using System.Collections.Generic;
using UnityEngine;

public class ConditionNode : BehaviourNode
{
    private Func<bool> condition;

    public ConditionNode(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override NodeState Evaluate()
    {
        return condition() ? NodeState.Success : NodeState.Failure;
    }
}
