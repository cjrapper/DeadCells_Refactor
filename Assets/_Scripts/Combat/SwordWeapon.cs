using UnityEngine;

using DeadCells.Core;
using DeadCells.Player;

namespace DeadCells.Combat
{
    // Create Asset Menu for easy creation in Project window
    [CreateAssetMenu(fileName = "New Sword Weapon", menuName = "Combat/Sword Weapon")]
    public class SwordWeapon : WeaponData
{
    [Header("Attack Settings")]
    public LayerMask targetLayer;

    [System.NonSerialized] private Collider2D[] hitBuffer = new Collider2D[10];

    // Implementation of the abstract Attack method
    public override void Attack(PlayerController holder)
    {
        // 外观上挂了 WeaponHitbox → 伤害交给碰撞盒（所见即所得），这里不再做圆形判定，
        // 否则同一刀会被判定两次（圆形一次 + 碰撞盒一次）
        if (holder.PlayerCombat != null && holder.PlayerCombat.HasHitbox) return;

        Vector3 origin = holder.CurrentAttackOrigin != null
            ? holder.CurrentAttackOrigin.position
            : holder.transform.position;

        // Non-allocating overlap check
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, attackRange, hitBuffer, targetLayer);

        bool hasHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            IDamageable damageable = hitBuffer[i].GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Pass damage, source position (for knockback direction), and knockback force
                damageable.TakeDamage(damage, holder.transform.position, knockbackForce);
                hasHit = true;
            }
        }

        // 命中反馈：顿帧 + 震屏 + 命中音 + 命中特效。
        // 统一走 Feedback 入口 —— 以前是各武器自己 HitStop.Stop()，
        // 结果就是"有些武器有顿帧没震动没音效"，手感参差不齐
        if (hasHit)
        {
            Feedback.Hit(origin, hitStop: 0.05f, shake: 0.25f);
        }
    }
}
}
