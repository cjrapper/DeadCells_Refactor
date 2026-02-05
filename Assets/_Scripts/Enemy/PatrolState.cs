using System;
using UnityEngine;

public class PatrolState : EnemyState
{
    public PatrolState(Enemy enemy, EnemyStateMachine stateMachine):base(enemy, stateMachine) {}
    public override void Enter()
    {
        // enemy.animator.SetBool("IsPatrolling", true);
    }
    public override void LogicUpdate()
    {
        if(Vector2.Distance(enemy.transform.position, enemy.player.position) < enemy.chaseRange)
        {
            stateMachine.ChangeState(enemy.chaseState);
        }
        
    }
    public override void PhysicsUpdate()
    {
        enemy.UpdateVisuals();
        float patrolAmplitude = 3f;
        float maxSpeed = enemy.moveSpeed * 0.5f;
        float omega = patrolAmplitude <= 0f ? 0f : maxSpeed / patrolAmplitude;
        float velocityX = maxSpeed * MathF.Cos(Time.time * omega);
        enemy.rb.velocity = new Vector2(velocityX, enemy.rb.velocity.y);
    }
    public override void Exit()
    {
        // enemy.animator.SetBool("IsPatrolling", false);
    }
}
