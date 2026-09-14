using UnityEngine;

namespace DeadCells.Enemy
{
    /// <summary>飞行待机：悬停（velocity 正弦上下）→ 玩家进入范围切 flight</summary>
    public class FlyingIdleState : EnemyState
    {
        public override string AnimName => "FlyIdle";   // 与巡逻共用
        public FlyingIdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            if (enemy.rb != null)
                enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
        }

        public override void PhysicsUpdate()
        {
            if (enemy.rb == null) return;
            enemy.UpdateVisuals();

            FlyingEnemy flying = (FlyingEnemy)enemy;

            // ★ 离出生点太远 → 先飞回去。
            //   否则追玩家追到天边、丢了目标之后就【滞留在那里】悬停 ——
            //   看起来就是"没回到巡逻状态"（其实它在 flyingIdle，只是位置不对）。
            float homeDist = Vector2.Distance(enemy.transform.position, enemy.startPos);
            if (homeDist > flying.returnHomeDistance)
            {
                Vector2 toHome = ((Vector2)enemy.startPos - (Vector2)enemy.transform.position).normalized;
                enemy.rb.velocity = toHome * enemy.chaseSpeed;
                return;
            }

            // 原位悬浮：velocity 正弦上下（不直接改 transform.position，避免与飞行追踪打架）
            float vy = Mathf.Sin(Time.time * flying.floatSpeed) * flying.floatAmplitude;
            enemy.rb.velocity = new Vector2(0f, vy);
        }

        public override void LogicUpdate()
        {
            if (enemy.player == null) return;
            if (!enemy.CanSeePlayer()) return;   // 视线遮挡也不追（伪视觉）

            FlyingEnemy flying = (FlyingEnemy)enemy;
            float dist = Vector2.Distance(enemy.transform.position, enemy.player.position);
            if (dist < flying.idleRange)
                stateMachine.ChangeState(flying.flyingFlight);
        }
    }
}
