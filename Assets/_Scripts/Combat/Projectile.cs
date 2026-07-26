using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 20f;
        public float lifeTime = 5f;
        public int damage = 10;
        public float knockbackForce = 5f;
        public GameObject hitEffectPrefab;

        [Header("Collision")]
        public LayerMask targetLayer;
        public LayerMask groundLayer;

        private Rigidbody2D rb;
        private bool hasHit;
        private SamplePool pool;
        private bool hasPool;
        private Coroutine lifetimeCoroutine;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
            rb.velocity = transform.right * speed;
        }

        private System.Collections.IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSecondsRealtime(lifeTime);
            ReturnToPool();
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasHit) return;

            // 检查是否撞墙/地面 (Bitwise Operation)
            // (1 << layer) 将图层索引转换为二进制掩码，与 groundLayer 进行按位与运算
            if (((1 << collision.gameObject.layer) & groundLayer) != 0)
            {
                DestroyProjectile();
                return;
            }

            // 检查是否命中目标 (Enemy)
            if (((1 << collision.gameObject.layer) & targetLayer) != 0)
            {
                IDamageable damageable = collision.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // 造成伤害并施加击退
                    damageable.TakeDamage(damage, transform.position, knockbackForce);
                    hasHit = true;
                    DestroyProjectile();
                }
            }
        }

        void DestroyProjectile()
        {
            if (hitEffectPrefab != null)
            {
                // 生成命中特效 (Spawn Hit VFX)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            ReturnToPool();
        }

        void ReturnToPool()
        {
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }
            if (hasPool)
            {
                hasHit = false;
                rb.velocity = Vector2.zero;
                pool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AssignPool(SamplePool projectilePool)
        {
            pool = projectilePool;
            hasPool = pool != null;
        }

        public void OnGetFromPool()
        {
            hasHit = false;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.velocity = transform.right * speed;
            if (lifetimeCoroutine != null) StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
        }

        // Setup method to pass dynamic stats from WeaponData
        public void Setup(int damage, float knockbackForce, LayerMask targetLayer)
        {
            this.damage = damage;
            this.knockbackForce = knockbackForce;
            this.targetLayer = targetLayer;
        }
    }
}
