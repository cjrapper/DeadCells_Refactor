using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.AI.Node
{
    public class SequenceNode : BehaviourNode
{
    public SequenceNode(params BehaviourNode[] nodes) : base(nodes) { }
    protected override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Tick();
            if (state != NodeState.Success)
                return state;
        }
        return NodeState.Success;
    }
}}
