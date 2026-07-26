using UnityEngine;

namespace DeadCells.Enemy
{
    public class ChaseState : EnemyState
{
    public ChaseState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine){}
    public override void Enter()
    {

    }
    public override void PhysicsUpdate()
    {
        if(enemy.player == null) return;
        enemy.UpdateVisuals();

        Vector2 dir = (enemy.player.position - enemy.transform.position).normalized;
        enemy.rb.velocity = new Vector2(dir.x * enemy.chaseSpeed, enemy.rb.velocity.y);
    }
    public override void LogicUpdate()
    {
        if (enemy.player == null)
        {
            stateMachine.ChangeState(enemy.patrolState);
            return;
        }

        if (!enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(enemy.patrolState);
            return;
        }

        Vector2 diff = enemy.transform.position - enemy.player.position;
        float distSqr = diff.sqrMagnitude;
        float chaseRangeSqr = enemy.chaseRange * enemy.chaseRange;

        float distFromStartX = Mathf.Abs(enemy.transform.position.x - enemy.startPos.x);

        if (distSqr > chaseRangeSqr || distFromStartX > enemy.territoryRange)
        {
            stateMachine.ChangeState(enemy.patrolState);
            return;
        }

        float attackRangeSqr = enemy.attackRange * enemy.attackRange;
        if (distSqr < attackRangeSqr && enemy.CanAttack())
        {
            stateMachine.ChangeState(enemy.telegraphState);
        }
    }
    public override void Exit()
    {

    }
}
}
