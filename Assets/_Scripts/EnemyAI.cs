using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("Combat")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public int damage = 10;
    public float knockbackForce = 10f;
    private float nextAttackTime;

    [Header("Stats")]
    [SerializeField] private int health = 100;
    private int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float escapeRange = 8f;
    private Vector3 startPos;
    private bool faceRight = true;

    [Header("References")]
    public Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // State Machine 
    public enum State { Patrol, Chase, Hurt ,Attack}
    public State currentState;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = health;
        startPos = transform.position;
    }

    void Update()
    {
        // State transitions logic only
        if (currentState == State.Hurt) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (currentState == State.Patrol)
        {
            if (dist < chaseRange) currentState = State.Chase;
        }
        else if (currentState == State.Chase)
        {
            if(dist < attackRange) currentState = State.Attack;
            else if (dist > escapeRange) currentState = State.Patrol;
        }
        else if(currentState == State.Attack)
        {
            if(dist > attackRange) currentState = State.Chase;
        }
    }

    void FixedUpdate()
    {
        // Physics movement based on state
        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic();
                break;
            case State.Chase:
                ChaseLogic();
                break;
            case State.Hurt:
                // Do nothing, let physics (knockback) take control
                break;
            case State.Attack:
                AttackLogic();
                break;
        }
    }

    // --- Logic Implementation ---

    void PatrolLogic()
    {
        // Simple Sine wave patrol (Left-Right oscillation)
        float patrolOffset = Mathf.Sin(Time.time) * 2f; 
        float targetX = startPos.x + patrolOffset;
        
        Vector2 dir = (targetX > transform.position.x) ? Vector2.right : Vector2.left;
        rb.velocity = new Vector2(dir.x * (moveSpeed * 0.5f), rb.velocity.y);

        CheckFlip(dir.x);
    }

    void ChaseLogic()
    {
        if (player == null) return;

        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
        
        CheckFlip(dir.x);
    }

    void CheckFlip(float xDir)
    {
        if (xDir > 0.1f && !faceRight) Flip();
        else if (xDir < -0.1f && faceRight) Flip();
    }

    void Flip()
    {
        faceRight = !faceRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void AttackLogic()
    {
        rb.velocity = Vector2.zero;
        if(Time.time > nextAttackTime)
        {
            IDamageable target = player.GetComponent<IDamageable>();
            if(target != null)
            {
                target.TakeDamage(damage, transform.position, knockbackForce);
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    // --- IDamageable Interface ---

    public void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentState = State.Hurt; // Lock movement state
        
        // 1. Knockback
        if (rb != null)
        {
            Vector2 direction = (transform.position - sourcePosition).normalized;
            Vector2 force = direction * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
            
            rb.velocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        // 2. Visual Feedback
        StartCoroutine(FlashEffect());

        // 3. Recovery
        StartCoroutine(RecoverFromHit(0.5f));

        if (currentHealth <= 0) Die();
    }

    IEnumerator RecoverFromHit(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (currentHealth > 0)
        {
            currentState = State.Chase; // Resume chase after stun
        }
    }

    IEnumerator FlashEffect()
    {
        if (sr == null) yield break;
        Color original = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = original;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}