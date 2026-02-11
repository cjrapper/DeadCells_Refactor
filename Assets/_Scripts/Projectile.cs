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

        // Check if hit Ground
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            DestroyProjectile();
            return;
        }

        // Check if hit Enemy (or Target)
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
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