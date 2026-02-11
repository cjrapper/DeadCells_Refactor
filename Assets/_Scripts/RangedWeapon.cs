using UnityEngine;

[CreateAssetMenu(fileName = "New Ranged Weapon", menuName = "Combat/Ranged Weapon")]
public class RangedWeapon : WeaponData
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public LayerMask targetLayer;

    public override void Attack(PlayerController holder)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is missing on Ranged Weapon!");
            return;
        }

        // Determine spawn position (slightly in front of player)
        // Use attackPoint if available, otherwise player center
        Transform spawnPoint = holder.attackPoint != null ? holder.attackPoint : holder.transform;

        // Calculate rotation based on player facing direction
        // If player scale.x is 1, rotation is 0. If -1, rotation is 180.
        float facingDir = holder.transform.localScale.x;
        Quaternion rotation = facingDir > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        // Spawn Projectile
        GameObject projObj = Instantiate(projectilePrefab, spawnPoint.position, rotation);
        
        // Setup Projectile Stats
        Projectile projScript = projObj.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.speed = projectileSpeed;
            // Pass stats from WeaponData to Projectile
            projScript.Setup(damage, knockbackForce, targetLayer);
        }
    }
}