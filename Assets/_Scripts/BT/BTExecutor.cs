using UnityEngine;
using System.Collections;

using DeadCells.AI.Node;
using DeadCells.Core;

namespace DeadCells.AI.BehaviourTree
{
    /// <summary>
    /// 行为树执行器：挂载到敌人 GameObject，引用一份 BTConfig。
    /// 启动时自动编译，每帧 Evaluate。替代 DummyEnemy 的硬编码 BuildBehaviourTree。
    /// </summary>
    public class BTExecutor : MonoBehaviour, IDamageable
    {
        [Header("行为树")]
        [SerializeField] private BTConfig btConfig; 

        [Header("属性")]
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private SpriteRenderer displaySprite;

        // ---- 运行时 ----
        public Rigidbody2D Rb { get; private set; }
        public Blackboard Blackboard { get; private set; }
        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public float LastActionTime { get; set; }
        public Vector3 StartPosition { get; private set; }

        private BehaviourNode rootNode;
        private Transform player;
        private Vector3 baseScale;
        private static readonly WaitForSeconds FlashWait = new(0.1f);

        void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            if (displaySprite == null)
                displaySprite = GetComponent<SpriteRenderer>();
            CurrentHealth = maxHealth;
            baseScale = transform.localScale;
            StartPosition = transform.position;
            Blackboard = new Blackboard();
        }

        void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            CompileTree();
        }

        void Update()
        {
            if (player != null)
                Blackboard.Set("player", player);

            if (btConfig != null) btConfig.ResetRuntimeState();
            rootNode?.Tick();
            AnimateIdle();
        }

        /// <summary>(重新)编译行为树，支持热重载</summary>
        public void CompileTree()
        {
            if (btConfig == null)
            {
                Debug.LogWarning($"[BTExecutor] {name} 没有引用 BTConfig");
                return;
            }
            rootNode = BTCompiler.Compile(btConfig, this);
        }

        public void FlipSprite(float dir)
        {
            if (displaySprite != null && Mathf.Abs(dir) > 0.01f)
                displaySprite.flipX = dir < 0f;
        }

        void AnimateIdle()
        {
            float bob = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.localScale = new Vector3(baseScale.x * bob, baseScale.y * bob, 1f);
        }

        // ---- IDamageable ----

        public void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
        {
            if (CurrentHealth <= 0) return;
            CurrentHealth -= amount;
            StartCoroutine(FlashRed());

            if (CurrentHealth <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            if (Rb != null)
            {
                Vector2 dir = (transform.position - sourcePosition).normalized;
                Rb.velocity = Vector2.zero;
                Rb.AddForce(dir * knockbackForce + Vector2.up * (knockbackForce * 0.5f), ForceMode2D.Impulse);
            }
        }

        IEnumerator FlashRed()
        {
            if (displaySprite == null) yield break;
            Color orig = displaySprite.color;
            displaySprite.color = Color.red;
            yield return FlashWait;
            displaySprite.color = orig;
        }
    }
}
