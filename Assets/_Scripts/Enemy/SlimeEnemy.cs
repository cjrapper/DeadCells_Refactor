using UnityEngine;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 史莱姆 —— 跳着走（不是滑行），攻击前明显压扁蓄力。
    ///
    /// 移动方式通过覆写 Enemy.MoveHorizontal 实现：
    ///   状态机只管"往哪走、走多快"，具体怎么动由这个类决定 ——
    ///   所以改成跳着走不用动 PatrolState / ChaseState 一行代码，以后加新怪也一样。
    /// </summary>
    public class SlimeEnemy : Enemy
    {
        [Header("Slime Bounce Settings（挤压动画幅度）")]
        public float jumpForce = 5f;
        public float jumpAmount = 0.1f;

        [Header("Hop（跳着走）")]
        [Tooltip("两次跳跃之间的间隔（秒）。越小越活跃")]
        public float hopInterval = 0.55f;
        [Tooltip("起跳的竖直速度（越高跳得越高）")]
        public float hopHeight = 6.5f;
        [Tooltip("起跳时的水平速度倍率（乘状态机给的速度）")]
        public float hopForwardScale = 1.1f;
        [Tooltip("判定\"站在地上\"的竖直速度阈值")]
        public float groundedVelocity = 0.2f;

        [Header("Telegraph（蓄力）")]
        [Tooltip("蓄力时压到多扁（1 = 不压）")]
        public float telegraphSquashY = 0.55f;
        [Tooltip("蓄力时横向撑到多宽（1 = 不撑）")]
        public float telegraphSquashX = 1.35f;

        private float hopTimer;
        private float hopDir = 1f;
        private float telegraphProgress;   // 0 = 正常，1 = 完全蓄力

        // ==================== 移动：跳着走 ====================

        public override void MoveHorizontal(float dir, float speed)
        {
            if (rb == null) return;

            bool grounded = Mathf.Abs(rb.velocity.y) <= groundedVelocity;

            // 空中：保持当前速度，让重力自然作用。
            // （每帧重设速度会跟重力打架 → 跳不起来，只能在原地抖）
            if (!grounded) return;

            hopDir = Mathf.Abs(dir) > 0.01f ? Mathf.Sign(dir) : hopDir;

            // 落地后先歇一会儿再跳 —— 这个"歇"就是蹦跳节奏感的来源
            hopTimer -= Time.fixedDeltaTime;
            if (hopTimer > 0f)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }

            hopTimer = hopInterval;
            rb.velocity = new Vector2(hopDir * speed * hopForwardScale, hopHeight);
        }

        public override void StopHorizontal()
        {
            base.StopHorizontal();
            // 停下之后要再动就立刻起跳，否则会有一段"莫名其妙不动"的延迟
            hopTimer = 0f;
        }

        /// <summary>
        /// 必须【落地】才能开始蓄力。
        /// 不限制的话它会在跳跃的半空中开始蓄力、边飘边扑 ——
        /// 玩家看到的是"它没蹲就直接飞过来了"，蓄力完全失去预告作用。
        /// </summary>
        public override bool CanStartAttack()
        {
            return rb != null && Mathf.Abs(rb.velocity.y) <= groundedVelocity;
        }

        // ==================== 蓄力：压扁 ====================

        public override void OnTelegraphTick(float progress)
        {
            telegraphProgress = Mathf.Clamp01(progress);
        }

        public override void OnTelegraphEnd()
        {
            telegraphProgress = 0f;
        }

        // ==================== 视觉 ====================

        public override void UpdateVisuals()
        {
            // 蓄力优先：压扁下蹲是"要扑过来了"最直观的预告，
            // 比单靠一个感叹号清楚得多
            if (telegraphProgress > 0f)
            {
                ApplyScale(telegraphSquashX, telegraphSquashY);
                return;
            }

            HandleBounce();
        }

        private void HandleBounce()
        {
            if (rb == null) return;

            // 只有在有速度时才进行缩放动画
            if (rb.velocity.magnitude > 0.1f)
            {
                float single = Mathf.Sin(Time.time * jumpForce);
                ApplyScale(1f - single * jumpAmount, 1f + single * jumpAmount);
            }
            else
            {
                // 停止运动时缓慢恢复正常比例
                float currentXDir = transform.localScale.x > 0 ? 1 : -1;
                Vector3 target = new Vector3(currentXDir, 1f, 1f);
                transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * 5f);
            }
        }

        /// <summary>设置缩放并保持当前朝向（x 取负号会翻转贴图，不能丢）</summary>
        private void ApplyScale(float x, float y)
        {
            float dir = transform.localScale.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(Mathf.Abs(x) * dir, y, 1f);
        }

        protected override void Die()
        {
            base.Die();
        }
    }
}
