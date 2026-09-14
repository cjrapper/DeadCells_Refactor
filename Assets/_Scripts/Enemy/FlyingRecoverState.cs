using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 飞行 Boss 的"低空硬直"状态 —— 下坠落地之后的收招。
    ///
    /// 这是 Boss 战节奏里最关键的一个状态：
    ///   · 下坠【打空】→ 长硬直（diveRecovery，默认 1.6s），Boss 趴在低空起不来
    ///     → 玩家这一段就是"偷伤害"的奖励窗口
    ///   · 下坠【打中】→ 短硬直（diveRecoveryOnHit，默认 0.8s），不惩罚成功的一方
    ///
    /// 硬直期间：不攻击、移动很慢、贴图发暗（视觉上"它现在动不了"），
    /// 硬直结束才升回高空。没有这个状态的话 Boss 一落地就秒回高空，
    /// 玩家永远抓不到输出窗口 —— 那就是"两个技能的 Boss 很容易被磨死"的根源。
    /// </summary>
    public class FlyingRecoverState : EnemyState
    {
        public override string AnimName => "FlyIdle";   // 复用悬浮动画（Boss 控制器里没有专属状态）

        private float timer;
        private float totalTime;
        private SpriteRenderer sr;
        private Color originalColor = Color.white;

        public FlyingRecoverState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            FlyingEnemy boss = (FlyingEnemy)enemy;

            totalTime = boss.LastDiveHit ? boss.diveRecoveryOnHit : boss.diveRecovery;
            totalTime = Mathf.Max(0.05f, totalTime);
            timer = totalTime;

            // 落地就停住（"砸在地上起不来"）
            if (enemy.rb != null) enemy.rb.velocity = Vector2.zero;

            // 视觉：发暗表示"硬直中，可以打"
            sr = enemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                originalColor = sr.color;
                sr.color = originalColor * 0.55f;
            }

            Feedback.Shake(boss.LastDiveHit ? 0.35f : 0.5f);
        }

        public override void PhysicsUpdate()
        {
            if (enemy.rb == null) return;

            // 硬直期间几乎不动，只保留一点点下沉感（像贴在地上）
            enemy.rb.velocity = new Vector2(0f, -0.6f);
        }

        public override void LogicUpdate()
        {
            timer -= Time.deltaTime;

            // 快要恢复时闪一下，给玩家"要起来了"的预告（避免被无预警反打）
            if (sr != null && timer < 0.5f)
            {
                float blink = Mathf.PingPong(Time.time * 12f, 1f);
                sr.color = Color.Lerp(originalColor * 0.55f, originalColor, blink);
            }

            if (timer <= 0f)
                stateMachine.ChangeState(((FlyingEnemy)enemy).flyingFlight);
        }

        public override void Exit()
        {
            if (sr != null) sr.color = originalColor;
        }
    }
}
