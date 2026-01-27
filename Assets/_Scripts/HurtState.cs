using UnityEngine;

public class HurtState : EnemyState
{
    private float hurtTimer;
    private float hurtDuration = 0.3f;

    public HurtState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        hurtTimer = hurtDuration;
        enemy.UpdateVisuals();
    }

    public override void LogicUpdate()
    {
        hurtTimer -= Time.deltaTime;

        if (hurtTimer <= 0)
        {
            if (enemy.player != null)
            {
                stateMachine.ChangeState(enemy.chaseState);
            }
            else
            {
                stateMachine.ChangeState(enemy.patrolState);
            }
        }
    }
}