using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 远程激光攻击 —— 中距离（激光触发范围内、近战范围外）的 AI 状态：
    /// 原地蓄力（显示感叹号）→ 朝玩家方向发射一段短激光（Raycast 命中即伤，
    /// LineRenderer 画出激光线）→ 短暂展示后回追击。
    /// </summary>
    public class LaserAttackState : EnemyState
    {
        public override string AnimName => "Laser";
        private float timer;
        private bool hasFired;
        private LineRenderer laserLine;

        public LaserAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            timer = enemy.windupTime * 0.8f;   // 蓄力（比近战略短）
            hasFired = false;
            enemy.RegisterAttack();

            // 蓄力提示（感叹号，和近战预警共用 alertSign）
            if (enemy.alertSign != null) enemy.alertSign.SetActive(true);

            EnsureLine();
        }

        private void EnsureLine()
        {
            if (laserLine != null) return;
            laserLine = enemy.GetComponentInChildren<LineRenderer>();
            if (laserLine != null)
            {
                laserLine.enabled = false;
                // 默认材质兜底（Sprites/Default 保证可见，不紫屏）
                if (laserLine.sharedMaterial == null)
                    laserLine.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }
        }

        public override void LogicUpdate()
        {
            timer -= Time.deltaTime;
            enemy.UpdateVisuals();

            if (!hasFired)
            {
                if (timer <= 0)
                {
                    Fire();
                    hasFired = true;
                    timer = 0.3f;          // 激光线展示时长
                }
            }
            else if (timer <= 0)
            {
                if (laserLine != null) laserLine.enabled = false;
                stateMachine.ChangeState(enemy.GetCombatReturnState());
            }
        }

        private void Fire()
        {
            if (enemy.player == null) return;

            // 朝玩家方向发射短激光（层级过滤只打玩家层）
            Vector2 dir = ((Vector2)(enemy.player.position - enemy.transform.position)).normalized;
            RaycastHit2D hit = Physics2D.Raycast(
                enemy.transform.position, dir, enemy.laserRange, enemy.playerLayer);

            // 视觉：激光线从敌人到命中点（或最大距离）
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.positionCount = 2;
                laserLine.SetPosition(0, enemy.transform.position);
                laserLine.SetPosition(1, hit
                    ? (Vector3)hit.point
                    : enemy.transform.position + (Vector3)(dir * enemy.laserRange));
            }

            // 伤害
            if (hit)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(enemy.laserDamage, enemy.transform.position, 6f);
            }
        }

        public override void Exit()
        {
            if (enemy.alertSign != null) enemy.alertSign.SetActive(false);
            if (laserLine != null) laserLine.enabled = false;
        }
    }
}
