using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;


public class PlayerController : MonoBehaviour, IDamageable
{
    #region Components
    public Rigidbody2D RigidBody { get; private set; }
    public Animator Animator { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public TrailRenderer TrailRenderer { get; private set; }
    public PLayerEffect PlayerVFX { get; private set; } 
    public Collider2D BodyCollider { get; private set; }

    [Header("Check Transforms")]
    public Transform GroundCheckPos; 
    public Transform WallCheckPos;   
    public Transform AttackOrigin;   
    public Transform WeaponPivotTransform; 
    #endregion

    #region Settings
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public int MaxJumpCount = 1; 
    public float RisingGravityScale = 1f; 
    public float FallingGravityScale = 2.5f; 

    [Header("Jump Physics")]
    public float JumpBufferDuration = 0.2f; 
    public float CoyoteDuration = 0.1f;     

    [Header("Physics")]
    public PhysicsMaterial2D noFrictionMaterial;

    [Header("Physics Layers")]
    public float CheckRadius = 0.3f;
    public LayerMask GroundLayer;
    public LayerMask WallLayer;
    public LayerMask OneWayPlatformLayer;

    [Header("Combat")]
    public List<WeaponData> WeaponInventory; 
    public WeaponData CurrentWeapon; 
    private int currentWeaponIndex = 0;
    public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float swingDuration = 0.25f;
    public float maxSwingAngle = 120f;
    private float nextAttackTime = 0f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;

    [Header("VFX")]
    public GameObject HitEffectPrefab;
    public CinemachineImpulseSource ImpulseSource;
    public GameObject JumpDustPrefab;
    public GameObject LandDustPrefab;

    // Cached VFX pool references (lazy lookup with Instantiate fallback)
    private SamplePool jumpDustPool;
    private SamplePool landDustPool;
    private SamplePool hitEffectPool;
    private bool poolsInitialized;

    [Header("One Way Platform")]
    // 单向平台下落参数：不再需要，改为瞬移方案
    private Collider2D currentPlatformCollider;

    [Header("Wall Mechanics")]
    public float wallSlideSpeed;
    public Vector2 wallJumpForce;
    public float wallJumpTime = 0.2f;

    [Header("Debug")]
    public bool showDebugInfo = true;
    [SerializeField] private string currentStateName;
    #endregion

    // Cached WaitForSeconds to avoid per-coroutine allocations
    private static readonly WaitForSeconds FlashWait = new WaitForSeconds(0.1f);
    private static readonly WaitForSeconds KnockbackWait = new WaitForSeconds(0.2f);

    #region Runtime
    public float InputX { get; private set; }
    public bool IsHurting { get; private set; }
    public float JumpBufferTimer { get; private set; }
    public float CoyoteTimeCounter { get; private set; }

    private float coyoteTimer; 
    private float jumpBufferTimer; 
    private bool isFallingThroughPlatform = false;
    private Coroutine swingCoroutine;
    private Vector2 WorkVector;

    // Cached FSM state instances (reuse to avoid per-frame GC allocations)
    private InnerIdleState innerIdleState;
    private InnerMoveState innerMoveState;
    private InnerJumpState innerJumpState;
    private InnerFallState innerFallState;
    private InnerDashState innerDashState;
    private InnerAttackState innerAttackState;
    private InnerWallSlideState innerWallSlideState;
    private InnerWallJumpState innerWallJumpState;
    #endregion

    void Awake()
    {
        RigidBody = GetComponent<Rigidbody2D>();
        TrailRenderer = GetComponent<TrailRenderer>();
        BodyCollider = GetComponent<Collider2D>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        PlayerVFX = GetComponentInChildren<PLayerEffect>();
        Animator = GetComponentInChildren<Animator>();

        if (RigidBody == null) Debug.LogError("PlayerController: Rigidbody2D is missing!");
        if (GroundCheckPos == null) Debug.LogWarning("PlayerController: Ground Check Pos is not assigned!");
        if (WallCheckPos == null) Debug.LogWarning("PlayerController: Wall Check Pos is not assigned!");
        
        // Fix Wall Stick via serialized material
        if (noFrictionMaterial != null)
        {
            BodyCollider.sharedMaterial = noFrictionMaterial;
        }

        currentHealth = maxHealth;
        if (WeaponPivotTransform == null && AttackOrigin != null)
        {
            WeaponPivotTransform = AttackOrigin;
        }

        if (WeaponInventory != null && WeaponInventory.Count > 0)
        {
            CurrentWeapon = WeaponInventory[0];
            currentWeaponIndex = 0;
        }

        // Pre-create FSM state instances to avoid per-frame allocations
        innerIdleState = new InnerIdleState(this);
        innerMoveState = new InnerMoveState(this);
        innerJumpState = new InnerJumpState(this);
        innerFallState = new InnerFallState(this);
        innerDashState = new InnerDashState(this);
        innerAttackState = new InnerAttackState(this);
        innerWallSlideState = new InnerWallSlideState(this);
        innerWallJumpState = new InnerWallJumpState(this);
    }

    void Start()
    {
        if (SpriteRenderer != null) SpriteRenderer.sortingOrder = 10;
        InitializeInnerFSM();
    }

    void Update()
    {
        if (IsHurting) return;

        // 1. Input
        InputX = Input.GetAxisRaw("Horizontal");

        // 2. Timers
        if (CheckGrounded()) coyoteTimer = CoyoteDuration;
        else coyoteTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = JumpBufferDuration;
        else jumpBufferTimer -= Time.deltaTime;

        CoyoteTimeCounter = coyoteTimer;
        JumpBufferTimer = jumpBufferTimer;

        // 3. FSM
        RunInnerFSM();

        // 4. Debug
        if (Input.GetKeyDown(KeyCode.BackQuote)) showDebugInfo = !showDebugInfo;

        // 5. Weapon
        if (Input.GetKeyDown(KeyCode.Q)) SwitchWeapon();
    }

    void FixedUpdate()
    {
        if (IsHurting) return;
        RunInnerPhysics();
    }

    // ============================================================================
    //  Public Methods
    // ============================================================================

    public void SpawnDust(GameObject dustPrefab)
    {
        if (dustPrefab != null && GroundCheckPos != null)
        {
            InitVfxPools();

            SamplePool pool = null;
            if (dustPrefab == JumpDustPrefab) pool = jumpDustPool;
            else if (dustPrefab == LandDustPrefab) pool = landDustPool;

            if (pool != null)
            {
                GameObject dust = pool.Get();
                if (dust != null)
                {
                    dust.transform.position = GroundCheckPos.position;
                    dust.transform.rotation = Quaternion.identity;
                    StartCoroutine(ReturnAfterDelay(dust, pool, 1f));
                    return;
                }
            }
            // 生成灰尘特效 (Spawn Dust VFX) — fallback
            Instantiate(dustPrefab, GroundCheckPos.position, Quaternion.identity);
        }
    }

    public bool CheckGrounded()
    {
        if (GroundCheckPos == null) return false;
        return !isFallingThroughPlatform && (Physics2D.OverlapCircle(GroundCheckPos.position, CheckRadius, GroundLayer) || Physics2D.OverlapCircle(GroundCheckPos.position, CheckRadius, OneWayPlatformLayer));
    }

    public bool CheckTouchingWall()
    {
        if (WallCheckPos == null) return false;
        return !isFallingThroughPlatform && Physics2D.OverlapCircle(WallCheckPos.position, CheckRadius, WallLayer);
    }

    public void SwitchWeapon()
    {
        if (WeaponInventory == null || WeaponInventory.Count == 0) return;

        currentWeaponIndex++;
        if (currentWeaponIndex >= WeaponInventory.Count)
        {
            currentWeaponIndex = 0;
        }

        CurrentWeapon = WeaponInventory[currentWeaponIndex];
        Debug.Log($"Switched to weapon: {CurrentWeapon.name}");
    }

    private void InitVfxPools()
    {
        if (poolsInitialized) return;
        poolsInitialized = true;

        var jumpPoolObj = GameObject.Find("JumpDustPool");
        if (jumpPoolObj != null) jumpDustPool = jumpPoolObj.GetComponent<SamplePool>();

        var landPoolObj = GameObject.Find("LandDustPool");
        if (landPoolObj != null) landDustPool = landPoolObj.GetComponent<SamplePool>();

        var hitPoolObj = GameObject.Find("HitEffectPool");
        if (hitPoolObj != null) hitEffectPool = hitPoolObj.GetComponent<SamplePool>();
    }

    private IEnumerator ReturnAfterDelay(GameObject obj, SamplePool pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (pool != null && obj != null) pool.Return(obj);
    }

    public void SetJumpBuffer(float value) => jumpBufferTimer = value;
    public void SetCoyoteTime(float value) => coyoteTimer = value;

    // 处理受击逻辑：扣血、UI更新、击退、特效、震动
    public void TakeDamage(int amount,Vector3 sourcePosition, float knockbackForce)
    {
        if (IsHurting) return; // 避免短时间内连续受击

        currentHealth -= amount;
        if(EventCenter.Instance != null)
        {
            EventCenter.Instance.Broadcast(EventCenter.EventType.PlayerHealthChange.ToString(), currentHealth, maxHealth);
        }
        
        // 击退逻辑 (Knockback)
        if (RigidBody != null)
        {
            StartCoroutine(KnockbackRoutine(knockbackForce));
            
            // 计算击退方向（从伤害源推向玩家）
            Vector2 direction = (transform.position - sourcePosition).normalized;
            // 添加向上的分量，产生抛物线击退效果
            Vector2 force = direction * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
            
            RigidBody.velocity = Vector2.zero; // 重置当前速度，保证击退力度一致
            RigidBody.AddForce(force, ForceMode2D.Impulse);
        }

        // 视觉反馈 (Visual Feedback)
        StartCoroutine(FlashEffect());

        // 屏幕震动
        if(ImpulseSource != null)
        {
            Vector3 shakeVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0) * 0.5f;
            ImpulseSource.GenerateImpulse(shakeVelocity);
        }

        // 受击特效
        if(HitEffectPrefab != null)
        {
            InitVfxPools();
            if (hitEffectPool != null)
            {
                GameObject effect = hitEffectPool.Get();
                if (effect != null)
                {
                    effect.transform.position = transform.position;
                    effect.transform.rotation = Quaternion.identity;
                    StartCoroutine(ReturnAfterDelay(effect, hitEffectPool, 1f));
                }
                else
                {
                    Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);
                }
            }
            else
            {
                // 生成受击特效 (Spawn Hit VFX) — fallback
                Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        if (currentHealth <= 0) Die();
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if(SpriteRenderer != null)
        {
            Color original = SpriteRenderer.color;
            SpriteRenderer.color = Color.red;
            yield return FlashWait;
            SpriteRenderer.color = original;
        }
    }

    private static float previousTimeScale = 1f;

    void Die()
    {
        Debug.Log("Game Over!");
        if(EventCenter.Instance != null)
        {
            EventCenter.Instance.Broadcast(EventCenter.EventType.PlayerDead.ToString());
        }
        // 只在时间正常运行时才保存（避免 HitStop 期间 timeScale=0 被错误保存）
        if (Time.timeScale > 0f)
            previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public static void RestoreTimeScale()
    {
        Time.timeScale = previousTimeScale;
    }

    IEnumerator PlaySwingCurve()
    {
        if (WeaponPivotTransform == null || swingDuration <= 0f || swingCurve == null)
        {
            swingCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            float progress = elapsed / swingDuration;
            float curveValue = swingCurve.Evaluate(progress);
            WeaponPivotTransform.localRotation = Quaternion.Euler(0f, 0f, -curveValue * maxSwingAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        WeaponPivotTransform.localRotation = Quaternion.identity;
        swingCoroutine = null;
    }

    public bool CanAttack()
    {
        return CurrentWeapon != null && Time.time >= nextAttackTime;
    }

    public void UpdateNextAttackTime()
    {
        if (CurrentWeapon != null)
        {
            nextAttackTime = Time.time + CurrentWeapon.cooldown;
        }
    }

    public void StartSwing()
    {
        if (swingCoroutine != null) StopCoroutine(swingCoroutine);
        swingCoroutine = StartCoroutine(PlaySwingCurve());
    }

    IEnumerator KnockbackRoutine(float force)
    {
        IsHurting = true;
        yield return KnockbackWait;
        IsHurting = false;
    }

    // Platform Collision Handling
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
        if (collision.gameObject.tag == "OneWayPlatform" && collision.collider == currentPlatformCollider)
        {
            currentPlatformCollider = null;
        }
    }

    private Coroutine platformDropCoroutine;

    public void StartPlatformDrop()
    {
        if (currentPlatformCollider == null) return;
        if (platformDropCoroutine != null) StopCoroutine(platformDropCoroutine);
        platformDropCoroutine = StartCoroutine(PlatformDropRoutine());
    }

    /// <summary>
    /// 单向平台下落：无视碰撞一帧，把角色瞬移到平台下方，让重力自然接管。
    /// 不用定时器、不改速度、不设重力，避免穿透多层平台。
    /// </summary>
    IEnumerator PlatformDropRoutine()
    {
        Collider2D platform = currentPlatformCollider;
        isFallingThroughPlatform = true;

        // 忽略碰撞一帧就够了
        Physics2D.IgnoreCollision(BodyCollider, platform, true);

        // 把角色瞬移到平台下方（角色碰撞体高度 + 一点余量）
        float dropDistance = BodyCollider.bounds.size.y + 0.15f;
        transform.position += Vector3.down * dropDistance;

        yield return null; // 等一帧，让物理引擎更新

        Physics2D.IgnoreCollision(BodyCollider, platform, false);
        isFallingThroughPlatform = false;
        platformDropCoroutine = null;
    }

    void OnDrawGizmos()
    {
        if(AttackOrigin != null && CurrentWeapon != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(AttackOrigin.position, CurrentWeapon.attackRange);
        }
        Gizmos.color = Color.red;
        if (GroundCheckPos != null)
            Gizmos.DrawWireSphere(GroundCheckPos.position, CheckRadius);
    }

    

    // ============================================================================
    //  Inner FSM Classes (内部状态机类)
    //  所有状态逻辑都在此处集中管理，直接访问 PlayerController 的私有成员
    // ============================================================================

    public abstract class BaseState
    {
        protected PlayerController core;
        protected float startTime;

        public BaseState(PlayerController _core) => core = _core;

        public virtual void Enter() 
        { 
            startTime = Time.time;
            // 调试模式下打印状态切换日志
            if (core.showDebugInfo) Debug.Log($"Enter State: {this.GetType().Name}");
        }
        public virtual void Exit() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
    }

    /// <summary>
    /// 站立状态：处理跳跃、移动、攻击、冲刺、下落以及单向平台下落
    /// </summary>
    public class InnerIdleState : BaseState
    {
        public InnerIdleState(PlayerController core) : base(core) { }

        public override void Enter()
        {
            base.Enter();
            core.SetVelocityX(0); 
            core.RigidBody.gravityScale = core.FallingGravityScale; // 使用下落重力（通常较大，手感更稳）
            if(core.Animator != null) core.Animator.CrossFade("Idle", 0.1f);
        }

        public override void LogicUpdate()
        {
            // 切换到移动状态
            if (core.InputX != 0) core.SwitchInnerState(core.innerMoveState);
            
            // 单向平台下落 (S + Jump)
            if (Input.GetAxisRaw("Vertical") < 0 && Input.GetButtonDown("Jump"))
            {
                core.StartPlatformDrop();
                return; // 优先处理下落，避免触发普通跳跃
            }

            // 普通跳跃
            if (Input.GetButtonDown("Jump") && core.CheckGrounded())
                core.SwitchInnerState(core.innerJumpState);
                
            // 攻击输入
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1")) 
                core.SwitchInnerState(core.innerAttackState);

            // 冲刺输入
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(core.innerDashState);
                
            // 自然下落（例如走出边缘）
            if (!core.CheckGrounded())
                core.SwitchInnerState(core.innerFallState);
        }
    }

    /// <summary>
    /// 移动状态：处理水平移动、跳跃、攻击等
    /// </summary>
    public class InnerMoveState : BaseState
    {
        public InnerMoveState(PlayerController core) : base(core) { }

        public override void Enter()
        {
            base.Enter();
            core.RigidBody.gravityScale = core.FallingGravityScale;
            if(core.Animator != null) core.Animator.CrossFade("Move", 0.1f);
        }

        public override void LogicUpdate()
        {
            core.CheckFlip(); // 检查翻转
            core.SetVelocityX(core.moveSpeed * core.InputX);

            // 停止移动 -> 切回 Idle
            if (core.InputX == 0) core.SwitchInnerState(core.innerIdleState);
            
            // 单向平台下落 (S + Jump)
            if (Input.GetAxisRaw("Vertical") < 0 && Input.GetButtonDown("Jump"))
            {
                core.StartPlatformDrop();
                return;
            }

            // 跳跃
            if (Input.GetButtonDown("Jump") && core.CheckGrounded())
                core.SwitchInnerState(core.innerJumpState);
                
            // 攻击
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(core.innerAttackState);

            // 冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(core.innerDashState);

            // 下落
            if (!core.CheckGrounded())
                core.SwitchInnerState(core.innerFallState);
        }
    }

    /// <summary>
    /// 跳跃状态：处理上升过程、空中移动、二段跳（如需）、贴墙判定
    /// </summary>
    public class InnerJumpState : BaseState
    {
        public InnerJumpState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            core.SetVelocityY(core.jumpForce);
            core.RigidBody.gravityScale = core.RisingGravityScale; // 上升重力（较小，手感更轻盈）
            if(core.Animator != null) core.Animator.CrossFade("Jump", 0.1f);
            core.SpawnDust(core.JumpDustPrefab);
        }
        public override void LogicUpdate()
        {
            core.CheckFlip();
            core.SetVelocityX(core.moveSpeed * core.InputX);

            // 速度小于0转为下落
            if (core.RigidBody.velocity.y < 0) core.SwitchInnerState(core.innerFallState);
            
            // 贴墙检测 -> 切换滑墙
            if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
            {
                 core.SwitchInnerState(core.innerWallSlideState);
            }

            // 空中冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(core.innerDashState);
                
            // 空中攻击
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(core.innerAttackState);
        }
    }

    /// <summary>
    /// 下落状态：处理自然下落、落地检测、土狼时间跳跃
    /// </summary>
    public class InnerFallState : BaseState
    {
        public InnerFallState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            core.RigidBody.gravityScale = core.FallingGravityScale; // 下落重力
            if(core.Animator != null) core.Animator.CrossFade("Fall", 0.1f);
        }
        public override void LogicUpdate()
        {
            core.CheckFlip();
            core.SetVelocityX(core.moveSpeed * core.InputX);

            // 落地检测
            if (core.CheckGrounded())
            {
                core.SpawnDust(core.LandDustPrefab);
                core.SwitchInnerState(core.innerIdleState);
            }
            
            // 贴墙检测
            if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
            {
                 core.SwitchInnerState(core.innerWallSlideState);
            }

            // 土狼时间 (Coyote Time)：允许离开平台一小段时间内仍可跳跃
            if (core.CoyoteTimeCounter > 0 && Input.GetButtonDown("Jump"))
                 core.SwitchInnerState(core.innerJumpState);

            // 空中冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(core.innerDashState);

            // 空中攻击
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(core.innerAttackState);
        }
    }

    /// <summary>
    /// 冲刺状态：忽略重力、快速移动、无敌帧（可选）
    /// </summary>
    public class InnerDashState : BaseState
    {
        public InnerDashState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            if(core.Animator != null) core.Animator.CrossFade("Dash", 0f); 
            core.dashCooldownTimer = core.dashCooldown; 
            int dir = core.transform.localScale.x > 0 ? 1 : -1;
            core.SetVelocity(dir * core.dashSpeed, 0); // 冲刺时Y轴速度清零
            core.RigidBody.gravityScale = 0; // 关闭重力
        }
        public override void Exit()
        {
            core.RigidBody.gravityScale = core.FallingGravityScale; // 恢复重力
            core.SetVelocityX(0); // 冲刺结束稍微停顿一下（可选）
        }
        public override void LogicUpdate()
        {
            if (Time.time >= startTime + core.dashTime)
                core.SwitchInnerState(core.innerIdleState);
        }
    }

