using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionNode : BehaviourNode
{
    private Func<NodeState> action;

    public ActionNode(Func<NodeState> action)
    {
        this.action = action;
    }

    public override NodeState Evaluate()
    {
        return action();
    }
}
