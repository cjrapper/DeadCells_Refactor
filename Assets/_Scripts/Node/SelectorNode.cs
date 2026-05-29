using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectorNode : BehaviourNode
{
    public SelectorNode(params BehaviourNode[] nodes) : base(nodes) { }
    public override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Evaluate();
            if (state != NodeState.Failure)
                return state;
        }
        return NodeState.Failure;
    }
}
