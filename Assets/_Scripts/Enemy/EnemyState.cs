public class EnemyState //基础状态类
{
    protected Enemy enemy;
    protected EnemyStateMachine stateMachine;

    public EnemyState(Enemy enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }


    //规定子类实现的接口方法
    public virtual void Enter(){}
    public virtual void Exit(){}
    public virtual void PhysicsUpdate(){}
    public virtual void LogicUpdate(){}
}
