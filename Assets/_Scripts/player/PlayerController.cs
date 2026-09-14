using System.Collections.Generic;
using UnityEngine;
using XLua;

using DeadCells.Core;
using DeadCells.Combat;
using DeadCells.Save;
using UnityEngine.SceneManagement;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家控制器 —— 状态机编排器。
    /// 所有具体逻辑已委托给 PlayerInput / PlayerMovement / PlayerHealth / PlayerCombat / PlayerEffect。
    /// </summary>
    [LuaCallCSharp]
    // 输入刷新必须早于 PlayerLuaBridge 的 Lua 状态机驱动（脚本执行顺序默认随机，
    // 否则 Lua 每帧读到的是上一帧输入，跳跃/攻击延迟一帧甚至丢帧）
    [DefaultExecutionOrder(-100)]
    public class PlayerController : MonoBehaviour,ISaveable
    {
        // ==================== 组件引用（Awake时自动获取）====================
        public PlayerInput PlayerInput { get; private set; }
        public PlayerMovement PlayerMovement { get; private set; }
        public PlayerHealth PlayerHealth { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }
        public PlayerEffect PlayerVFX { get; private set; }
        public PlayerClimb PlayerClimb { get; private set; }
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

        [Tooltip("手持外观的挂载点（每把武器的外观预制体实例化到这里）。拖 WeaponHolder")]
        public Transform WeaponHolderTransform;

        [Header("Combat")]
        public List<DeadCells.Combat.WeaponData> WeaponInventory;
        public DeadCells.Combat.WeaponData CurrentWeapon;

        [Header("Debug")]
        public bool showDebugInfo = true;

        // ==================== 动画安全播放入口（Lua 侧统一走这里）====================
        // 命名规范：Animator 状态名统一用 Idle/Move/Jump/Fall/Dash/Hurt/Attack/Death
        // （恢复"零配置"：不用每角色填名字；状态不存在时静默跳过，不报 GotoState 错）

        /// <summary>
        /// 安全播放动画：Animator 或状态不存在时静默跳过（避免 GotoState 报错刷屏）。
        /// </summary>
        public void PlayAnim(string stateName, float fade = 0.1f)
        {
            if (Animator == null || string.IsNullOrEmpty(stateName)) return;
            if (!Animator.HasState(0, Animator.StringToHash(stateName))) return;
            Animator.CrossFade(stateName, fade);
        }

        // 便捷方法（Lua 状态文件用这些，动画名不硬编码在 Lua 里）
        public void PlayIdle(float fade = 0.1f) => PlayAnim("Idle", fade);
        public void PlayMove(float fade = 0.1f) => PlayAnim("Move", fade);
        public void PlayJump(float fade = 0.1f) => PlayAnim("Jump", fade);
        public void PlayFall(float fade = 0.1f) => PlayAnim("Fall", fade);
        public void PlayDash(float fade = 0f) => PlayAnim("Dash", fade);
        public void PlayHurt(float fade = 0.05f) => PlayAnim("Hurt", fade);
        public void PlayDeath(float fade = 0.05f) => PlayAnim("Death", fade);
        public void PlayAttack(string overrideName = null, float fade = 0.05f)
            => PlayAnim(string.IsNullOrEmpty(overrideName) ? "Attack" : overrideName, fade);

        // ==================== 运行时 ====================
        public float JumpBufferTimer => PlayerInput.JumpBufferTimer;
        public float CoyoteTimeCounter => PlayerInput.CoyoteTimer;

        /// <summary>
        /// 是否冲刺中（dash.lua 设置）—— 冲刺无敌帧：
        /// 敌人攻击判定会跳过冲刺中的玩家（不触发火球/冲撞），且物理穿过敌人。
        /// </summary>
        public bool IsDashing { get; set; }

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

            PlayerVFX = GetComponentInChildren<PlayerEffect>();
            PlayerClimb = GetComponent<PlayerClimb>();
            if (PlayerClimb == null) PlayerClimb = gameObject.AddComponent<PlayerClimb>();
            Animator = GetComponentInChildren<Animator>();
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // 注入位置引用（场景绑定，组件自身无法序列化）
            if (PlayerMovement != null)
            {
                PlayerMovement.groundCheckPos = GroundCheckPos;
                PlayerMovement.wallCheckPos = WallCheckPos;

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
                    int layer = LayerMask.NameToLayer("Ground");
                    if (layer >= 0) PlayerMovement.wallLayer = 1 << layer;
                }
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
                if (WeaponHolderTransform != null) PlayerCombat.weaponHolder = WeaponHolderTransform;

                // 从 PlayerController 的序列化字段注入到 PlayerCombat
                if (PlayerCombat.weaponInventory == null || PlayerCombat.weaponInventory.Count == 0)
                    PlayerCombat.weaponInventory = WeaponInventory;
                if (PlayerCombat.currentWeapon == null)
                    PlayerCombat.currentWeapon = CurrentWeapon;

            }

        }

        void Start()
        {
            if (SpriteRenderer != null) SpriteRenderer.sortingOrder = 10;
        }

        void Update()
        {
            // 输入永远刷新：受击期间 HFSM ABNORMAL 层也要读到最新输入
            if (PlayerMovement != null && PlayerInput != null)
                PlayerInput.Tick(PlayerMovement.CheckGrounded());

            // 眩晕（Boss 冲撞命中）：把输入清零，状态机照常跑但玩家动不了
            if (PlayerHealth != null && PlayerHealth.IsStunned && PlayerInput != null)
                PlayerInput.ClearOutputs();

            // 受击/死亡期间，其余逻辑由 Lua HFSM 的 ABNORMAL 层接管（死亡则完全冻结）
            if (PlayerHealth != null && (PlayerHealth.IsHurting || PlayerHealth.IsDead)) return;

            // 切换武器（攻击中禁止——防止攻击段/动画残留错乱）
            if (PlayerInput != null && PlayerInput.SwitchWeaponDown
                && (PlayerCombat == null || !PlayerCombat.IsAttacking))
                PlayerCombat?.SwitchWeapon();

            // 状态机由 PlayerLuaBridge 驱动 Lua HFSM（本组件不再管理）

            // Debug
            if (Input.GetKeyDown(KeyCode.BackQuote)) showDebugInfo = !showDebugInfo;
        }

        // ==================== 对外接口（给 FSM 状态和外部调用）====================

        public bool CheckGrounded() => PlayerMovement != null && PlayerMovement.CheckGrounded();
        public bool CheckTouchingWall() => PlayerMovement != null && PlayerMovement.CheckTouchingWall();

        /// <summary>
        /// 当前武器的攻击原点 —— 武器脚本一律用这个，不要直接用 AttackOrigin：
        /// 枪/弓这类武器把枪口做进外观预制体后，攻击点会跟着武器一起动
        /// （固定用一个 AttackPos 的话，枪一转准星就偏了）
        /// </summary>
        public Transform CurrentAttackOrigin =>
            PlayerCombat != null && PlayerCombat.CurrentAttackOrigin != null
                ? PlayerCombat.CurrentAttackOrigin
                : AttackOrigin;

        public void SetVelocityX(float x) => PlayerMovement?.SetVelocityX(x);
        public void SetVelocityY(float y) => PlayerMovement?.SetVelocityY(y);
        public void SetVelocity(float x, float y) => PlayerMovement?.SetVelocity(x, y);
        public void CheckFlip() => PlayerMovement?.CheckFlip(InputX);
        public void Flip() => PlayerMovement?.Flip();
        public void StartPlatformDrop() => PlayerMovement?.StartPlatformDrop();
        public void ConsumeJumpBuffer() => PlayerInput?.ConsumeJumpBuffer();

        /// <summary>
        /// 冲刺无敌帧：与场景中所有敌人 IgnoreCollision（穿过敌人不被卡住）。
        /// 冲刺进入时 ignore=true，退出时 ignore=false 恢复。
        /// </summary>
        public void SetDashInvincible(bool ignore)
        {
            if (BodyCollider == null) return;
            // 完全限定名：本命名空间下有 DeadCells.Enemy 命名空间，Enemy 直接写会被歧义解析
            var enemies = FindObjectsByType<DeadCells.Enemy.Enemy>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e != null && e.bodyCollider != null)
                    Physics2D.IgnoreCollision(BodyCollider, e.bodyCollider, ignore);
            }
        }

        public void SpawnDust(GameObject dustPrefab)
        {
            if (dustPrefab == null || GroundCheckPos == null) return;
            if (PoolManager.Instance == null)
            {
                Instantiate(dustPrefab, GroundCheckPos.position, Quaternion.identity);
                return;
            }
            PoolType poolType = dustPrefab == PlayerVFX?.JumpDustPrefab ? PoolType.JumpDust : PoolType.LandDust;
            GameObject dust = PoolManager.Instance.Spawn(poolType, GroundCheckPos.position, Quaternion.identity);
            if (dust != null)
                PoolManager.Instance.ReturnAfterDelay(poolType, dust, 1f);
            else
                Instantiate(dustPrefab, GroundCheckPos.position, Quaternion.identity);
        }

        public bool CanAttack() => PlayerCombat != null && PlayerCombat.CanAttack();
        public void Attack() => PlayerCombat?.Attack();
        public void SwitchWeapon() => PlayerCombat?.SwitchWeapon();

        /// <summary>停止持续攻击后摇（由攻击状态机调用，通知当前武器播结束动画等）</summary>
        public void PlayAttackRecovery()
        {
            if (PlayerCombat != null && PlayerCombat.currentWeapon != null)
                PlayerCombat.currentWeapon.PlayRecoveryAnim(this);
        }

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

        // ==================== ISaveable ====================

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
