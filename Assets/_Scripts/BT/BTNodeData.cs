using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryBirds.AI.BehaviourTree
{
    /// <summary>单个行为树节点的可序列化数据，存入 BTConfig。</summary>
    [Serializable]
    public class BTNodeData
    {
        // ---- 唯一标识（连线用） ----
        public string guid = Guid.NewGuid().ToString("N").Substring(0, 8);

        // ---- 编辑器显示 ----
        public string comment;          // 节点备注
        public Vector2 editorPosition;  // 在编辑器画布上的位置

        // ---- 节点类型 ----
        public BTNodeType nodeType;

        // ---- 组合节点专用：子节点 guid 列表 ----
        public List<string> childGuids = new();

        // ---- 条件节点专用 ----
        public BTConditionType conditionType;
        public float conditionParam = 3f;   // 通用浮点参数（距离/百分比/冷却时间）

        // ---- 动作节点专用 ----
        public BTActionType actionType;
        public float actionParam = 3f;      // 速度等
        public int actionParamInt = 10;     // 伤害等整型参数

        /// <summary>编辑器里显示的名字</summary>
        public string DisplayName
        {
            get
            {
                string baseName = nodeType switch
                {
                    BTNodeType.Sequence  => "→ Sequence",
                    BTNodeType.Selector  => "? Selector",
                    BTNodeType.Condition => $"🔍 {conditionType}",
                    BTNodeType.Action    => $"⚡ {actionType}",
                    _                    => "Node"
                };
                return string.IsNullOrEmpty(comment) ? baseName : $"{baseName}\n// {comment}";
            }
        }

        /// <summary>判断 guid 是否匹配（忽略大小写，容忍截断）</summary>
        public bool GuidMatches(string otherGuid)
            => !string.IsNullOrEmpty(otherGuid)
            && guid.StartsWith(otherGuid, StringComparison.OrdinalIgnoreCase);
    }
}
