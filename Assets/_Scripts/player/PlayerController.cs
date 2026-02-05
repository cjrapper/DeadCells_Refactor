using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Reflection;

public class PlayerController : MonoBehaviour, IDamageable
{ 
    #region State Machine
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerWallJumpState WallJumpState { get; private set; }
    public PlayerWallSlideState WallSlideState { get; private set; }
    #endregion

    #region Settings
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    
    public Rigidbody2D RB { get; private set; }
    public Animator Anim { get; private set; }
    public SpriteRenderer SR { get; private set; }
    public TrailRenderer TR { get; private set; }
    public PLayerEffect Effects { get; private set; }
    public Collider2D PlayerCollider { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public int amountOfJumps = 1;
    public float jumpGravityScale = 1f; // Rising gravity
    public float fallGravityScale = 2.5f; // Falling gravity

    [Header("Jump Feel (Coyote & Buffer)")]
    // 核心手感优化：土狼时间与预输入缓冲
    public float jumpBufferTime = 0.2f; // 预输入：落地前按下跳跃也能触发
    public float coyoteTime = 0.1f;     // 土狼时间：离开平台后短时间内仍可跳跃

    [Header("Ground Detection")]
    public Transform feetPos;
    public float checkRadius = 0.3f;
    public LayerMask ground;

    [Header("Combat System")]
    public Transform attackPoint;
    public WeaponData currentWeapon;
    public Transform weaponPivot;
    public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float swingDuration = 0.25f;
    public float maxSwingAngle = 120f;
    private float nextAttackTime = 0f;
    private Coroutine swingCoroutine;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public CinemachineImpulseSource impulseSource;
    public GameObject jumpDustPrefab;
    public GameObject landDustPrefab;

    [Header("One Way Platform Settings")]
    public LayerMask oneWayPlatformLayer;
    public float platformFallForce = -10f; // 下落时的向下初始速度，增强手感
    public float platformFallTime = 0.5f;  // 忽略碰撞的时间
    private Collider2D currentPlatformCollider; // 缓存当前平台的碰撞体

    [Header("Wall Jump Settings")]
    public Transform frontCheck;// Check for wall in front
    public LayerMask whatIsWall;
    public float wallSlideSpeed;
    public Vector2 wallJumpForce;// Force applied when wall jumping
    public float wallJumpTime = 0.2f; // Time to wall jump
    private bool isFallingThroughPlatform = false; // 标记是否正在穿过平台

    [Header("Debug Info")]
    public bool showDebugInfo = true;
    [SerializeField] private string currentStateName; // Inspector only
    #endregion

    // Internal Variables
    public float MoveInput { get; private set; }
    public bool IsHurting { get; private set; } // 受击状态锁
    public float JumpBufferCounter { get; private set; }
    public float CoyoteTimeCounter { get; private set; }
    
    private float coyoteTimeTimer;
    private float jumpBufferTimer;
    public void SpawnDust(GameObject dustPrefab)
    {
        if(dustPrefab != null && feetPos != null)
        {
            Instantiate(dustPrefab, feetPos.position, Quaternion.identity);
        }
    }

    public bool CheckGrounded()
    {
        if (feetPos == null) return false;
        return !isFallingThroughPlatform && (Physics2D.OverlapCircle(feetPos.position, checkRadius, ground) || Physics2D.OverlapCircle(feetPos.position, checkRadius, oneWayPlatformLayer));
    }

    public bool CheckTouchingWall()
    {
        if (frontCheck == null) return false;
        return !isFallingThroughPlatform && Physics2D.OverlapCircle(frontCheck.position, checkRadius, whatIsWall);
    }

    void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        TR = GetComponent<TrailRenderer>();
        PlayerCollider = GetComponent<Collider2D>();
        SR = GetComponentInChildren<SpriteRenderer>();
        Effects = GetComponentInChildren<PLayerEffect>();
        Anim = GetComponentInChildren<Animator>();

        if (RB == null) Debug.LogError("PlayerController: Rigidbody2D is missing!");
        if (feetPos == null) Debug.LogWarning("PlayerController: Feet Pos is not assigned!");
        if (frontCheck == null) Debug.LogWarning("PlayerController: Front Check is not assigned!");
        if (Effects == null) Debug.LogWarning("PlayerController: PLayerEffect component is missing (neither on this object nor children)!");

        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine, "Idle");
        MoveState = new PlayerMoveState(this, StateMachine, "Move");
        JumpState = new PlayerJumpState(this, StateMachine, "Jump");
        FallState = new PlayerFallState(this, StateMachine, "Fall");
        DashState = new PlayerDashState(this, StateMachine, "Dash");
        AttackState = new PlayerAttackState(this, StateMachine, "Attack");
        // Reuse "Jump" animation for WallJump since we don't have a specific one
        WallJumpState = new PlayerWallJumpState(this, StateMachine, "Jump");
        // Reuse "Fall" animation for WallSlide since we don't have a specific one
        WallSlideState = new PlayerWallSlideState(this, StateMachine, "Fall");

        // Fix Wall Stick
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        PlayerCollider.sharedMaterial = noFriction;

