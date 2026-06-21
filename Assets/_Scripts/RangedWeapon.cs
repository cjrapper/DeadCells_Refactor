using UnityEngine;

using AngryBirds.Loading;
using AngryBirds.Core;
using AngryBirds.Player;

namespace AngryBirds.Combat
{
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
            projectilePrefab = ProjectileCache.FireballPrefab;

        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is missing on Ranged Weapon!");
            return;
        }

        // Determine spawn position (slightly in front of player)
        // Use AttackOrigin if available, otherwise player center
        Transform spawnPoint = holder.AttackOrigin != null ? holder.AttackOrigin : holder.transform;

        // Calculate rotation based on player facing direction
        // If player scale.x is 1, rotation is 0. If -1, rotation is 180.
        float facingDir = holder.transform.localScale.x;
        Quaternion rotation = facingDir > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        // Try to get projectile from pool, fallback to Instantiate
        SamplePool projectilePool = PoolManager.Instance?.GetPool(PoolType.Projectile);

        GameObject projObj;
        Projectile projScript = null;

        if (projectilePool != null)
        {
            projObj = projectilePool.Get();
            if (projObj != null)
            {
                projObj.transform.position = spawnPoint.position;
                projObj.transform.rotation = rotation;
                projScript = projObj.GetComponent<Projectile>();
                if (projScript != null)
                {
                    projScript.speed = projectileSpeed;
                    projScript.OnGetFromPool();
                }
            }
            else
            {
                projObj = Instantiate(projectilePrefab, spawnPoint.position, rotation);
            }
        }
        else
        {
            projObj = Instantiate(projectilePrefab, spawnPoint.position, rotation);
        }

        // Trigger Hit Stop for combat feel
        HitStop.Stop(0.05f);

        // Setup Projectile Stats
        if (projScript == null) projScript = projObj.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Setup(damage, knockbackForce, targetLayer);
            if (projectilePool != null) projScript.AssignPool(projectilePool);
        }
    }
}}
