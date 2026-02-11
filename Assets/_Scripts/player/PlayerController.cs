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

    public void TakeDamage(int amount,Vector3 sourcePosition, float knockbackForce)
    {
        if (IsHurting) return; 

        currentHealth -= amount;
        if(UIManager.instance != null)
        {
            UIManager.instance.UpdateHealthBar(currentHealth,maxHealth);
        }
        
        // Knockback Logic
        if (RigidBody != null)
        {
            StartCoroutine(KnockbackRoutine(knockbackForce));
            
            Vector2 direction = (transform.position - sourcePosition).normalized;
            Vector2 force = direction * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
            
            RigidBody.velocity = Vector2.zero;
            RigidBody.AddForce(force, ForceMode2D.Impulse);
        }

        // Visual Feedback
        StartCoroutine(FlashEffect());

        if(ImpulseSource != null)
        {
            Vector3 shakeVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0) * 0.5f;
            ImpulseSource.GenerateImpulse(shakeVelocity);
        }
        if(HitEffectPrefab != null)
        {
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
    //  Inner FSM Classes
    // ============================================================================

    public abstract class BaseState
    {
        protected PlayerController core;
        protected float startTime;

        public BaseState(PlayerController _core) => core = _core;

        public virtual void Enter() 
        { 
            startTime = Time.time;
            if (core.showDebugInfo) Debug.Log($"Enter State: {this.GetType().Name}");
        }
        public virtual void Exit() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
    }

    public class InnerIdleState : BaseState
    {
        public InnerIdleState(PlayerController core) : base(core) { }

        public override void Enter()
        {
            base.Enter();
            core.SetVelocityX(0); 
            core.RigidBody.gravityScale = core.FallingGravityScale; // Normal gravity
            if(core.Animator != null) core.Animator.CrossFade("Idle", 0.1f);
        }

        public override void LogicUpdate()
        {
            if (core.InputX != 0) core.SwitchInnerState(new InnerMoveState(core));
            
            if (Input.GetButtonDown("Jump") && core.CheckGrounded())
                core.SwitchInnerState(new InnerJumpState(core));
                
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1")) 
                core.SwitchInnerState(new InnerAttackState(core));

            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));
                
            if (!core.CheckGrounded())
                core.SwitchInnerState(new InnerFallState(core));
        }
    }

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
            core.CheckFlip();
            core.SetVelocityX(core.moveSpeed * core.InputX);

            if (core.InputX == 0) core.SwitchInnerState(new InnerIdleState(core));
            
            if (Input.GetButtonDown("Jump") && core.CheckGrounded())
                core.SwitchInnerState(new InnerJumpState(core));
                
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(new InnerAttackState(core));

            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));

            if (!core.CheckGrounded())
                core.SwitchInnerState(new InnerFallState(core));
        }
    }

    public class InnerJumpState : BaseState
    {
        public InnerJumpState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            core.SetVelocityY(core.jumpForce);
            core.RigidBody.gravityScale = core.RisingGravityScale; // Jump gravity
            if(core.Animator != null) core.Animator.CrossFade("Jump", 0.1f);
            core.SpawnDust(core.JumpDustPrefab);
        }
        public override void LogicUpdate()
        {
            core.CheckFlip();
            core.SetVelocityX(core.moveSpeed * core.InputX);

            if (core.RigidBody.velocity.y < 0) core.SwitchInnerState(new InnerFallState(core));
            
            if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
            {
                 core.SwitchInnerState(new InnerWallSlideState(core));
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));
                
            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(new InnerAttackState(core));
        }
    }

    public class InnerFallState : BaseState
    {
        public InnerFallState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            core.RigidBody.gravityScale = core.FallingGravityScale; // Fall gravity
            if(core.Animator != null) core.Animator.CrossFade("Fall", 0.1f);
        }
        public override void LogicUpdate()
        {
            core.CheckFlip();
            core.SetVelocityX(core.moveSpeed * core.InputX);

            if (core.CheckGrounded())
            {
                core.SpawnDust(core.LandDustPrefab);
                core.SwitchInnerState(new InnerIdleState(core));
            }
            
            if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
            {
                 core.SwitchInnerState(new InnerWallSlideState(core));
            }

            if (core.CoyoteTimeCounter > 0 && Input.GetButtonDown("Jump"))
                 core.SwitchInnerState(new InnerJumpState(core));

            if (Input.GetKeyDown(KeyCode.LeftShift) && core.dashCooldownTimer <= 0)
                core.SwitchInnerState(new InnerDashState(core));

            if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Fire1"))
                core.SwitchInnerState(new InnerAttackState(core));
        }
    }

    public class InnerDashState : BaseState
    {
        public InnerDashState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            if(core.Animator != null) core.Animator.CrossFade("Dash", 0f); 
            core.dashCooldownTimer = core.dashCooldown; 
            int dir = core.transform.localScale.x > 0 ? 1 : -1;
            core.SetVelocity(dir * core.dashSpeed, 0);
            core.RigidBody.gravityScale = 0; 
        }
        public override void Exit()
        {
            core.RigidBody.gravityScale = core.FallingGravityScale; 
            core.SetVelocityX(0);
        }
        public override void LogicUpdate()
        {
            if (Time.time >= startTime + core.dashTime)
                core.SwitchInnerState(new InnerIdleState(core));
        }
    }

    public class InnerAttackState : BaseState
    {
        public InnerAttackState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            if (core.CheckGrounded())
            {
                 core.SetVelocityX(0);
            }
            
            if (core.CurrentWeapon != null)
            {
                core.CurrentWeapon.Attack(core);
                if (core.CurrentWeapon.useMeleeSwing)
                     core.StartCoroutine(core.PlaySwingCurve());
            }
        }
        public override void LogicUpdate()
        {
            if (Time.time >= startTime + core.swingDuration + 0.1f) 
            {
                if (core.CheckGrounded())
                    core.SwitchInnerState(new InnerIdleState(core));
                else
                    core.SwitchInnerState(new InnerFallState(core));
            }
        }
    }

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
            if (Input.GetButtonDown("Jump"))
            {
                core.SwitchInnerState(new InnerWallJumpState(core));
                return;
            }

            bool isMovingAway = core.InputX != 0 && core.InputX != core.transform.localScale.x;
            
            if (!core.CheckTouchingWall() || core.CheckGrounded() || isMovingAway)
            {
                if(core.CheckGrounded()) core.SwitchInnerState(new InnerIdleState(core));
                else core.SwitchInnerState(new InnerFallState(core));
                return;
            }

            core.SetVelocity(core.RigidBody.velocity.x, -core.wallSlideSpeed);
        }
    }

    public class InnerWallJumpState : BaseState
    {
        public InnerWallJumpState(PlayerController core) : base(core) { }
        public override void Enter()
        {
            base.Enter();
            if(core.Animator != null) core.Animator.CrossFade("Jump", 0.1f);
            
            float jumpDir = -core.transform.localScale.x; 
            
            Vector2 force = new Vector2(core.wallJumpForce.x * jumpDir, core.wallJumpForce.y);
            core.RigidBody.velocity = Vector2.zero; 
            core.RigidBody.AddForce(force, ForceMode2D.Impulse);
            
            core.Flip();
        }
        public override void LogicUpdate()
        {
            if (Time.time >= startTime + core.wallJumpTime)
            {
                core.SwitchInnerState(new InnerFallState(core));
            }
        }
    }

    // ============================================================================
    //  Helper Methods
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