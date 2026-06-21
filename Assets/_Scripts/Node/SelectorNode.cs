using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryBirds.AI.Node
{
    public class SelectorNode : BehaviourNode
{
    public SelectorNode(params BehaviourNode[] nodes) : base(nodes) { }
    protected override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Tick();
            if (state != NodeState.Failure)
                return state;
        }
        return NodeState.Failure;
    }
}
}
