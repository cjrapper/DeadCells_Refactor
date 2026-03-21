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

    [Header("One Way Platform")]
    public float PlatformFallSpeed = -10f; 
    public float platformFallTime = 0.5f;
    private Collider2D currentPlatformCollider;

    [Header("Wall Mechanics")]
    public float wallSlideSpeed;
    public Vector2 wallJumpForce;
    public float wallJumpTime = 0.2f;

    [Header("Debug")]
    public bool showDebugInfo = true;
    [SerializeField] private string currentStateName;
    #endregion

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
        
        // Fix Wall Stick
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        BodyCollider.sharedMaterial = noFriction;

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
        if(dustPrefab != null && GroundCheckPos != null)
        {
            // 生成灰尘特效 (Spawn Dust VFX)
            // 确保 Prefab 的 Sorting Order 在 Inspector 中已正确设置
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

    public void SetJumpBuffer(float value) => jumpBufferTimer = value;
    public void SetCoyoteTime(float value) => coyoteTimer = value;

    // 处理受击逻辑：扣血、UI更新、击退、特效、震动
    public void TakeDamage(int amount,Vector3 sourcePosition, float knockbackForce)
    {
        if (IsHurting) return; // 避免短时间内连续受击

        currentHealth -= amount;
        if(UIManager.instance != null)
        {
            UIManager.instance.UpdateHealthBar(currentHealth,maxHealth);
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
            // 生成受击特效 (Spawn Hit VFX)
            Instantiate(HitEffectPrefab,transform.position,Quaternion.identity);
        }

        if (currentHealth <= 0) Die();
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if(SpriteRenderer != null)
        {
            Color original = SpriteRenderer.color;
            SpriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            SpriteRenderer.color = original;
        }
    }

    void Die()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
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
        yield return new WaitForSeconds(0.2f);
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

    public void StartPlatformDrop()
    {
        StartCoroutine(DisableCollision());
    }

    IEnumerator DisableCollision()
    {
        if (currentPlatformCollider == null) yield break;

        Collider2D targetPlatform = currentPlatformCollider;
        isFallingThroughPlatform = true; 

        Physics2D.IgnoreCollision(BodyCollider, targetPlatform, true);
        
        RigidBody.velocity = new Vector2(RigidBody.velocity.x, PlatformFallSpeed);

        yield return new WaitForSeconds(platformFallTime);
        
        if (targetPlatform != null)
        {
            Physics2D.IgnoreCollision(BodyCollider, targetPlatform, false);
        }

        isFallingThroughPlatform = false; 
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

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 18;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        string info = $"<b>[DEV MODE]</b>\n" +
                      $"State: <color=yellow>{currentStateName}</color>\n" +
                      $"Vel: {RigidBody.velocity:F2}\n" +
                      $"Grounded: {CheckGrounded()}\n" +
                      $"Wall: {CheckTouchingWall()}\n" +
                      $"FPS: {(1.0f / Time.smoothDeltaTime):F0}";

        float height = 140f;
        GUI.Box(new Rect(10, 10, 200, height), info, style);
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
            if (core.InputX != 0) core.SwitchInnerState(new InnerMoveState(core));
            
            // 单向平台下落 (S + Jump)
            if (Input.GetAxisRaw("Vertical") < 0 && Input.GetButtonDown("Jump"))
            {
                core.StartPlatformDrop();
                return; // 优先处理下落，避免触发普通跳跃
            }

            // 普通跳跃
            if (Input.GetButtonDown("Jump") && core.CheckGrounded())
                core.SwitchInnerState(new InnerJumpState(core));
                
            // 攻击输入
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1")) 
                core.SwitchInnerState(new InnerAttackState(core));

            // 冲刺输入
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));
                
            // 自然下落（例如走出边缘）
            if (!core.CheckGrounded())
                core.SwitchInnerState(new InnerFallState(core));
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
            if (core.InputX == 0) core.SwitchInnerState(new InnerIdleState(core));
            
            // 单向平台下落 (S + Jump)
            if (Input.GetAxisRaw("Vertical") < 0 && Input.GetButtonDown("Jump"))
            {
                core.StartPlatformDrop();
                return;
            }

            // 跳跃
            if (Input.GetButtonDown("Jump") && core.CheckGrounded())
                core.SwitchInnerState(new InnerJumpState(core));
                
            // 攻击
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(new InnerAttackState(core));

            // 冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));

            // 下落
            if (!core.CheckGrounded())
                core.SwitchInnerState(new InnerFallState(core));
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
            if (core.RigidBody.velocity.y < 0) core.SwitchInnerState(new InnerFallState(core));
            
            // 贴墙检测 -> 切换滑墙
            if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
            {
                 core.SwitchInnerState(new InnerWallSlideState(core));
            }

            // 空中冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));
                
            // 空中攻击
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(new InnerAttackState(core));
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
                core.SwitchInnerState(new InnerIdleState(core));
            }
            
            // 贴墙检测
            if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
            {
                 core.SwitchInnerState(new InnerWallSlideState(core));
            }

            // 土狼时间 (Coyote Time)：允许离开平台一小段时间内仍可跳跃
            if (core.CoyoteTimeCounter > 0 && Input.GetButtonDown("Jump"))
                 core.SwitchInnerState(new InnerJumpState(core));

            // 空中冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));

            // 空中攻击
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(new InnerAttackState(core));
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
                core.SwitchInnerState(new InnerIdleState(core));
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
                     core.StartCoroutine(core.PlaySwingCurve());
            }
        }
        public override void LogicUpdate()
        {
            // 攻击动作结束判定
            if (Time.time >= startTime + core.swingDuration + 0.1f) 
            {
                if (core.CheckGrounded())
                    core.SwitchInnerState(new InnerIdleState(core));
                else
                    core.SwitchInnerState(new InnerFallState(core));
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
                core.SwitchInnerState(new InnerWallJumpState(core));
                return;
            }

            // 脱离墙壁判定：输入反向或不再贴墙
            bool isMovingAway = core.InputX != 0 && core.InputX != core.transform.localScale.x;
            
            if (!core.CheckTouchingWall() || core.CheckGrounded() || isMovingAway)
            {
                if(core.CheckGrounded()) core.SwitchInnerState(new InnerIdleState(core));
                else core.SwitchInnerState(new InnerFallState(core));
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
                core.SwitchInnerState(new InnerFallState(core));
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
        SwitchInnerState(new InnerIdleState(this));
    }

    public void SwitchInnerState(BaseState newState)
    {
        currentInnerState?.Exit();
        currentInnerState = newState;
        currentInnerState.Enter();
        currentStateName = newState.GetType().Name; 
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