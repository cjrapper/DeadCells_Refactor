using UnityEngine;

// Create Asset Menu for easy creation in Project window
[CreateAssetMenu(fileName = "New Sword Weapon", menuName = "Combat/Sword Weapon")]
public class SwordWeapon : WeaponData
{
    [Header("Attack Settings")]
    public LayerMask targetLayer;

    // Implementation of the abstract Attack method
    public override void Attack(PlayerController holder)
    {
        Vector3 origin = holder.attackPoint.position;
        
        // Detect enemies in range
        Collider2D[] hitRange = Physics2D.OverlapCircleAll(origin, attackRange, targetLayer);
        
        bool hasHit = false;

        // Apply damage to all valid targets
        foreach (var enemy in hitRange)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if(damageable != null)
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

        Debug.Log($"Used {weaponName}, hit {hitRange.Length} targets");
    }
}
