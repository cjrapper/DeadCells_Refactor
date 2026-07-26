using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeadCells.AI.BehaviourTree
{
    /// <summary>
    /// 行为树配置 ScriptableObject。
/// 在 BTEditorWindow 里编辑，运行时由 BTCompiler 编译为 BehaviourNode 树。
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "NewBT", menuName = "AI/行为树配置")]
#endif
    public class BTConfig : ScriptableObject
{
    public List<BTNodeData> nodes = new();

    /// <summary>编辑器正在运行（Play Mode）时，当前激活节点 guid</summary>
    [System.NonSerialized]
    public string activeNodeGuid;

    /// <summary>清除运行时状态</summary>
    public void ResetRuntimeState()
    {
        activeNodeGuid = null;
    }

    /// <summary>查找根节点（没有父节点的那个）</summary>
    public BTNodeData FindRoot()
    {
        if (nodes.Count == 0) return null;
        var childGuids = new HashSet<string>(nodes.SelectMany(n => n.childGuids));
        return nodes.Find(n => !childGuids.Contains(n.guid));
    }

    /// <summary>按 guid 查节点</summary>
    public BTNodeData FindByGuid(string guid)
    {
        return nodes.Find(n => n.GuidMatches(guid));
    }
}
}