using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System;

using DeadCells.Core;

namespace DeadCells.Enemy
{
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
        public float territoryRange = 10f;
        public Vector3 startPos;

        [Header("Health Settings")]
        public int maxHealth = 100;
        protected int currentHealth;

        [Header("Vision Settings")]
        public float visionHeight = 3f;

        [Header("Attack Settings")]
        public float attackRange = 1f;
        public int damage = 1;
        public float windupTime = 0.3f; // 攻击预警时间
        public float attackDuration = 0.2f; // 攻击动作持续时间
        public float attackCooldown = 1.5f;
        public float lungeSpeed = 8f; // 冲刺速度
        public LayerMask playerLayer;
        public Transform attackPos;
        public GameObject alertSign;

        // 状态机
        [HideInInspector] public bool useLuaFSM = false;  // 如果挂载了 EnemyLuaBridge，由 Lua 接管
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
            if (alertSign != null) alertSign.SetActive(false);

            StateMachine = new EnemyStateMachine();
            patrolState = new PatrolState(this, StateMachine);
            chaseState = new ChaseState(this, StateMachine);
            attackState = new AttackState(this, StateMachine);
            hurtState = new HurtState(this, StateMachine);
            telegraphState = new TelegraphState(this, StateMachine);
        }

        protected virtual void Start()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }
            startPos = transform.position;
            StateMachine.Initialize(patrolState);
        }

        protected virtual void Update()
        {
            if (!useLuaFSM)
                StateMachine.CurrentState.LogicUpdate();
        }

        protected virtual void FixedUpdate()
        {
            if (!useLuaFSM)
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

        public bool CanSeePlayer()
        {
            if (player == null) return false;
            float distSqr = (transform.position - player.position).sqrMagnitude;
            if (distSqr > chaseRange * chaseRange) return false;
            return Mathf.Abs(transform.position.y - player.position.y) <= visionHeight;
        }

        // 敌人受击逻辑
        public virtual void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
        {
            if (currentHealth <= 0) return;
            currentHealth -= amount;

            // 受击视觉反馈 (闪红)
            StartCoroutine(FlashRed());

            // 切换到受击状态或死亡
            if (currentHealth > 0)
                StateMachine.ChangeState(hurtState); // 打断当前动作进入硬直
            else
                Die();

            // 施加物理击退
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
                yield return FlashWait;
                sr.color = originalColor;
            }
        }

        private static readonly WaitForSeconds FlashWait = new WaitForSeconds(0.1f);
        private float nextAttackTime;

        // 获取敌人背部中心点 (可用于背刺判定或特效生成)
        public Vector3 GetBackCenter()
        {
            if (bodyCollider != null)
            {
                Bounds bounds = bodyCollider.bounds;
                // 根据朝向计算背部位置
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
}
