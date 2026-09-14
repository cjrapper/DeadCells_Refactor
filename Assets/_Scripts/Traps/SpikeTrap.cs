using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Traps
{
    /// <summary>
    /// 地刺 —— 静止陷阱：玩家接触（站上去）即受伤 + 击退。
    /// 碰撞体用【实心】(非 Trigger)：玩家踩上去"站"在刺上，持续接触持续掉血（有伤害间隔）。
    /// 伤害走 IDamageable，与敌人攻击同一套逻辑解耦。
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        [Header("伤害")]
        public int damage = 10;
        public float knockbackForce = 8f;
        public float hitCooldown = 0.5f;   // 伤害间隔（配合 PlayerHealth 的受击硬直，防连跳秒杀）

        private float lastHitTime;

        private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);
        private void OnCollisionStay2D(Collision2D collision) => TryHit(collision.collider);

        private void TryHit(Collider2D col)
        {
            if (Time.time < lastHitTime + hitCooldown) return;
            if (!col.CompareTag("Player")) return;

            // 用 IDamageable 接口解耦：伤害来源位置 = 刺的位置（击退方向 = 玩家->刺 的反方向）
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable == null) return;

            lastHitTime = Time.time;
            damageable.TakeDamage(damage, transform.position, knockbackForce);
        }
    }
}
