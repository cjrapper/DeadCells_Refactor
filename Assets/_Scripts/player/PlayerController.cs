using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadCells.Core;
using DeadCells.Combat;
using DeadCells.Save;
using UnityEngine.SceneManagement;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家控制器 —— 状态机编排器。
    /// 所有具体逻辑已委托给 PlayerInput / PlayerMovement / PlayerHealth / PlayerCombat / PLayerEffect。
    /// </summary>
    public class PlayerController : MonoBehaviour,ISaveable
    {
        // ==================== 组件引用（Awake时自动获取）====================
        public PlayerInput PlayerInput { get; private set; }
        public PlayerMovement PlayerMovement { get; private set; }
        public PlayerHealth PlayerHealth { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }
        public PLayerEffect PlayerVFX { get; private set; }
        public Animator Animator { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }

        // ==================== 快捷访问（给 FSM 状态用）====================
        public Rigidbody2D RigidBody => PlayerMovement != null ? PlayerMovement.Rb : GetComponent<Rigidbody2D>();
        public Collider2D BodyCollider => PlayerMovement != null ? PlayerMovement.BodyCollider : GetComponent<Collider2D>();
        public float InputX => PlayerInput != null ? PlayerInput.InputX : 0;

        // ==================== 配置参数 ====================
        [Header("Check Transforms")]
        public Transform GroundCheckPos;
        public Transform WallCheckPos;
        public Transform AttackOrigin;
        public Transform WeaponPivotTransform;

        [Header("Combat — 保留在 PlayerController 以兼容场景序列化，Awake 注入到 PlayerCombat")]
        public List<DeadCells.Combat.WeaponData> WeaponInventory;
        public DeadCells.Combat.WeaponData CurrentWeapon;
        public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float swingDuration = 0.25f;
        public float maxSwingAngle = 120f;

        [Header("VFX — 保留以兼容场景序列化")]
        public GameObject JumpDustPrefab;
        public GameObject LandDustPrefab;

        [Header("Physics — 保留以注入 PlayerMovement")]
        public float CheckRadius = 0.3f;
        public LayerMask GroundLayer;
        public LayerMask WallLayer;
        public LayerMask OneWayPlatformLayer;
        public PhysicsMaterial2D noFrictionMaterial;

        [Header("Movement — 保留以注入 PlayerMovement")]
        public float moveSpeed = 5f;
        public float jumpForce = 2f;
        public float RisingGravityScale = 1f;
        public float FallingGravityScale = 2.5f;
        public float dashSpeed = 15f;
        public float dashTime = 0.2f;
        public float dashCooldown = 1f;
        public float wallSlideSpeed = 2f;
        public Vector2 wallJumpForce = new Vector2(10f, 12f);
        public float wallJumpTime = 0.2f;

        [Header("Debug")]
        public bool showDebugInfo = true;

        // ==================== 运行时 ====================
        public float JumpBufferTimer => PlayerInput.JumpBufferTimer;
        public float CoyoteTimeCounter => PlayerInput.CoyoteTimer;

        // ==================== FSM ====================
        private BaseState currentInnerState;
        private InnerIdleState innerIdleState;
        private InnerMoveState innerMoveState;
        private InnerJumpState innerJumpState;
        private InnerFallState innerFallState;
        private InnerDashState innerDashState;
        private InnerAttackState innerAttackState;
        private InnerWallSlideState innerWallSlideState;
        private InnerWallJumpState innerWallJumpState;

        void Awake()
        {
            // 自动获取或添加组件
            PlayerInput = GetComponent<PlayerInput>();
            PlayerMovement = GetComponent<PlayerMovement>();
            PlayerHealth = GetComponent<PlayerHealth>();
            PlayerCombat = GetComponent<PlayerCombat>();

            // 如果组件缺失，自动补齐（无需手动 Add Component）
            if (PlayerInput == null) PlayerInput = gameObject.AddComponent<PlayerInput>();
            if (PlayerMovement == null) PlayerMovement = gameObject.AddComponent<PlayerMovement>();
            if (PlayerHealth == null) PlayerHealth = gameObject.AddComponent<PlayerHealth>();
            if (PlayerCombat == null) PlayerCombat = gameObject.AddComponent<PlayerCombat>();

            PlayerVFX = GetComponentInChildren<PLayerEffect>();
            Animator = GetComponentInChildren<Animator>();
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // 注入地面/墙壁检测参数
            if (PlayerMovement != null)
            {
                PlayerMovement.groundCheckPos = GroundCheckPos;
                PlayerMovement.wallCheckPos = WallCheckPos;
                PlayerMovement.groundLayer = GroundLayer;
                PlayerMovement.wallLayer = WallLayer;
                PlayerMovement.oneWayPlatformLayer = OneWayPlatformLayer;

                // 兜底：如果场景序列化丢失（值为 0），按层名字查找
                if (PlayerMovement.groundLayer.value == 0)
                {
                    int layer = LayerMask.NameToLayer("Ground");
                    if (layer >= 0) PlayerMovement.groundLayer = 1 << layer;
                }
                if (PlayerMovement.oneWayPlatformLayer.value == 0)
                {
                    int layer = LayerMask.NameToLayer("OneWayPlatform");
                    if (layer >= 0) PlayerMovement.oneWayPlatformLayer = 1 << layer;
                }
                if (PlayerMovement.wallLayer.value == 0)
                {
                    int layer = LayerMask.NameToLayer("Ground"); // 项目中 Wall ≈ Ground
                    if (layer >= 0) PlayerMovement.wallLayer = 1 << layer;
                }
                PlayerMovement.checkRadius = CheckRadius;
                PlayerMovement.noFrictionMaterial = noFrictionMaterial;


                // 注入移动参数
                PlayerMovement.moveSpeed = moveSpeed;
                PlayerMovement.jumpForce = jumpForce;
                PlayerMovement.risingGravityScale = RisingGravityScale;
                PlayerMovement.fallingGravityScale = FallingGravityScale;
                PlayerMovement.dashSpeed = dashSpeed;
                PlayerMovement.dashTime = dashTime;
                PlayerMovement.dashCooldown = dashCooldown;
                PlayerMovement.wallSlideSpeed = wallSlideSpeed;
                PlayerMovement.wallJumpForce = wallJumpForce;
                PlayerMovement.wallJumpTime = wallJumpTime;
            }

            // Ghost Pool 注入
            if (PlayerVFX != null && PoolManager.Instance != null)
            {
                PlayerVFX.AssignPool(PoolManager.Instance.GetPool(PoolType.Ghost));
            }

            // 武器轴心 + 注入战斗配置（保留场景序列化）
            if (PlayerCombat != null)
            {
                PlayerCombat.attackOrigin = AttackOrigin;
                PlayerCombat.weaponPivot = WeaponPivotTransform != null ? WeaponPivotTransform : AttackOrigin;

                // 从 PlayerController 的序列化字段注入到 PlayerCombat
                if (PlayerCombat.weaponInventory == null || PlayerCombat.weaponInventory.Count == 0)
                    PlayerCombat.weaponInventory = WeaponInventory;
                if (PlayerCombat.currentWeapon == null)
                    PlayerCombat.currentWeapon = CurrentWeapon;
                if (swingCurve != null && PlayerCombat.swingCurve.keys.Length == 0)
                    PlayerCombat.swingCurve = swingCurve;
                if (PlayerCombat.swingDuration <= 0)
                    PlayerCombat.swingDuration = swingDuration;
                if (PlayerCombat.maxSwingAngle <= 0)
                    PlayerCombat.maxSwingAngle = maxSwingAngle;
            }

            // 注入 VFX 预制体到 PLayerEffect
            if (PlayerVFX != null)
            {
                if (PlayerVFX.JumpDustPrefab == null) PlayerVFX.JumpDustPrefab = JumpDustPrefab;
                if (PlayerVFX.LandDustPrefab == null) PlayerVFX.LandDustPrefab = LandDustPrefab;
            }

            // 预创建状态实例
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
            if (PlayerHealth != null && PlayerHealth.IsHurting) return;

            // 1. 输入
            if (PlayerMovement == null || PlayerInput == null) return;
            bool grounded = PlayerMovement.CheckGrounded();
            PlayerInput.Tick(grounded);


            // 2. 切换武器
            if (PlayerInput != null && PlayerInput.SwitchWeaponDown)
                PlayerCombat?.SwitchWeapon();

            // 3. 状态机
            RunInnerFSM();

            // 4. Debug
            if (Input.GetKeyDown(KeyCode.BackQuote)) showDebugInfo = !showDebugInfo;
        }

        void FixedUpdate()
        {
            if (PlayerHealth != null && PlayerHealth.IsHurting) return;
            RunInnerPhysics();
        }

        // ==================== 对外接口（给 FSM 状态和外部调用）====================

        public bool CheckGrounded() => PlayerMovement != null && PlayerMovement.CheckGrounded();
        public bool CheckTouchingWall() => PlayerMovement != null && PlayerMovement.CheckTouchingWall();

        public void SetVelocityX(float x) => PlayerMovement?.SetVelocityX(x);
        public void SetVelocityY(float y) => PlayerMovement?.SetVelocityY(y);
        public void SetVelocity(float x, float y) => PlayerMovement?.SetVelocity(x, y);
        public void CheckFlip() => PlayerMovement?.CheckFlip(InputX);
        public void Flip() => PlayerMovement?.Flip();
        public void StartPlatformDrop() => PlayerMovement?.StartPlatformDrop();

        public void SpawnDust(GameObject dustPrefab)
        {
            if (dustPrefab == null || GroundCheckPos == null) return;
            if (PoolManager.Instance == null)
            {
                Instantiate(dustPrefab, GroundCheckPos.position, Quaternion.identity);
                return;
            }
            PoolType poolType = dustPrefab == JumpDustPrefab ? PoolType.JumpDust : PoolType.LandDust;
            GameObject dust = PoolManager.Instance.Spawn(poolType, GroundCheckPos.position, Quaternion.identity);
            if (dust != null)
                PoolManager.Instance.ReturnAfterDelay(poolType, dust, 1f);
            else
                Instantiate(dustPrefab, GroundCheckPos.position, Quaternion.identity);
        }

        public bool CanAttack() => PlayerCombat != null && PlayerCombat.CanAttack();
        public void Attack() => PlayerCombat?.Attack();
        public void SwitchWeapon() => PlayerCombat?.SwitchWeapon();

        // ==================== Gizmos ====================

        void OnDrawGizmos()
        {
            if (GroundCheckPos != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(GroundCheckPos.position, 0.3f);
            }
            if (AttackOrigin != null && PlayerCombat != null && PlayerCombat.currentWeapon != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(AttackOrigin.position, PlayerCombat.currentWeapon.attackRange);
            }
        }

        // ==================== 内部 FSM ====================

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
                if (core.PlayerMovement != null)
                    core.RigidBody.gravityScale = core.PlayerMovement.fallingGravityScale;
                core.Animator?.CrossFade("Idle", 0.1f);
            }
            public override void LogicUpdate()
            {
                if (core.InputX != 0) { core.SwitchInnerState(core.innerMoveState); return; }
                if (core.PlayerInput.VerticalInput < 0 && core.PlayerInput.JumpDown)
                { core.StartPlatformDrop(); return; }
                if (core.PlayerInput.JumpDown && core.CheckGrounded())
                {
                    core.SwitchInnerState(core.innerJumpState); return;
                }
                if (core.PlayerInput.AttackDown)
                { core.SwitchInnerState(core.innerAttackState); return; }
                if (core.PlayerInput.DashDown && core.PlayerMovement.DashCooldownTimer <= 0)
                { core.SwitchInnerState(core.innerDashState); return; }
                if (!core.CheckGrounded())
                { core.SwitchInnerState(core.innerFallState); }
            }
        }

        public class InnerMoveState : BaseState
        {
            public InnerMoveState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                if (core.PlayerMovement != null)
                    core.RigidBody.gravityScale = core.PlayerMovement.fallingGravityScale;
                core.Animator?.CrossFade("Move", 0.1f);
            }
            public override void LogicUpdate()
            {
                core.CheckFlip();
                core.SetVelocityX(core.PlayerMovement.moveSpeed * core.InputX);
                if (core.InputX == 0) { core.SwitchInnerState(core.innerIdleState); return; }
                if (core.PlayerInput.VerticalInput < 0 && core.PlayerInput.JumpDown)
                { core.StartPlatformDrop(); return; }
                if (core.PlayerInput.JumpDown && core.CheckGrounded())
                { core.SwitchInnerState(core.innerJumpState); return; }
                if (core.PlayerInput.AttackDown)
                { core.SwitchInnerState(core.innerAttackState); return; }
                if (core.PlayerInput.DashDown && core.PlayerMovement.DashCooldownTimer <= 0)
                { core.SwitchInnerState(core.innerDashState); return; }
                if (!core.CheckGrounded())
                { core.SwitchInnerState(core.innerFallState); }
            }
        }

        public class InnerJumpState : BaseState
        {
            public InnerJumpState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                core.SetVelocityY(core.PlayerMovement.jumpForce);
                if (core.PlayerMovement != null)
                    core.RigidBody.gravityScale = core.PlayerMovement.risingGravityScale;
                core.Animator?.CrossFade("Jump", 0.1f);
                core.SpawnDust(core.PlayerVFX?.JumpDustPrefab);
            }
            public override void LogicUpdate()
            {
                core.CheckFlip();
                core.SetVelocityX(core.PlayerMovement.moveSpeed * core.InputX);
                if (core.RigidBody.velocity.y < 0)
                { core.SwitchInnerState(core.innerFallState); return; }
                if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
                { core.SwitchInnerState(core.innerWallSlideState); return; }
                if (core.PlayerInput.DashDown && core.PlayerMovement.DashCooldownTimer <= 0)
                { core.SwitchInnerState(core.innerDashState); }
                if (core.PlayerInput.AttackDown)
                { core.SwitchInnerState(core.innerAttackState); }
            }
        }

        public class InnerFallState : BaseState
        {
            public InnerFallState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                if (core.PlayerMovement != null)
                    core.RigidBody.gravityScale = core.PlayerMovement.fallingGravityScale;
                core.Animator?.CrossFade("Fall", 0.1f);
            }
            public override void LogicUpdate()
            {
                core.CheckFlip();
                core.SetVelocityX(core.PlayerMovement.moveSpeed * core.InputX);
                if (core.CheckGrounded())
                {
                    core.SpawnDust(core.PlayerVFX?.LandDustPrefab);
                    core.SwitchInnerState(core.innerIdleState);
                    return;
                }
                if (core.CheckTouchingWall() && core.InputX == core.transform.localScale.x)
                { core.SwitchInnerState(core.innerWallSlideState); return; }
                if (core.CoyoteTimeCounter > 0 && core.PlayerInput.JumpDown)
                { core.SwitchInnerState(core.innerJumpState); return; }
                if (core.PlayerInput.DashDown && core.PlayerMovement.DashCooldownTimer <= 0)
                { core.SwitchInnerState(core.innerDashState); }
                if (core.PlayerInput.AttackDown)
                { core.SwitchInnerState(core.innerAttackState); }
            }
        }

        public class InnerDashState : BaseState
        {
            public InnerDashState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                core.Animator?.CrossFade("Dash", 0f);
                core.PlayerMovement.DashCooldownTimer = core.PlayerMovement.dashCooldown;
                int dir = core.transform.localScale.x > 0 ? 1 : -1;
                core.SetVelocity(dir * core.PlayerMovement.dashSpeed, 0);
                core.RigidBody.gravityScale = 0;
            }
            public override void Exit()
            {
                if (core.PlayerMovement != null)
                    core.RigidBody.gravityScale = core.PlayerMovement.fallingGravityScale;
                core.SetVelocityX(0);
            }
            public override void LogicUpdate()
            {
                if (Time.time >= startTime + core.PlayerMovement.dashTime)
                    core.SwitchInnerState(core.innerIdleState);
            }
        }

        public class InnerAttackState : BaseState
        {
            public InnerAttackState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                if (core.CheckGrounded()) core.SetVelocityX(0);
                core.Attack();
            }
            public override void LogicUpdate()
            {
                if (Time.time >= startTime + core.PlayerCombat.swingDuration + 0.1f)
                {
                    core.SwitchInnerState(core.CheckGrounded() ? core.innerIdleState : core.innerFallState);
                }
            }
        }

        public class InnerWallSlideState : BaseState
        {
            public InnerWallSlideState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                core.Animator?.CrossFade("Fall", 0.1f);
            }
            public override void LogicUpdate()
            {
                if (core.PlayerInput.JumpDown)
                { core.SwitchInnerState(core.innerWallJumpState); return; }
                bool movingAway = core.InputX != 0 && core.InputX != core.transform.localScale.x;
                if (!core.CheckTouchingWall() || core.CheckGrounded() || movingAway)
                {
                    core.SwitchInnerState(core.CheckGrounded() ? core.innerIdleState : core.innerFallState);
                    return;
                }
                core.SetVelocity(core.RigidBody.velocity.x, -core.PlayerMovement.wallSlideSpeed);
            }
        }

        public class InnerWallJumpState : BaseState
        {
            public InnerWallJumpState(PlayerController core) : base(core) { }
            public override void Enter()
            {
                base.Enter();
                core.Animator?.CrossFade("Jump", 0.1f);
                float jumpDir = -core.transform.localScale.x;
                core.RigidBody.velocity = Vector2.zero;
                core.RigidBody.AddForce(new Vector2(
                    core.PlayerMovement.wallJumpForce.x * jumpDir,
                    core.PlayerMovement.wallJumpForce.y), ForceMode2D.Impulse);
                core.Flip();
            }
            public override void LogicUpdate()
            {
                if (Time.time >= startTime + core.PlayerMovement.wallJumpTime)
                    core.SwitchInnerState(core.innerFallState);
            }
        }

        // ==================== FSM 控制器 ====================

        public void InitializeInnerFSM() => SwitchInnerState(innerIdleState);

        public void SwitchInnerState(BaseState newState)
        {
            currentInnerState?.Exit();
            currentInnerState = newState;
            currentInnerState.Enter();
        }

        public void RunInnerFSM()
        {
            if (PlayerMovement != null && PlayerMovement.DashCooldownTimer > 0)
                PlayerMovement.DashCooldownTimer -= Time.deltaTime;
            currentInnerState?.LogicUpdate();
        }

        public void RunInnerPhysics()
        {
            currentInnerState?.PhysicsUpdate();
        }

        public void OnSave(SaveData data)
        {
            data.posX = transform.position.x;
            data.posY = transform.position.y;
            data.posZ = transform.position.z;
            data.sceneName = SceneManager.GetActiveScene().name;
        }

        public void OnLoad(SaveData data)
        {
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
        }
    }
}
