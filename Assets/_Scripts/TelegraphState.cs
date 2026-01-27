using UnityEngine;

public class TelegraphState : EnemyState
{
    private float timer;

    public TelegraphState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.windupTime;
        enemy.rb.velocity = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        timer -= Time.deltaTime;
        
        enemy.UpdateVisuals();

        if (timer <= 0)
        {
            stateMachine.ChangeState(enemy.attackState);
        }
    }

    public override void Exit()
    {
    }
}
