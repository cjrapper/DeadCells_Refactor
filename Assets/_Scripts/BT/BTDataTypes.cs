namespace DeadCells.AI.BehaviourTree
{
    /// <summary>行为树节点类型</summary>
    public enum BTNodeType
    {
        Sequence,   // 顺序执行，任一失败则失败
        Selector,   // 选择执行，任一成功则成功
        Condition,  // 条件判断（叶子）
        Action,     // 行为动作（叶子）
    }

    /// <summary>条件类型 — 对应 Func&lt;bool&gt; 的预制实现</summary>
    public enum BTConditionType
    {
        IsPlayerInRange,       // 玩家在范围内？参数: float 距离
        IsHealthBelow,         // 生命值低于？参数: float 百分比(0-1)
        HasTarget,             // 有目标？无参数
        IsCooldownReady,       // 冷却就绪？参数: float 冷却时间
    }

    /// <summary>动作类型 — 对应 Func&lt;NodeState&gt; 的预制实现</summary>
    public enum BTActionType
    {
        MoveToPlayer,          // 移向玩家，参数: float 速度
        MoveToStartPos,        // 返回起始位置，参数: float 速度
        StandIdle,             // 站立不动，无参数
        Attack,                // 攻击，参数: int 伤害值
        Patrol,                // 巡逻（余弦振荡），参数: float 速度
    }
}