        currentHealth = maxHealth;
        if (weaponPivot == null && attackPoint != null)
        {
            weaponPivot = attackPoint;
        }
    }

    void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    void Update()
    {
        if (IsHurting) return;

        // 1. Input Processing
        MoveInput = Input.GetAxisRaw("Horizontal"); 

        // 2. Flip Character
        if (StateMachine.currentState != WallJumpState)
        {
            if (MoveInput > 0) transform.localScale = new Vector3(1, 1, 1);
            else if (MoveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
        }

        // 3. Coyote & Jump Buffer
        if (CheckGrounded()) coyoteTimeTimer = coyoteTime;
        else coyoteTimeTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        CoyoteTimeCounter = coyoteTimeTimer;
        JumpBufferCounter = jumpBufferTimer;

        // 4. State Machine Updates
        StateMachine.currentState.HandleInput();
        StateMachine.currentState.LogicUpdate();

        // 5. Debug Info Update
        if (StateMachine.currentState != null)
        {
            currentStateName = StateMachine.currentState.GetType().Name.Replace("Player", "");
        }
        if (Input.GetKeyDown(KeyCode.BackQuote)) // Toggle Debug UI with ` key
        {
            showDebugInfo = !showDebugInfo;
        }
    }

    void FixedUpdate()
    {
        if (IsHurting) return;
        StateMachine.currentState.PhysicsUpdate();
    }

    public void SetJumpBuffer(float value) => jumpBufferTimer = value;
    public void SetCoyoteTime(float value) => coyoteTimeTimer = value;

    public void TakeDamage(int amount,Vector3 sourcePosition, float knockbackForce)
    {
        if (IsHurting) return; // 防止连续受击

        currentHealth -= amount;
        // Update Health Bar
        if(UIManager.instance != null)
        {
            UIManager.instance.UpdateHealthBar(currentHealth,maxHealth);
        }
        
        // 1. Knockback Logic
        if (RB != null)
        {
            // 进入受击状态，锁定移动
            StartCoroutine(KnockbackRoutine(knockbackForce));
            
            Vector2 direction = (transform.position - sourcePosition).normalized;
            // 击退方向稍微向上一点，防止贴地摩擦过大
            Vector2 force = direction * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
            
            RB.velocity = Vector2.zero;
            RB.AddForce(force, ForceMode2D.Impulse);
        }

        // 2. Visual Feedback
        StartCoroutine(FlashEffect());

        if(impulseSource != null)
        {
            // 强制给一个随机方向的力，确保一定会震动
            Vector3 shakeVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0) * 0.5f;
            impulseSource.GenerateImpulse(shakeVelocity);
        }
        if(hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab,transform.position,Quaternion.identity);
        }

        if (currentHealth <= 0) Die();
    }
    //
    System.Collections.IEnumerator FlashEffect()
    {
        if(SR != null)
        {
            Color original = SR.color;
            SR.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            SR.color = original;
        }
    }
    void Die()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
    }

    IEnumerator PlaySwingCurve()
    {
        if (weaponPivot == null || swingDuration <= 0f || swingCurve == null)
        {
            swingCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            float progress = elapsed / swingDuration;
            float curveValue = swingCurve.Evaluate(progress);
            weaponPivot.localRotation = Quaternion.Euler(0f, 0f, -curveValue * maxSwingAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponPivot.localRotation = Quaternion.identity;
        swingCoroutine = null;
    }

    public bool CanAttack()
    {
        return currentWeapon != null && Time.time >= nextAttackTime;
    }

    public void UpdateNextAttackTime()
    {
        if (currentWeapon != null)
        {
            nextAttackTime = Time.time + currentWeapon.cooldown;
        }
    }

    public void StartSwing()
    {
        if (swingCoroutine != null) StopCoroutine(swingCoroutine);
        swingCoroutine = StartCoroutine(PlaySwingCurve());
    }

    // 受击硬直协程
    IEnumerator KnockbackRoutine(float force)
    {
        IsHurting = true;
        // 根据受击力度动态调整硬直时间，或者固定一个时间（例如 0.2s）
        yield return new WaitForSeconds(0.2f);
        IsHurting = false;
    }

    // 持续检测脚下的单向平台
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "OneWayPlatform")
        {
            currentPlatformCollider = collision.collider;
        }
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "OneWayPlatform")
        {
            currentPlatformCollider = collision.collider;
        }
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        // 只有当离开的平台是当前记录的平台时，才置空
        if (collision.gameObject.tag == "OneWayPlatform" && collision.collider == currentPlatformCollider)
        {
            currentPlatformCollider = null;
        }
    }

    IEnumerator DisableCollision()
    {
        // 安全检查：如果当前没有平台，直接退出
        if (currentPlatformCollider == null) yield break;

        // 锁定该次操作针对的平台（防止协程中途 currentPlatformCollider 发生变化）
        Collider2D targetPlatform = currentPlatformCollider;

        isFallingThroughPlatform = true; // 开始穿墙

        Physics2D.IgnoreCollision(PlayerCollider, targetPlatform, true);
        
        // 施加向下的初始速度
        RB.velocity = new Vector2(RB.velocity.x, platformFallForce);

        yield return new WaitForSeconds(platformFallTime);
        
        // 恢复碰撞前再次检查对象是否存在
        if (targetPlatform != null)
        {
            Physics2D.IgnoreCollision(PlayerCollider, targetPlatform, false);
        }

        isFallingThroughPlatform = false; // 结束穿墙
    }

    void OnDrawGizmos()
    {
        if(attackPoint != null && currentWeapon != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
        }
        Gizmos.color = Color.red;
        if (feetPos != null)
            Gizmos.DrawWireSphere(feetPos.position, checkRadius);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // Define style
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 18;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        // Build info string
        string info = $"<b>[DEV MODE]</b>\n" +
                      $"State: <color=yellow>{currentStateName}</color>\n" +
                      $"Vel: {RB.velocity:F2}\n" +
                      $"Grounded: {CheckGrounded()}\n" +
                      $"Wall: {CheckTouchingWall()}\n" +
                      $"FPS: {(1.0f / Time.smoothDeltaTime):F0}";

        // Draw box (dynamic height based on content)
        float height = 140f;
        GUI.Box(new Rect(10, 10, 200, height), info, style);
    }
}
