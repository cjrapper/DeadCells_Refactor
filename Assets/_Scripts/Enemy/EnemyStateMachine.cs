public class EnemyStateMachine
{
    //记住当前状态
    public EnemyState CurrentState{ get; private set; }
    //初始化
    public void Initialize(EnemyState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    //切换状态
    public void ChangeState(EnemyState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }
}
