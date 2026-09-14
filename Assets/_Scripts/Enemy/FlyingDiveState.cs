using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// Boss 下坠技能 —— 三段式：
    ///   ① 前摇（diveWindup，默认 2 秒）：读取玩家当前位置作为落点，地面生成预警红圈（闪烁）
    ///   ② 下坠：朝落点高速冲撞（gravityScale=0，用 velocity 直接驱动）
    ///   ③ 落地：以 Boss 身体为中心做范围冲击波判定 + 伤害击退 → 回飞行轨道
    ///
    /// 职责划分（这是"预警圈看起来像在打人"的根因）：
    ///   预警圈 = 纯视觉 telegraph：不带 Collider、不参与任何伤害计算
    ///   伤害   = 只从 Boss 身体发出：下坠途中身体撞到人 + 落地时身体周围的冲击波
    /// </summary>
    public class FlyingDiveState : EnemyState
    {
        public override string AnimName => "Dive";

        private float timer;
        private float diveTime;                // 已下坠时长（用于"被地形挡住"的判定去抖）
        private Vector2 targetPos;
        private bool isDiving;
        private bool hasHitPlayer;             // 一次下坠只结算一次伤害
        private GameObject warningCircle;
        private SpriteRenderer warningSr;      // 预警圈渲染器（做闪烁）
        private Vector3 warningScale = Vector3.one;   // 预警圈基准缩放（含预制体自带缩放）

        public FlyingDiveState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            FlyingEnemy boss = (FlyingEnemy)enemy;

            // ① 读取玩家当前位置（地面落点）
            targetPos = enemy.player != null
                ? (Vector2)enemy.player.position
                : (Vector2)enemy.transform.position;

            timer = boss.diveWindup;      // 2 秒前摇
            diveTime = 0f;
            isDiving = false;
            hasHitPlayer = false;
            enemy.RegisterAttack();

            // 前摇：Boss 原地悬停
            if (enemy.rb != null) enemy.rb.velocity = Vector2.zero;
            if (enemy.alertSign != null) enemy.alertSign.SetActive(true);

            SpawnWarningCircle(boss);
        }

        /// <summary>
        /// 生成预警圈（纯视觉）。
        ///
        /// ★ 两个必踩的坑，都是"预制体 Inspector 里的配置被代码覆盖"：
        ///   1) Instantiate 传 Quaternion.identity → 预制体自带的旋转被清掉。
        ///      Circle.prefab 根节点是 X 轴 59.355°（贴地椭圆），进游戏就变成正对摄像机的正圆。
        ///   2) localScale 直接赋值 Vector3.one * 系数 → 预制体缩放比例被清掉
        ///      （如果用非等比缩放压扁，同样会失效）。
        /// </summary>
        private void SpawnWarningCircle(FlyingEnemy boss)
        {
            if (boss.warningCirclePrefab == null) return;

            Transform prefabTf = boss.warningCirclePrefab.transform;
            warningCircle = Object.Instantiate(
                boss.warningCirclePrefab, targetPos, prefabTf.rotation);   // 保留预制体旋转，不要 identity
            warningCircle.name = "DiveWarning";

            warningSr = warningCircle.GetComponentInChildren<SpriteRenderer>();

            // 尺寸标定：让圈的宽度正好等于命中直径（素材多大都能对）。
            // 只统一乘一个系数 k、保留预制体缩放的 x:y 比例 ——
            // 直接 Vector3.one * k 会连带清掉"用缩放压扁"的做法
            Vector3 baseScale = prefabTf.localScale;
            warningScale = baseScale;
            if (warningSr != null && warningSr.sprite != null && warningSr.sprite.bounds.size.x > 0.01f)
            {
                float currentWidth = warningSr.sprite.bounds.size.x * Mathf.Abs(baseScale.x);
                if (currentWidth > 0.0001f)
                {
                    float k = (boss.diveHitRadius * 2f) / currentWidth;
                    warningScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
                }
            }

            warningCircle.transform.localScale = warningScale;
        }

        public override void LogicUpdate()
        {
            FlyingEnemy boss = (FlyingEnemy)enemy;
            timer -= Time.deltaTime;

            // ===== ① 前摇阶段 =====
            if (!isDiving)
            {
                // 红圈提示：透明度脉动 + 轻微缩放（比"缩放脉动"更像预警圈，不会让人误判范围）
                if (warningCircle != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.04f;
                    warningCircle.transform.localScale = warningScale * pulse;

                    if (warningSr != null)
                    {
                        Color c = warningSr.color;
                        c.a = 0.45f + (Mathf.Sin(Time.time * 10f) + 1f) * 0.25f;   // 0.45~0.95
                        warningSr.color = c;
                    }
                }

                if (timer <= 0f) StartDive(boss);
                return;
            }

            // ===== ② 下坠阶段 =====
            diveTime += Time.deltaTime;

            // 身体撞到玩家：立刻结算（"撞到才痛"，而不是站在圈里就掉血）
            if (!hasHitPlayer)
                TryHitPlayer(boss, boss.diveHitRadius * 0.85f);   // 贴身半径 < 落地半径

            // ===== ③ 落地 =====
            if (HasLanded(boss))
            {
                if (!hasHitPlayer)
                    TryHitPlayer(boss, boss.diveHitRadius);      // 落地冲击波：范围判定

                boss.LastDiveHit = hasHitPlayer;   // 决定接下来是长硬直还是短硬直

                // 打没打中会直接决定硬直时长，日志里说清楚，省得靠猜
                float playerDist = enemy.player != null
                    ? Vector2.Distance(enemy.transform.position, enemy.player.position)
                    : -1f;
                Debug.Log(hasHitPlayer
                    ? $"[Dive] 落地命中（伤害 {boss.diveDamage}，眩晕 {boss.diveStunDuration}s）"
                    : $"[Dive] 落地未命中：Boss({enemy.transform.position.x:F1},{enemy.transform.position.y:F1}) " +
                      $"玩家({(enemy.player != null ? enemy.player.position.x : 0f):F1}," +
                      $"{(enemy.player != null ? enemy.player.position.y : 0f):F1}) 距离 {playerDist:F2}，" +
                      $"命中半径 {boss.diveHitRadius}");

                CleanupWarning();
                // 不再直接回飞行轨道：先进"低空硬直"状态 ——
                // 打空了就是玩家的偷伤害窗口，这是 Boss 战节奏的核心
                stateMachine.ChangeState(boss.flyingRecover);
            }
        }

        private void StartDive(FlyingEnemy boss)
        {
            isDiving = true;
            diveTime = 0f;

            if (enemy.rb != null)
            {
                Vector2 self = enemy.transform.position;
                Vector2 dir = targetPos - self;
                enemy.rb.velocity = (dir.sqrMagnitude < 0.0001f ? Vector2.down : dir.normalized) * boss.diveSpeed;
            }

            // 预警圈保持可见到下坠结束 —— 圈是给玩家看"落点在哪"的，
            // 下坠途中正是最需要这条信息的时刻，提前隐藏等于把提示撤了
            if (enemy.alertSign != null) enemy.alertSign.SetActive(false);
        }

        /// <summary>
        /// 落地判定：到达落点 / 被地形挡住 / 超时兜底，三者任一即算落地。
        /// 只用"距离 &lt; 0.6"会漏判：下坠撞到平台被物理系统挡住时速度归零，
        /// 但距离可能永远到不了落点，旧代码要靠 timer&lt;-2 兜底（前摇 2s + 2s），Boss 会僵在原地很久。
        /// </summary>
        private bool HasLanded(FlyingEnemy boss)
        {
            if (Vector2.Distance(enemy.transform.position, targetPos) < 0.6f) return true;

            // 速度被物理系统吃掉 → 撞到东西了（下坠中速度应恒为 diveSpeed；
            // 加 0.1s 去抖，避免刚起步那一帧速度还没生效就误判）
            if (diveTime > 0.1f && enemy.rb != null && enemy.rb.velocity.sqrMagnitude < 1f) return true;

            return timer < -1f;   // 兜底：下坠已超过 1 秒还没到
        }

        private static readonly Collider2D[] hitBuffer = new Collider2D[8];

        /// <summary>
        /// 命中结算：圆心永远是 **Boss 的身体**（enemy.transform.position），
        /// 不是地面预警圈的位置 —— 这样伤害来源就是 Boss 本身，而不是"圈在打人"。
        ///
        /// ★ 必须用 OverlapCircleNonAlloc 扫【全部】重叠碰撞体 + GetComponentInParent：
        ///   单数的 OverlapCircle 只返回"其中一个"，返回哪个不确定；
        ///   而玩家身上不止一个碰撞体，IDamageable（PlayerHealth）挂在根节点上，
        ///   子碰撞体用 GetComponent 找不到 → 判定直接失效。
        ///   （这个坑在 AttackState 里已经踩过一次，这里是同一个 —— 冲撞"撞到了却没伤害"的根因）
        /// </summary>
        private bool TryHitPlayer(FlyingEnemy boss, float radius)
        {
            if (enemy.playerLayer.value == 0) return false;

            int count = Physics2D.OverlapCircleNonAlloc(
                enemy.transform.position, radius, hitBuffer, enemy.playerLayer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = hitBuffer[i];
                if (col == null) continue;

                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                damageable.TakeDamage(boss.diveDamage, enemy.transform.position, 12f);
                hasHitPlayer = true;

                // 眩晕惩罚：被 Boss 砸中不只是掉血，还会短暂失控
                // （这是"冲撞"和"普通挨打"的区别，也是玩家必须躲的理由）
                if (boss.diveStunDuration > 0f)
                {
                    DeadCells.Player.PlayerHealth ph = col.GetComponentInParent<DeadCells.Player.PlayerHealth>();
                    if (ph != null) ph.Stun(boss.diveStunDuration);
                }

                return true;
            }

            return false;
        }

        public override void Exit()
        {
            CleanupWarning();
            if (enemy.alertSign != null) enemy.alertSign.SetActive(false);
        }

        private void CleanupWarning()
        {
            if (warningCircle != null)
            {
                Object.Destroy(warningCircle);
                warningCircle = null;
            }
        }
    }
}
