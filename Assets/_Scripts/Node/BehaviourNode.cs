using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryBirds.AI.Node
{
    public abstract class BehaviourNode
    {
        protected List<BehaviourNode> children = new();
        private Blackboard blackboard;
        public Blackboard Blackboard
        {
            get => blackboard;
            set
            {
                blackboard = value;
                for (int i = 0; i < children.Count; i++)
                    children[i].Blackboard = value;
            }
        }

        public BehaviourNode() { }
        public BehaviourNode(params BehaviourNode[] nodes)
        {
            children.AddRange(nodes);
        }

        /// <summary>每帧 Evaluate 前触发，编辑器用做运行时高亮</summary>
        public System.Action OnEvaluateStart;

        /// <summary>外部调用入口（BTExecutor 调这个，别直接调 Evaluate）</summary>
        public NodeState Tick()
        {
            OnEvaluateStart?.Invoke();
            return Evaluate();
        }

        protected abstract NodeState Evaluate();
    }
}
