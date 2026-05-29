using System;
using System.Collections.Generic;
using UnityEngine;

public class SequenceNode : BehaviourNode
{
    public SequenceNode(params BehaviourNode[] nodes) : base(nodes) { }
    public override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Evaluate();
            if (state != NodeState.Success)
                return state;
        }
        return NodeState.Success;
    }
}