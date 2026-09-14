using UnityEngine;

using DeadCells.Player;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 飞行追踪：立体移动（X/Y 都朝玩家）+ 距离曲线（太近减速太远加速）
    /// + 攻击选集：贴脸 → 冲撞（复用 AttackState）；中远 → 火球。
    /// 玩家冲刺中：不触发攻击（冲刺无敌帧：火球不锁定、冲撞不发动）。
    /// </summary>
    public class FlyingFlightState : EnemyState
    {
        // 与待机共用同一段悬浮动画。
        // ★ 这里以前写的是 "Fly"，但 FlyingEnemy.controller 里的状态名是 "FlyIdle" —— 名字对不上，
        //   而 PlayStateAnim 找不到状态、又没配回退动画时就"什么都不播"，
        //   于是受击结束后 Animator 一直停在 Hurt 上：表现就是"Boss 受击动画反复播放/卡住"。
        public override string AnimName => "FlyIdle";
        public FlyingFlightState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void PhysicsUpdate()
        {
            if (enemy.player == null) return;
            enemy.UpdateVisuals();

            FlyingEnemy flying = (FlyingEnemy)enemy;

            Vector2 self = enemy.transform.position;
            Vector2 player = enemy.player.position;

            float dx = player.x - self.x;
            float horizontal = Mathf.Abs(dx);

            // ★ 保持远程距离：水平方向太近就【主动后退】，而不是继续贴上去。
            //   Boss 的定位是"高空远程 + 偶尔下坠"，贴脸近战是玩家的事。
            if (horizontal < flying.keepDistance)
            {
                float away = dx >= 0f ? -1f : 1f;
                enemy.rb.velocity = new Vector2(away * enemy.chaseSpeed * 0.8f, 0f);
                return;
            }

            // ★ 高度分层：目标点是【玩家上方 chaseHeight】，不是玩家本人。
            //   直接朝玩家飞 = 越飞越低、最后贴着地面打，冲撞也就没有"从高处砸下来"的压迫感。
            Vector2 target = player + Vector2.up * flying.chaseHeight;
            Vector2 toTarget = target - self;

            // 距离曲线：接近保持距离时减速，避免"冲过去又弹回来"的抖动
            float speed = horizontal < flying.keepDistance * 1.4f
                ? enemy.chaseSpeed * 0.7f
                : enemy.chaseSpeed;

            enemy.rb.velocity = toTarget.normalized * speed;
        }

        public override void LogicUpdate()
        {
            if (enemy.player == null)
            {
                stateMachine.ChangeState(((FlyingEnemy)enemy).flyingIdle);   // 丢玩家 → 回飞行待机（不是基类地面巡逻）
                return;
            }
            if (!enemy.CanSeePlayer())
            {
                stateMachine.ChangeState(((FlyingEnemy)enemy).flyingIdle);
                return;
            }

            // 冲刺无敌帧：玩家冲刺中不锁攻击（等他冲完——这是"冲刺可以赖皮穿过弹幕"的核心）
            PlayerController pc = enemy.player.GetComponent<PlayerController>();
            if (pc != null && pc.IsDashing) return;

            float dist = Vector2.Distance(enemy.transform.position, enemy.player.position);
            FlyingEnemy flying = (FlyingEnemy)enemy;

            // 攻击选集（按距离分层）：
            // ① 贴脸 → 冲撞（复用基类冲撞状态）
            if (dist < enemy.attackRange * 1.5f && enemy.CanAttack())
            {
                stateMachine.ChangeState(enemy.attackState);
                return;
            }
            // ② 中远 → 火球（独立冷却，避免吐球太频繁）
            if (dist < flying.fireballRange && flying.CanFireball())
            {
                stateMachine.ChangeState(flying.flyingFireball);
                return;
            }
            // ③ 距离远 → Boss 下坠技能（读取玩家位置 → 红圈预警 2s → 冲撞坠落）
            if (dist > flying.diveTriggerRange && enemy.CanAttack())
            {
                stateMachine.ChangeState(flying.flyingDive);
                return;
            }
        }
    }
}
