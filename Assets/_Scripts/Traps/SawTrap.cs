using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Traps
{
    /// <summary>
    /// 移动齿轮 —— 两点间往返 + 自转的陷阱：
    /// 移动用 transform 直接插值（正弦缓动，起步减速/到点减速，观感像"滚"），
    /// 自转做视觉旋转；检测用【Trigger】：碰到玩家受伤，但不物理推挤玩家。
    /// </summary>
    public class SawTrap : MonoBehaviour
    {
        [Header("移动（相对物体起始位置的偏移）")]
        public Vector2 startLocalPos = Vector2.zero;
        public Vector2 endLocalPos = new Vector2(3f, 0f);
        [Tooltip("单程时间（秒），越大越慢")]
        public float moveDuration = 2f;

        [Header("伤害")]
        public int damage = 15;
        public float knockbackForce = 10f;
        public float hitCooldown = 0.6f;

        [Header("视觉")]
        [Tooltip("是否自转（半圆/扇形贴图建议关掉，直线移动更像机械陷阱）")]
        public bool spinEnabled = false;
        [Tooltip("自转速度（度/秒），负号反向")]
        public float spinSpeed = -300f;

        private float lastHitTime;
        private Vector3 basePos;

        private void Awake()
        {
            basePos = transform.position;
        }

        private void Update()
        {
            // 往返：sin 在 -1~1 间变化，映射到 0~1 做插值；一个完整周期 = 去 + 回
            float t = (Mathf.Sin(Time.time * (2f * Mathf.PI) / (moveDuration * 2f)) + 1f) * 0.5f;

            Vector2 target = Vector2.Lerp(startLocalPos, endLocalPos, t);
            transform.position = basePos + (Vector3)target;

            // 自转可选（默认关；Time.deltaTime 在 HitStop 时归零 → 顿帧时也停）
            if (spinEnabled)
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < lastHitTime + hitCooldown) return;
            if (!other.CompareTag("Player")) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null) return;

            lastHitTime = Time.time;
            damageable.TakeDamage(damage, transform.position, knockbackForce);
        }
    }
}
