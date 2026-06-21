using System;
using UnityEngine;

namespace AngryBirds.Enemy
{
    public class PatrolState : EnemyState
{
    public PatrolState(Enemy enemy, EnemyStateMachine stateMachine):base(enemy, stateMachine) {}
    public override void Enter()
    {
        // enemy.animator.SetBool("IsPatrolling", true);
    }
    public override void LogicUpdate()
    {
        if (enemy.player == null) return;
        if (!enemy.CanSeePlayer()) return;

        float distFromStartX = Mathf.Abs(enemy.transform.position.x - enemy.startPos.x);
        if (distFromStartX > enemy.territoryRange * 0.5f) return;

        float chaseRangeSqr = enemy.chaseRange * enemy.chaseRange;
        if ((enemy.transform.position - enemy.player.position).sqrMagnitude < chaseRangeSqr)
        {
            stateMachine.ChangeState(enemy.chaseState);
        }
    }
    public override void PhysicsUpdate()
    {
        enemy.UpdateVisuals();

        float toStartX = enemy.startPos.x - enemy.transform.position.x;
        if (Mathf.Abs(toStartX) > 4f)
        {
            float dirX = Mathf.Sign(toStartX);
            enemy.rb.velocity = new Vector2(dirX * enemy.moveSpeed * 0.5f, enemy.rb.velocity.y);
            return;
        }

        float patrolAmplitude = 3f;
        float maxSpeed = enemy.moveSpeed * 0.5f;
        // 守护：amplitude 无效时停止巡逻，而不是朝一个方向匀速漂移
        if (patrolAmplitude <= 0f || maxSpeed <= 0f)
        {
            enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
            return;
        }
        float omega = maxSpeed / patrolAmplitude;
        float velocityX = maxSpeed * MathF.Cos(Time.time * omega);
        enemy.rb.velocity = new Vector2(velocityX, enemy.rb.velocity.y);
    }
    public override void Exit()
    {
        // enemy.animator.SetBool("IsPatrolling", false);
    }
}
}
