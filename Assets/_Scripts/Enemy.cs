using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System;

public abstract class Enemy : MonoBehaviour, IDamageable
{
    public Rigidbody2D rb { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public Collider2D bodyCollider { get; private set; }
    public Transform player;

    [Header("Base Settings")]
    public float moveSpeed = 5f;
    public float chaseSpeed = 7f;
    public float chaseRange = 5f;
    public Vector3 startPos;

    [Header("Health Settings")]
    public int maxHealth = 100;
    protected int currentHealth;

    [Header("Attack Settings")]
    public float attackRange = 1f;
    public int damage = 1;
    public float windupTime = 0.3f; // 攻击预警时间
    public float attackDuration = 0.2f; // 攻击动作持续时间
    public float attackCooldown = 0.6f;
    public float lungeSpeed = 10f; // 冲刺速度
    public LayerMask playerLayer;
    public Transform attackPos;
    public GameObject alertSign;

    // 状态机
    public EnemyStateMachine StateMachine { get; private set; }
    public PatrolState patrolState { get; private set; }
    public ChaseState chaseState { get; private set; }
    public AttackState attackState { get; private set; }
    public HurtState hurtState { get; private set; }
    public TelegraphState telegraphState { get; private set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;
        alertSign.SetActive(false);

        StateMachine = new EnemyStateMachine();
        patrolState = new PatrolState(this, StateMachine);
        chaseState = new ChaseState(this, StateMachine);
        attackState = new AttackState(this, StateMachine);
        hurtState = new HurtState(this, StateMachine);
        telegraphState = new TelegraphState(this, StateMachine);
    }

    protected virtual void Start()
    {
        startPos = transform.position;
        StateMachine.Initialize(patrolState);
    }

    protected virtual void Update()
    {
        StateMachine.CurrentState.LogicUpdate();
    }

    protected virtual void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    // 视觉更新虚方法，子类重写以实现各自的动画表现
    public virtual void UpdateVisuals() { }

    public bool CanAttack()
    {
        return Time.time >= nextAttackTime;
    }

    public void RegisterAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
    }

    public virtual void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
    {
        if (currentHealth <= 0) return;
        currentHealth -= amount;

        // 受击视觉反馈
        StartCoroutine(FlashRed());

        if (currentHealth > 0)
            StateMachine.ChangeState(hurtState);
        else
            Die();

        if (rb != null)
        { 
            Vector2 direction = (transform.position - sourcePosition).normalized;
            Vector2 force = direction * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
            rb.velocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    protected IEnumerator FlashRed()
    {
        if (sr != null)
        {
            Color originalColor = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;
        }
    }

    private float nextAttackTime;

    public Vector3 GetBackCenter()
    {
        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            float facing = transform.localScale.x >= 0f ? 1f : -1f;
            return new Vector3(bounds.center.x - facing * bounds.extents.x, bounds.center.y, bounds.center.z);
        }
        return transform.position;
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    public void CheckFlip(float xDir)
    {
        if (Mathf.Abs(xDir) < 0.1f) return;
        Vector3 scale = transform.localScale;
        scale.x = xDir > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}



