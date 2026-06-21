using UnityEngine;

using AngryBirds.AI.Node;
using AngryBirds.Core;

namespace AngryBirds.AI.BehaviourTree
{
    /// <summary>
    /// 将 BTConfig 编译为运行时 BehaviourNode 树。
    /// 条件/动作的闭包在这里生成，引用 BTExecutor 的组件和属性。
    /// </summary>
    public static class BTCompiler
    {
        /// <summary>编译整棵树，返回根节点</summary>
        public static BehaviourNode Compile(BTConfig config, BTExecutor exec)
        {
            if (config == null || config.nodes.Count == 0) return null;

            BTNodeData rootData = config.FindRoot();
            if (rootData == null)
            {
                Debug.LogError("[BTCompiler] 行为树配置没有根节点（无父节点的节点）");
                return null;
            }

            // 运行时高亮回调：包装节点，Evaluate 时更新 activeNodeGuid
            BehaviourNode root = BuildNode(rootData, config, exec);
            root.Blackboard = exec.Blackboard;
            return root;
        }

        private static BehaviourNode BuildNode(BTNodeData data, BTConfig config, BTExecutor exec)
        {
            int childCount = data.childGuids.Count;
            BehaviourNode[] children = new BehaviourNode[childCount];
            for (int i = 0; i < childCount; i++)
            {
                BTNodeData childData = config.FindByGuid(data.childGuids[i]);
                if (childData != null)
                    children[i] = BuildNode(childData, config, exec);
            }

            BehaviourNode node = data.nodeType switch
            {
                BTNodeType.Sequence => new SequenceNode(children),
                BTNodeType.Selector => new SelectorNode(children),
                BTNodeType.Condition => BuildCondition(data, exec),
                BTNodeType.Action => BuildAction(data, exec),
                _ => null,
            };

            // ★ 运行时高亮：每个节点 Evaluate 前标记 activeNodeGuid
            if (node != null)
            {
                string capturedGuid = data.guid;
                BTConfig capturedConfig = config;
                node.OnEvaluateStart = () => capturedConfig.activeNodeGuid = capturedGuid;
            }

            return node;
        }

        // ==================== 条件 ====================

        private static ConditionNode BuildCondition(BTNodeData data, BTExecutor exec)
        {
            return data.conditionType switch
            {
                BTConditionType.IsPlayerInRange => new ConditionNode(() =>
                {
                    Transform player = exec.Blackboard.Get<Transform>("player");
                    if (player == null) return false;
                    float range = data.conditionParam;
                    return (exec.transform.position - player.position).sqrMagnitude <= range * range;
                }),

                BTConditionType.IsHealthBelow => new ConditionNode(() =>
                {
                    return exec.CurrentHealth < exec.MaxHealth * data.conditionParam;
                }),

                BTConditionType.HasTarget => new ConditionNode(() =>
                {
                    return exec.Blackboard.Get<Transform>("player") != null;
                }),

                BTConditionType.IsCooldownReady => new ConditionNode(() =>
                {
                    return Time.time >= exec.LastActionTime + data.conditionParam;
                }),

                _ => new ConditionNode(() => false),
            };
        }

        // ==================== 动作 ====================

        private static ActionNode BuildAction(BTNodeData data, BTExecutor exec)
        {
            return data.actionType switch
            {
                BTActionType.MoveToPlayer => new ActionNode(() =>
                {
                    Transform player = exec.Blackboard.Get<Transform>("player");
                    if (player == null || exec.Rb == null) return NodeState.Failure;
                    float dir = Mathf.Sign(player.position.x - exec.transform.position.x);
                    exec.Rb.velocity = new Vector2(dir * data.actionParam, exec.Rb.velocity.y);
                    exec.FlipSprite(dir);
                    return NodeState.Success;
                }),

                BTActionType.MoveToStartPos => new ActionNode(() =>
                {
                    Vector3 start = exec.StartPosition;
                    float dir = Mathf.Sign(start.x - exec.transform.position.x);
                    if (Mathf.Abs(exec.transform.position.x - start.x) < 0.2f)
                    {
                        if (exec.Rb != null) exec.Rb.velocity = new Vector2(0, exec.Rb.velocity.y);
                        return NodeState.Success;
                    }
                    if (exec.Rb != null)
                        exec.Rb.velocity = new Vector2(dir * data.actionParam, exec.Rb.velocity.y);
                    exec.FlipSprite(dir);
                    return NodeState.Running;
                }),

                BTActionType.StandIdle => new ActionNode(() =>
                {
                    if (exec.Rb != null)
                        exec.Rb.velocity = new Vector2(0, exec.Rb.velocity.y);
                    return NodeState.Success;
                }),

                BTActionType.Attack => new ActionNode(() =>
                {
                    exec.LastActionTime = Time.time;
                    Transform player = exec.Blackboard.Get<Transform>("player");
                    if (player != null)
                    {
                        var dmg = player.GetComponent<IDamageable>();
                        dmg?.TakeDamage(data.actionParamInt, exec.transform.position, 5f);
                    }
                    return NodeState.Success;
                }),

                BTActionType.Patrol => new ActionNode(() =>
                {
                    Vector3 start = exec.StartPosition;
                    float amp = 3f;
                    float maxSpd = data.actionParam;
                    float omega = amp <= 0f ? 0f : maxSpd / amp;
                    float velX = omega <= 0f ? 0f : maxSpd * Mathf.Cos(Time.time * omega);
                    if (exec.Rb != null) exec.Rb.velocity = new Vector2(velX, exec.Rb.velocity.y);
                    exec.FlipSprite(Mathf.Sign(velX));
                    return NodeState.Running;
                }),

                _ => new ActionNode(() => NodeState.Failure),
            };
        }
    }
}
