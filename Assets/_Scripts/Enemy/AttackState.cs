using System.Collections;
using UnityEngine;

using AngryBirds.Core;

namespace AngryBirds.Enemy
{
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

            // 攻击期间禁用与玩家的物理碰撞，防止把玩家推开导致 AABB 打空
            if (enemy.bodyCollider != null && enemy.player != null)
            {
                Collider2D playerCol = enemy.player.GetComponent<Collider2D>();
                if (playerCol != null)
                    Physics2D.IgnoreCollision(enemy.bodyCollider, playerCol, true);
            }

            // 冲刺：向玩家方向爆发位移
            if (enemy.player != null)
            {
                Vector2 dir = (enemy.player.position - enemy.transform.position).normalized;
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
                if (stateMachine.CurrentState != this) return;
            }

            if (timer <= 0)
            {
                stateMachine.ChangeState(enemy.chaseState);
            }
        }

        void DoAttack()
        {
            if (enemy.player == null) return;

            // 获取玩家的碰撞盒
            Collider2D playerCol = enemy.player.GetComponent<Collider2D>();
            if (playerCol == null) return;

            // 构建敌人的攻击包围盒
            Bounds attackBounds;
            if (enemy.bodyCollider != null)
            {
                attackBounds = enemy.bodyCollider.bounds;
            }
            else if (enemy.attackPos != null)
            {
                float r = enemy.attackRange;
                attackBounds = new Bounds(enemy.attackPos.position, new Vector3(r * 2, r * 2, 0));
            }
            else return;

            // 层级检测
            if ((enemy.playerLayer.value & (1 << enemy.player.gameObject.layer)) == 0) return;

            // AABB 碰撞检测
            Bounds playerBounds = playerCol.bounds;
            if (attackBounds.min.x <= playerBounds.max.x
                && attackBounds.max.x >= playerBounds.min.x
                && attackBounds.min.y <= playerBounds.max.y
                && attackBounds.max.y >= playerBounds.min.y)
            {
                IDamageable target = enemy.player.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(enemy.damage, enemy.GetBackCenter(), 10f);
                    hasHit = true;
                }
            }
        }

        public override void Exit()
        {
            // 恢复与玩家的碰撞
            if (enemy.bodyCollider != null && enemy.player != null)
            {
                Collider2D playerCol = enemy.player.GetComponent<Collider2D>();
                if (playerCol != null)
                    Physics2D.IgnoreCollision(enemy.bodyCollider, playerCol, false);
            }

            // 攻击结束，立即减速，防止无限滑动
            enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
        }
    }
}
