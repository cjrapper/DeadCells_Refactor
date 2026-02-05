using System.Collections;
using UnityEngine;

public class AttackState : EnemyState
{
    private float timer;
    private bool hasHit;

    public AttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.attackDuration;
        hasHit = false;
        enemy.RegisterAttack();
        
        // 冲刺：向玩家方向爆发位移
        if (enemy.player != null)
        {
            Vector2 dir = (enemy.player.position - enemy.transform.position).normalized;
            // 史莱姆通常是水平冲刺，保持 y 轴当前速度（或给一个微小的跳跃感）
            enemy.rb.velocity = new Vector2(dir.x * enemy.lungeSpeed, enemy.rb.velocity.y);
        }
    }

    public override void LogicUpdate()
    {
        timer -= Time.deltaTime;
        
        // 攻击期间也保持视觉更新
        enemy.UpdateVisuals();

        if (!hasHit)
        {
            DoAttack();
        }

        if (timer <= 0)
        {
            stateMachine.ChangeState(enemy.chaseState);
        }
    }

    void DoAttack()
    {
        Collider2D collision = null;
        if (enemy.bodyCollider != null)
        {
            Bounds bounds = enemy.bodyCollider.bounds;
            collision = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, enemy.playerLayer);
        }
        else if (enemy.attackPos != null)
        {
            collision = Physics2D.OverlapCircle(enemy.attackPos.position, enemy.attackRange, enemy.playerLayer);
        }
        if (collision != null)
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(enemy.damage, enemy.GetBackCenter(), 10f);
                hasHit = true;
            }
        }
    }

    public override void Exit()
    {
        // 攻击结束，立即减速，防止无限滑动
        enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
    }
}