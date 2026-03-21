using UnityEngine;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        
        // Ensure physics settings are correct for a projectile
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Destroy self after lifeTime seconds to prevent lag
        Destroy(gameObject, lifeTime);
        
        // Give initial velocity
        rb.velocity = transform.right * speed;
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
        Destroy(gameObject);
    }
    
    // Setup method to pass dynamic stats from WeaponData
    public void Setup(int damage, float knockbackForce, LayerMask targetLayer)
    {
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.targetLayer = targetLayer;
    }
}