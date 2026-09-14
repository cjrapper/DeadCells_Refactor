namespace DeadCells.Enemy
{
    public class EnemyStateMachine
{
    private readonly Enemy enemy;   // 持有敌人引用：状态切换时统一播动画

    public EnemyStateMachine(Enemy enemy)
    {
        this.enemy = enemy;
    }

    //记住当前状态
    public EnemyState CurrentState{ get; private set; }
    //初始化
    public void Initialize(EnemyState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
        enemy?.PlayStateAnim(startState.AnimName);
    }

    //切换状态
    public void ChangeState(EnemyState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();

        // 状态切换动画：每个状态覆写 AnimName（如 "Patrol"），
        // 未配置 Animator 或 AnimName 为空时无副作用
        enemy?.PlayStateAnim(newState.AnimName);
    }
}
}
