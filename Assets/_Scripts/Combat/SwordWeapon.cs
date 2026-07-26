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
        Vector3 origin = holder.AttackOrigin.position;

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

        // Trigger Hit Stop if we hit something (Combat Feel)
        if (hasHit)
        {
            HitStop.Stop(0.05f);
        }
    }
}
}