    /// <summary>
    /// 攻击状态：调用 WeaponData 执行具体逻辑，处理硬直时间
    /// </summary>
    public class InnerAttackState : BaseState
    {
        public InnerAttackState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            // 地面攻击时静止，空中攻击保留惯性
            if (core.CheckGrounded())
            {
                 core.SetVelocityX(0);
            }
            
            if (core.CurrentWeapon != null)
            {
                core.CurrentWeapon.Attack(core);
                // 如果是近战，播放挥剑曲线动画
                if (core.CurrentWeapon.useMeleeSwing)
                     core.StartSwing(); // 用 StartSwing 避免协程堆积
            }
        }
        public override void LogicUpdate()
        {
            // 攻击动作结束判定
            if (Time.time >= startTime + core.swingDuration + 0.1f) 
            {
                if (core.CheckGrounded())
                    core.SwitchInnerState(core.innerIdleState);
                else
                    core.SwitchInnerState(core.innerFallState);
            }
        }
    }

    /// <summary>
    /// 滑墙状态：处理贴墙下滑、蹬墙跳
    /// </summary>
    public class InnerWallSlideState : BaseState
    {
        public InnerWallSlideState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            if(core.Animator != null) core.Animator.CrossFade("Fall", 0.1f); 
        }
        public override void LogicUpdate()
        {
            // 蹬墙跳
            if (Input.GetButtonDown("Jump"))
            {
                core.SwitchInnerState(core.innerWallJumpState);
                return;
            }

            // 脱离墙壁判定：输入反向或不再贴墙
            bool isMovingAway = core.InputX != 0 && core.InputX != core.transform.localScale.x;
            
            if (!core.CheckTouchingWall() || core.CheckGrounded() || isMovingAway)
            {
                if(core.CheckGrounded()) core.SwitchInnerState(core.innerIdleState);
                else core.SwitchInnerState(core.innerFallState);
                return;
            }

            // 施加滑墙摩擦力（匀速下滑）
            core.SetVelocity(core.RigidBody.velocity.x, -core.wallSlideSpeed);
        }
    }

    /// <summary>
    /// 蹬墙跳状态：施加反向力，短暂锁定输入
    /// </summary>
    public class InnerWallJumpState : BaseState
    {
        public InnerWallJumpState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            if(core.Animator != null) core.Animator.CrossFade("Jump", 0.1f);
            
            float jumpDir = -core.transform.localScale.x; // 反向跳跃
            
            Vector2 force = new Vector2(core.wallJumpForce.x * jumpDir, core.wallJumpForce.y);
            core.RigidBody.velocity = Vector2.zero; 
            core.RigidBody.AddForce(force, ForceMode2D.Impulse);
            
            core.Flip(); // 立即转向
        }
        public override void LogicUpdate()
        {
            // 锁定时间结束，转为下落
            if (Time.time >= startTime + core.wallJumpTime)
            {
                core.SwitchInnerState(core.innerFallState);
            }
        }
    }

    // ============================================================================
    //  Helper Methods (通用辅助方法)
    // ============================================================================
    private BaseState currentInnerState;
    public float dashCooldownTimer; 

    public void InitializeInnerFSM()
    {
        SwitchInnerState(innerIdleState);
    }

    public void SwitchInnerState(BaseState newState)
    {
        currentInnerState?.Exit();
        currentInnerState = newState;
        currentInnerState.Enter();
        currentStateName = newState.GetType().Name; 
        EventCenter.Instance?.Broadcast(EventCenter.EventType.PlayerStateChange.ToString());
    }

    public void RunInnerFSM()
    {
        if(dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        currentInnerState?.LogicUpdate();
    }
    
    public void RunInnerPhysics()
    {
        currentInnerState?.PhysicsUpdate();
    }

    private void SetVelocityX(float x) 
    {
        WorkVector.Set(x, RigidBody.velocity.y);
        RigidBody.velocity = WorkVector;
    }

    private void SetVelocityY(float y)
    {
        WorkVector.Set(RigidBody.velocity.x, y);
        RigidBody.velocity = WorkVector;
    }

    private void SetVelocity(float x, float y)
    {
        WorkVector.Set(x, y);
        RigidBody.velocity = WorkVector;
    }
    
    private void CheckFlip()
    {
        if (InputX > 0 && transform.localScale.x < 0) Flip();
        else if (InputX < 0 && transform.localScale.x > 0) Flip();
    }

    private void Flip()
    {
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }
}