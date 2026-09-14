using UnityEngine;

using DeadCells.Combat;
using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 火球攻击 —— 蓄力（感叹号提示）→ 朝玩家方向发射弹丸。
    /// 复用玩家的 Projectile 组件（Trigger 命中检测 + IDamageable + 对象池），
    /// 敌人侧只换 targetLayer = 玩家层、伤害值。同一套弹丸系统玩家敌人共用。
    /// </summary>
    public class FlyingFireballState : EnemyState
    {
        public override string AnimName => "Fireball";
        private float timer;

        public FlyingFireballState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            timer = enemy.windupTime;   // 蓄力时间（复用基类字段）
            if (enemy.alertSign != null) enemy.alertSign.SetActive(true);

            // 蓄力时悬停
            if (enemy.rb != null) enemy.rb.velocity = new Vector2(0, 0);

            // 火球独立冷却（避免吐球太频繁）
            ((FlyingEnemy)enemy).RegisterFireball();
        }

        public override void LogicUpdate()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                FireFireball();
                // 回归状态走虚方法：飞行怪回飞行轨道（不能回基类 chaseState——那是地面追击）
                stateMachine.ChangeState(enemy.GetCombatReturnState());
            }
        }

        private void FireFireball()
        {
            if (enemy.player == null) return;

            // 朝玩家方向（含 Y 的斜向弹道）
            Vector2 dir = ((Vector2)(enemy.player.position - enemy.transform.position)).normalized;

            // 从玩家侧弹丸池拿（Projectile 组件通用：Trigger 命中 + IDamageable）
            SamplePool pool = PoolManager.Instance?.GetPool(PoolType.Projectile);
            GameObject proj = pool != null ? pool.Get() : null;

            if (proj != null)
            {
                // 旋转 = 朝玩家方向（弹丸沿 transform.right 飞）+ 贴图朝向修正偏移
                proj.transform.position = enemy.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg
                              + ((FlyingEnemy)enemy).fireballAngleOffset;
                proj.transform.rotation = Quaternion.Euler(0, 0, angle);

                Projectile p = proj.GetComponent<Projectile>();
                if (p != null)
                {
                    // Setup(伤害, 击退, 目标层=玩家层) —— 与玩家发射互不干扰
                    p.Setup(enemy.damage, 5f, enemy.playerLayer);
                    p.AssignPool(pool);
                    p.OnGetFromPool();
                }
            }
            else if (((FlyingEnemy)enemy).fireballPrefab != null)
            {
                // 兜底：池不可用直接实例化。
                // 注意：EnemyState 不是 MonoBehaviour，没有实例方法 Instantiate，
                // 用 UnityEngine.Object 的静态版本
                GameObject go = UnityEngine.Object.Instantiate(
                    ((FlyingEnemy)enemy).fireballPrefab,
                    enemy.transform.position, Quaternion.identity);
                go.transform.rotation = Quaternion.Euler(0, 0,
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                Projectile p = go.GetComponent<Projectile>();
                if (p != null) p.Setup(enemy.damage, 5f, enemy.playerLayer);
            }
        }

        public override void Exit()
        {
            if (enemy.alertSign != null) enemy.alertSign.SetActive(false);
        }
    }
}
