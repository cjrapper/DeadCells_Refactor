using System.Collections;
using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    public class AttackState : EnemyState
    {
        public override string AnimName => "Attack";
        private float timer;
        private bool hasHit;
        private Vector2 dashDir;   // 冲刺方向（Enter 时锁定，冲撞不跟手变向）

        public AttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            timer = enemy.attackDuration;
            hasHit = false;
            enemy.RegisterAttack();

            // 攻击期间禁用与玩家的物理碰撞，防止把玩家推开导致判定打空
            if (enemy.bodyCollider != null && enemy.player != null)
            {
                Collider2D playerCol = enemy.player.GetComponent<Collider2D>();
                if (playerCol != null)
                    Physics2D.IgnoreCollision(enemy.bodyCollider, playerCol, true);
            }

            // 冲刺：向玩家方向爆发位移，并锁定方向
            if (enemy.player != null)
            {
                dashDir = ((Vector2)(enemy.player.position - enemy.transform.position)).normalized;
                enemy.rb.velocity = new Vector2(dashDir.x * enemy.lungeSpeed, enemy.rb.velocity.y);
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
            else
            {
                // 已撞到：保持停住（DoAttack 命中时已清零速度），等攻击时长走完回归
                // （回归状态走虚方法：地面怪回 chase，飞行怪回飞行轨道）
                if (timer <= 0)
                    stateMachine.ChangeState(enemy.GetCombatReturnState());
            }
        }

        private static readonly Collider2D[] hitBuffer = new Collider2D[8];

        void DoAttack()
        {
            if (enemy.player == null) return;

            // 层级检测（playerLayer 为 Nothing 时 Enemy.Awake 已兜底）
            if ((enemy.playerLayer.value & (1 << enemy.player.gameObject.layer)) == 0) return;

            // 冲撞判定：检测点朝冲刺方向前移半个判定半径（"撞头"优先判定，更灵敏），
            // 半径 = attackRange。用 OverlapCircle 而不是身体 AABB —— 冲刺期间已
            // IgnoreCollision 与玩家解耦，但 IgnoreCollision 不影响 OverlapCircle 查询。
            // 命中即停：撞到就清零速度，形成"冲撞"手感（而不是推着玩家跑）
            Vector2 center = (Vector2)enemy.transform.position + dashDir * (enemy.attackRange * 0.5f);

            // ★ 必须用 NonAlloc 把范围内的碰撞体【全部】拿出来扫一遍。
            //   旧代码用单数的 Physics2D.OverlapCircle —— 它只返回"其中一个"碰撞体，
            //   返回哪个是不确定的；如果拿到的是玩家身上没有 IDamageable 的那个子碰撞体，
            //   这一帧就直接判空了。表现就是"明明撞到了却没伤害"（冲撞经常打不中）。
            int count = Physics2D.OverlapCircleNonAlloc(center, enemy.attackRange, hitBuffer, enemy.playerLayer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = hitBuffer[i];
                if (col == null) continue;

                // GetComponentInParent：IDamageable 挂在根节点上，碰撞体可能在子物体上，
                // 用 GetComponent 会漏掉
                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                target.TakeDamage(enemy.damage, enemy.GetBackCenter(), 10f);
                hasHit = true;
                enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);  // 撞到即停
                break;
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
