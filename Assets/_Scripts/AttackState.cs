using System.Collections;
using UnityEngine;

public class AttackState : EnemyState
{
    private float timer;
    private bool isAttacking;

    public AttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.attackDuration;
        isAttacking = false;
        enemy.rb.velocity = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        timer -= Time.deltaTime;
        
        // 攻击期间也保持视觉更新
        enemy.UpdateVisuals();

        if (!isAttacking)
        {
            DoAttack();
            isAttacking = true;
        }

        if (timer <= 0)
        {
            stateMachine.ChangeState(enemy.chaseState);
        }
    }

    void DoAttack()
    {
        Collider2D collision = Physics2D.OverlapCircle(enemy.attackPos.position, enemy.attackRange, enemy.playerLayer);
        if (collision != null)
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(enemy.damage, enemy.transform.position, 10f);
            }
        }
    }

    public override void Exit()
    {
    }
}