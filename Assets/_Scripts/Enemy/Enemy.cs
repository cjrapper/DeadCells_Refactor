using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using XLua;

using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Player;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace DeadCells.Enemy
{
    [LuaCallCSharp]
    public abstract class Enemy : MonoBehaviour, IDamageable
    {
        public Rigidbody2D rb { get; private set; }
        public SpriteRenderer sr { get; private set; }
        public Collider2D bodyCollider { get; private set; }
        public Animator Animator { get; private set; }
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

        /// <summary>当前血量比例 0~1（血条用；currentHealth 是 protected，外部读不到）</summary>
        public float HealthRatio => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

        /// <summary>是否满血（血条默认满血不显示）</summary>
        public bool IsFullHealth => currentHealth >= maxHealth;

        /// <summary>是否正在播死亡流程（血条要隐藏）</summary>
        public bool IsDyingNow => isDying;

        // ==================== 移动钩子（子类可覆写）====================

        /// <summary>追击/巡逻时"想停在哪里"：0 表示用 attackRange</summary>
        public float StopDistance => stopDistance > 0.01f ? stopDistance : attackRange;

        /// <summary>玩家太近时是否主动后退（0 = 不后退）</summary>
        public bool HasMinDistance => minDistance > 0.01f;

        /// <summary>
        /// 水平移动。默认是"持续给速度"，子类可覆写换移动方式
        /// （比如史莱姆覆写成"跳着走"）。
        /// 状态机只负责决策"往哪走、走多快"，具体怎么动交给敌人自己 —— 加新怪不用改状态机。
        /// </summary>
        public virtual void MoveHorizontal(float dir, float speed)
        {
            if (rb != null) rb.velocity = new Vector2(dir * speed, rb.velocity.y);
        }

        /// <summary>水平刹停（进入攻击距离时用）</summary>
        public virtual void StopHorizontal()
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        /// <summary>
        /// 蓄力过程中每帧回调（progress: 0 → 1）。
        /// 子类用它做"起跳前压扁""身体发红"这类蓄力预备动作 —— 蓄力可读性是动作游戏的关键。
        /// </summary>
        public virtual void OnTelegraphTick(float progress) { }

        /// <summary>蓄力结束（进入攻击或被打断）</summary>
        public virtual void OnTelegraphEnd() { }

        /// <summary>
        /// 现在能不能开始蓄力。默认能。
        /// 子类覆写可以做限制：史莱姆要求"必须落地才能蓄力"，
        /// 否则会在半空中开始蓄力、边飘边扑。
        /// </summary>
        public virtual bool CanStartAttack() => true;

        [Tooltip("头顶血条。不想让杂兵显示就取消勾选")]
        public bool showHealthBar = true;

        [Header("脱战回血")]
        [Tooltip("开启后：一段时间没被打、且看不见玩家 → 缓慢回血（避免残血怪永远残着）")]
        public bool enableRegen = true;
        [Tooltip("脱战多久后开始回血（秒）")]
        public float regenDelay = 3f;
        [Tooltip("每秒回多少血")]
        public float regenPerSecond = 6f;

        private float lastDamagedTime = -999f;
        private float regenAccumulator;

        [Header("Vision Settings")]
        public float visionHeight = 3f;
        [Tooltip("遮挡视线的层（墙/地面/平台）。敌人看不到被遮挡的玩家，不追不打")]
        public LayerMask visionBlockLayer;

        [Header("Attack Settings")]
        public float attackRange = 1f;

        [Tooltip("追击到这个距离就停下等攻击（给冲撞留助跑距离，别贴到玩家脸上）。\n" +
                 "0 = 用 attackRange")]
        public float stopDistance = 2f;

        [Tooltip("玩家贴得比这个距离还近 → 主动后退，保持出手距离。\n" +
                 "0 = 不后退（会黏在玩家身上）。应当 < stopDistance\n" +
                 "★ 名字不能叫 keepDistance：FlyingEnemy 已经有同名字段，基类子类同名字段 Unity 不支持")]
        public float minDistance = 0f;
        public int damage = 1;
        public float windupTime = 0.3f; // 攻击预警时间
        public float attackDuration = 0.2f; // 攻击动作持续时间
        public float attackCooldown = 1.5f;
        public float lungeSpeed = 8f; // 冲刺速度
        public LayerMask playerLayer;
        public Transform attackPos;
        public GameObject alertSign;

        [Header("Laser Attack（远程）")]
        [Tooltip("玩家进入该距离（且近战范围外）时切激光攻击")]
        public float laserTriggerRange = 6f;
        [Tooltip("激光长度（短激光）")]
        public float laserRange = 3f;
        public int laserDamage = 3;
        public LaserAttackState laserAttackState { get; private set; }
        
        [FormerlySerializedAs("LootTable")] [Header("Loot")]
        public LootTable lootTable;

        private void SpawnLoot(GameObject prefab, Vector3 pos)
        {
            SamplePool pool = PoolManager.Instance?.GetPool(PoolType.Coin);
            if (pool)
            {
                GameObject go = pool.Get();
                if (go)
                {
                    go.transform.position = pos;
                    var pickup = go.GetComponent<CoinPickup>();
                    if(pickup)pickup.AssignPool(pool);
                    return;
                }
            }
            Instantiate(prefab,pos,Quaternion.identity);
        }

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
            Animator = GetComponentInChildren<Animator>();
            currentHealth = maxHealth;
            if (alertSign != null) alertSign.SetActive(false);

            // playerLayer 兜底：Inspector 忘配（默认 Nothing）时自动选玩家层，
            // 否则攻击判定 (playerLayer.value & ...) == 0 永远打不到（新手高频坑）。
            // 注意：本项目玩家层名是 "playerLayer"（小写 p），不是 "Player"
            if (playerLayer.value == 0)
            {
                int layer = LayerMask.NameToLayer("playerLayer");
                if (layer >= 0) playerLayer = 1 << layer;
            }

            // 视线遮挡层兜底：默认 Ground（墙/地面挡住就不追）
            if (visionBlockLayer.value == 0)
            {
                int layer = LayerMask.NameToLayer("Ground");
                if (layer >= 0) visionBlockLayer = 1 << layer;
            }

            StateMachine = new EnemyStateMachine(this);
            patrolState = new PatrolState(this, StateMachine);
            chaseState = new ChaseState(this, StateMachine);
            attackState = new AttackState(this, StateMachine);
            hurtState = new HurtState(this, StateMachine);
            telegraphState = new TelegraphState(this, StateMachine);
            laserAttackState = new LaserAttackState(this, StateMachine);

            // 头顶血条：没手动挂就自动补一个（所有敌人统一有，不用逐个预制体去配）
            if (showHealthBar && GetComponent<EnemyHealthBar>() == null)
                gameObject.AddComponent<EnemyHealthBar>();
        }

        protected virtual void Start()
        {
            // 防御：player 引用无效（null / 指向自己 / 指向已销毁对象）时自动查找。
            // 踩坑记录：预制体里 player 曾被误拖成"自己"的 Transform → 追击方向为零向量 → 敌人原地不动
            if (player == null || player == transform)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }
            startPos = transform.position;
            StateMachine.Initialize(patrolState);
        }

        protected virtual void Update()
        {
            if (isDying) return;   // 死亡动画期间不再驱动状态机

            TickHealthRegen();

            if (!useLuaFSM)
                StateMachine.CurrentState.LogicUpdate();
        }

        /// <summary>
        /// 脱战回血：一段时间没被打、并且看不见玩家 → 按 regenPerSecond 慢慢回。
        /// 小数部分先攒着（regenAccumulator），攒够 1 点才真的加 —— 血量是 int，
        /// 每帧直接加会被截断成 0 永远不回血。
        /// </summary>
        private void TickHealthRegen()
        {
            if (!enableRegen || currentHealth <= 0 || currentHealth >= maxHealth) return;

            bool inCombat = Time.time - lastDamagedTime < regenDelay;
            if (!inCombat && player != null && CanSeePlayer()) inCombat = true;   // 还在视野里 = 还在战斗

            if (inCombat)
            {
                regenAccumulator = 0f;
                return;
            }

            regenAccumulator += regenPerSecond * Time.deltaTime;
            if (regenAccumulator < 1f) return;

            int heal = (int)regenAccumulator;
            regenAccumulator -= heal;
            currentHealth = Mathf.Min(maxHealth, currentHealth + heal);
            // 血条每帧读 HealthRatio 自己会刷新，这里不用通知
        }

        protected virtual void FixedUpdate()
        {
            if (isDying) return;
            if (!useLuaFSM)
                StateMachine.CurrentState.PhysicsUpdate();
        }

        // 视觉更新虚方法，子类重写以实现各自的动画表现
        public virtual void UpdateVisuals() { }

        /// <summary>
        /// 受击结束后回到哪个状态（虚方法——框架扩展点）：
        /// 地面敌人回 chaseState；飞行敌人覆写为 flyingFlight。
        /// </summary>
        public virtual EnemyState GetPostHurtState()
        {
            return player != null ? (EnemyState)chaseState : (EnemyState)patrolState;
        }

        /// <summary>
        /// 攻击结束后回到哪个状态（虚方法——框架扩展点）：
        /// 地面敌人回 chaseState；飞行敌人覆写为 flyingFlight
        /// （否则飞行怪攻击完会掉进"地面追击"状态，行为错乱）
        /// </summary>
        public virtual EnemyState GetCombatReturnState()
        {
            return player != null ? (EnemyState)chaseState : (EnemyState)patrolState;
        }

        // ==================== 对外接口 ====================

        /// <summary>
        /// 播放状态动画（状态机切换时自动调用）：
        /// 先检查 Animator 里是否存在该状态——避免 "GotoState: State could not be found" 刷屏报错
        /// （动画状态还没配好时静默跳过）
        /// </summary>
        public void PlayStateAnim(string stateKey)
        {
            if (Animator == null || string.IsNullOrEmpty(stateKey)) return;

            // ① 逻辑键 → 实际动画名（配置优先；没配 config 就按状态类给的默认名）
            string stateName = animConfig != null ? animConfig.Resolve(stateKey) : stateKey;

            if (Animator.HasState(0, Animator.StringToHash(stateName)))
            {
                Animator.CrossFade(stateName, 0.1f);
                return;
            }

            // ② 该状态没配 → 回退动画（避免残留上一个动画，比如"攻击时还在播受击"）
            string fallbackName = animConfig != null && !string.IsNullOrEmpty(fallbackAnimName)
                ? animConfig.Resolve(fallbackAnimName)
                : fallbackAnimName;
            bool usedFallback = !string.IsNullOrEmpty(fallbackName)
                && Animator.HasState(0, Animator.StringToHash(fallbackName));
            if (usedFallback)
                Animator.CrossFade(fallbackName, 0.1f);

            // ③ 状态和回退都没有 → 什么都不播，Animator 会停在上一段动画上。
            //    这是"敌人卡在受击/攻击动画里不出来"最常见的原因，而以前是完全静默的，极难查。
            //    同一个名字只警告一次，避免状态机反复切换时刷屏。
            WarnMissingAnim(stateName, usedFallback);
        }

        private readonly HashSet<string> missingAnimWarned = new HashSet<string>();

        private void WarnMissingAnim(string stateName, bool usedFallback)
        {
            if (!missingAnimWarned.Add(stateName)) return;   // 这个状态名已经警告过了

            if (usedFallback)
            {
                Debug.LogWarning($"[Enemy] {name} 的 Animator 里没有状态 \"{stateName}\"，已回退到 " +
                                 $"\"{fallbackAnimName}\"。想让它播专属动画就在 Animator 里加 \"{stateName}\"");
            }
            else
            {
                Debug.LogWarning($"[Enemy] {name} 的 Animator 里没有状态 \"{stateName}\"，也没配回退动画 → " +
                                 $"动画会一直停在上一段（表现：卡在受击/攻击动作里出不来）。" +
                                 $"请在 Animator 里加 \"{stateName}\"，或把 Fallback Anim Name 配成一个存在的状态");
            }
        }

        public bool CanAttack()
        {
            return Time.time >= nextAttackTime;
        }

        public void RegisterAttack()
        {
            nextAttackTime = Time.time + attackCooldown;
        }

        /// <summary>
        /// 是否能看见玩家（距离 + 高度 + 视线遮挡）。virtual：子类可放宽（如飞行怪）。
        /// </summary>
        public virtual bool CanSeePlayer()
        {
            if (player == null) return false;
            Vector2 from = transform.position;
            Vector2 to = player.position;
            float dist = Vector2.Distance(from, to);

            // ① 距离 + 高度差（原有筛选）
            if (dist > chaseRange) return false;
            if (Mathf.Abs(from.y - to.y) > visionHeight) return false;

            // ② 伪视觉：障碍物遮挡（墙/平台挡住视线 = 看不见，不追击不攻击）
            //    解决"玩家站平台上方、敌人在脚下时一直卡住傻追"的问题
            //    注意：Raycast 只用 visionBlockLayer 层，玩家/敌人自身不挡视线
            if (dist > 0.1f && visionBlockLayer.value != 0)
            {
                Vector2 dir = (to - from).normalized;
                RaycastHit2D hit = Physics2D.Raycast(from, dir, dist, visionBlockLayer);
                if (hit) return false;   // 被障碍物挡住了
            }
            return true;
        }

        // 敌人受击逻辑
        public virtual void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
        {
            if (currentHealth <= 0) return;
            currentHealth -= amount;
            lastDamagedTime = Time.time;   // 脱战回血计时重新开始

            // 受击视觉反馈 (闪红)
            StartCoroutine(FlashRed());

            // 切换到受击状态或死亡
            if (currentHealth > 0)
            {
                if (useLuaFSM)
                {
                    // Lua 接管时由 Lua 状态机处理硬直
                    GetComponent<EnemyLuaBridge>()?.ChangeState("hurt");
                }
                else if (StateMachine.CurrentState != hurtState)
                {
                    // 已在受击硬直中再次挨打 → 不重复 ChangeState（否则受击动画反复从头播）
                    StateMachine.ChangeState(hurtState); // 打断当前动作进入硬直
                }
            }
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

        [Header("Death")]
        [Tooltip("死亡动画时长（播完再销毁）")]
        public float deathAnimDelay = 0.8f;
        protected bool isDying;

        [Header("Boss")]
        [Tooltip("勾选后死亡广播 BossDefeated（通关结算）——任意敌人都能当 Boss（含飞行 Boss）")]
        public bool isBoss = false;

        [Header("Animation")]
        [Tooltip("动画名配置（可选）：统一管理动画状态名——多怪物/换皮共用代码，只换配置")]
        public CharacterAnimConfig animConfig;
        [Tooltip("状态动画名在 Animator 里不存在时的回退动画（如 \"FlyIdle\"）——" +
                 "留空则保持当前动画；配一个可避免\"攻击时残留受击动画\"的观感问题")]
        public string fallbackAnimName = "";

        protected virtual void Die()
        {
            if (isDying) return;
            isDying = true;

            // 死亡停止一切行为
            if (rb != null) rb.velocity = Vector2.zero;

            // 掉落（数据驱动，原有逻辑保留）
            if (lootTable)
            {
                var entry = lootTable.Roll();
                if (entry!=null && entry.itemPrefab!=null)
                {
                    int count =Random.Range(entry.minAmount,entry.maxAmount+1);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.2f,0.6f),0f);
                        SpawnLoot(entry.itemPrefab, transform.position + offset);
                    }
                }
            }

            // 死亡动画：播 "Death"，播完再销毁（未挂 Animator 直接结算）
            Feedback.SoundAt(SfxId.EnemyDeath, transform.position);
            PlayStateAnim("Death");
            if (Animator != null)
                StartCoroutine(DestroyAfterDeath());
            else
                FinishDeath();
        }

        private System.Collections.IEnumerator DestroyAfterDeath()
        {
            yield return new WaitForSeconds(deathAnimDelay);
            FinishDeath();
        }

        /// <summary>
        /// 死亡收尾：Boss 在这里才广播通关（"先播完死亡动画，再结束游戏"），然后销毁
        /// </summary>
        private void FinishDeath()
        {
            if (isBoss)
            {
                Debug.Log("[Enemy] Boss 死亡 → 广播 BossDefeated（UI 应弹出胜利面板）");
                EventCenter.Instance?.Broadcast(EventCenter.EventType.BossDefeated.ToString());
            }

            Destroy(gameObject);
        }

        // ==================== 调试：战斗范围显示见 Assets/Editor/RangeDebugOverlay.cs ====================
        // 按住 F1（或菜单 Tools/战斗范围显示）在 Scene 视图画出所有敌人 + 玩家的范围。
        // 绘制放在编辑器脚本里而不是这里的 OnDrawGizmos —— 原因见 RangeDebug.cs 的注释。

        public void CheckFlip(float xDir)        {
            if (Mathf.Abs(xDir) < 0.1f) return;
            Vector3 scale = transform.localScale;
            scale.x = xDir > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
