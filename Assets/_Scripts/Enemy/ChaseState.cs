using UnityEngine;

namespace DeadCells.Enemy
{
    public class ChaseState : EnemyState
{
    // 追击与飞行共用一个动画（简化映射）
    public override string AnimName => "Fly";
    public ChaseState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine){}
    public override void Enter()
    {

    }
    public override void PhysicsUpdate()
    {
        if(enemy.player == null) return;
        enemy.UpdateVisuals();

        Vector2 self = enemy.transform.position;
        float dx = enemy.player.position.x - self.x;
        float horizontal = Mathf.Abs(dx);

        // 三段式追击：
        //   ① 玩家贴得太近 → 主动后退，保持出手距离
        //   ② 进入停下距离 → 刹停等攻击（留助跑距离，冲撞才有力量感）
        //   ③ 否则靠近
        // ★ 必须【先判"太近"】：顺序反了的话，贴着玩家时会一直命中"已到位"分支，
        //   永远不后退，也就永远保持不了出手距离
        if (enemy.HasMinDistance && horizontal < enemy.minDistance)
        {
            float away = dx >= 0f ? -1f : 1f;
            enemy.MoveHorizontal(away, enemy.chaseSpeed * 0.7f);
            return;
        }

        if (horizontal <= enemy.StopDistance)
        {
            enemy.StopHorizontal();
            return;
        }

        enemy.MoveHorizontal(Mathf.Sign(dx), enemy.chaseSpeed);
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

        // 触发攻击的距离 = 停下距离（冲撞的射程由 attackRange + 冲刺位移覆盖）
        float stopSqr = enemy.StopDistance * enemy.StopDistance;

        // 冲刺无敌帧：玩家冲刺中不触发攻击（敌人停下来等——"冲刺可以穿过敌人不进战斗"）
        var pc = enemy.player.GetComponent<DeadCells.Player.PlayerController>();
        if (pc != null && pc.IsDashing) return;

        if (distSqr < stopSqr && enemy.CanAttack() && enemy.CanStartAttack())
        {
            stateMachine.ChangeState(enemy.telegraphState);
            return;
        }

        // 敌人激光攻击本轮移除（LaserAttackState 文件保留，新怪物设计再启用）
        // float laserTriggerSqr = enemy.laserTriggerRange * enemy.laserTriggerRange;
        // if (distSqr < laserTriggerSqr && enemy.CanAttack())
        // {
        //     stateMachine.ChangeState(enemy.laserAttackState);
        //     return;
        // }
    }
    public override void Exit()
    {

    }
}
}
