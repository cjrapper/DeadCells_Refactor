using System.Collections;
using UnityEngine;

using AngryBirds.Core;

namespace AngryBirds.AI.Node
{
    public class DummyEnemy : MonoBehaviour, IDamageable
{
    // 组件引用
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // BT
    private BehaviourNode rootNode;
    private Blackboard blackboard;

    // 配置
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;
    private Transform player;

    private Vector3 baseScale;
    private static readonly WaitForSeconds FlashWait = new WaitForSeconds(0.1f);

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        baseScale = transform.localScale;
        blackboard = new Blackboard();
        BuildBehaviourTree();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        blackboard.Set("player", player);
        if (rootNode != null)
            rootNode.Tick();
        UpdateVisual();
    }

    void BuildBehaviourTree()
    {
        rootNode = new SelectorNode(
            new SequenceNode(
                new ConditionNode(IsPlayerInRange),
                new ActionNode(MoveToPlayer)
            ),
            new ActionNode(StandIdle)
        );
        rootNode.Blackboard = blackboard;
    }

    // 行为节点
    bool IsPlayerInRange()
    {
        Transform p = blackboard.Get<Transform>("player");
        if (p == null)
            return false;
        float distSqr = (transform.position - p.position).sqrMagnitude;
        return distSqr <= detectionRange * detectionRange;
    }

    NodeState MoveToPlayer()
    {
        Transform p = blackboard.Get<Transform>("player");
        if (p == null || rb == null)
            return NodeState.Failure;

        float dir = Mathf.Sign(p.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        CheckFlip(dir);
        return NodeState.Success;
    }

    NodeState StandIdle()
    {
        if (rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);
        return NodeState.Success;
    }

    void UpdateVisual()
    {
        float bob = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
        transform.localScale = new Vector3(baseScale.x * bob, baseScale.y * bob, 1f);
    }

    void CheckFlip(float dir)
    {
        if (sr == null) return;
        sr.flipX = dir < 0f;
    }

    public void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 击退：远离伤害来源
        if (rb != null)
        {
            Vector2 dir = (transform.position - sourcePosition).normalized;
            rb.velocity = Vector2.zero;
            rb.AddForce(dir * knockbackForce + Vector2.up * (knockbackForce * 0.5f), ForceMode2D.Impulse);
        }
    }

    void Die()
    {
        gameObject.SetActive(false);
    }

    IEnumerator FlashRed()
    {
        if (sr == null) yield break;
        Color originalColor = sr.color;
        sr.color = Color.red;
        yield return FlashWait;
        sr.color = originalColor;
    }
}
}