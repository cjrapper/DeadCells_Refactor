namespace DeadCells.Enemy
{
    public class EnemyState //基础状态类
    {
        protected Enemy enemy;
        protected EnemyStateMachine stateMachine;

        public EnemyState(Enemy enemy, EnemyStateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        /// <summary>
        /// 本状态对应的 Animator 动画状态名（各状态覆写；空 = 不播动画）。
        /// 状态机在 ChangeState 时自动 CrossFade。
        /// </summary>
        public virtual string AnimName => "";


        //规定子类实现的接口方法
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void PhysicsUpdate() { }
        public virtual void LogicUpdate() { }
    }
}
