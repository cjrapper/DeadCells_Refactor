using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BehaviourNode
{
    protected List<BehaviourNode> children = new ();
    private Blackboard blackboard;
    public Blackboard Blackboard{
        get => blackboard;
        set {
            blackboard = value;
            for (int i = 0; i < children.Count; i++)
                children[i].Blackboard = value;
        }
    }

    public BehaviourNode(){}
    public BehaviourNode(params BehaviourNode[] nodes)
    {
        children.AddRange(nodes);
    }
    public abstract NodeState Evaluate();//abstract没咋用过，后面NodeState后为啥还能跟Evaluate啊？
}
