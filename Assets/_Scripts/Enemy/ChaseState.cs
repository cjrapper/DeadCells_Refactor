using UnityEngine;

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
        enemy.rb.velocity = new Vector2(dir.x * enemy.moveSpeed, enemy.rb.velocity.y);
    }
    public override void LogicUpdate()
    {
        if(enemy.player == null)
        {
            stateMachine.ChangeState(enemy.patrolState);
            return;
        }
        float dist = Vector2.Distance(enemy.transform.position, enemy.player.position);
        if(dist > enemy.chaseRange)
        {
            stateMachine.ChangeState(enemy.patrolState);
        }
        else if(dist < enemy.attackRange && enemy.CanAttack())
        {
            stateMachine.ChangeState(enemy.telegraphState);
        }
    }
    public override void Exit()
    {

    }
}
