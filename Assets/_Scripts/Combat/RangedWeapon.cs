using UnityEngine;

using DeadCells.Loading;
using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Player;

namespace DeadCells.Combat
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

        // 出膛点 = 当前武器的攻击原点。
        // ★ 用 holder.CurrentAttackOrigin 而不是持平的 holder.AttackOrigin：
        //   枪口做进外观预制体后（WeaponData.attackOriginChildName = "Muzzle"），
        //   子弹就会从枪口出来、跟着枪动，而不是从一个固定的玩家挂点出来
        Transform spawnPoint = holder.CurrentAttackOrigin != null
            ? holder.CurrentAttackOrigin
            : holder.transform;

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

        // 发射反馈：轻顿帧 + 发射音。
        // 注意这里【没有命中】，所以不走 Feedback.Hit（那会播命中音 + 命中特效）
        AudioManager.PlayAt(SfxId.AttackSwing, spawnPoint.position);
        Feedback.Shake(0.1f);
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
