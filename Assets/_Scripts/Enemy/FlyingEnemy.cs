using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 飞行敌人 / 飞行 Boss —— 不落地（gravityScale=0）。
    /// 状态：idle / flight / fireball / dive（下坠技能）/ attack（冲撞复用）。
    /// hurt / death 复用基类（闪红、硬直、死亡掉落；勾 isBoss 则死亡广播通关事件）。
    /// </summary>
    public class FlyingEnemy : Enemy
    {
        [Header("Flying Settings")]
        [Tooltip("玩家进入该距离 → 从 idle 切 flight（发现距离）")]
        public float idleRange = 8f;
        [Tooltip("玩家超过该距离才放弃追击（丢失距离 > 发现距离 = 双阈值滞回，防边界反复切换抖动）")]
        public float loseRange = 12f;
        [Tooltip("追踪时保持的距离：太近减速、太远加速")]
        public float keepDistance = 2f;
        [Tooltip("离出生点超过这个距离就飞回去（丢了玩家之后不会滞留在天边）")]
        public float returnHomeDistance = 3f;

        [Header("Altitude（高度分层：Boss 的核心节奏）")]
        [Tooltip("巡逻时保持的悬浮高度（相对出生点）。Boss 大多数时间应该待在高空")]
        public float hoverHeight = 3f;
        [Tooltip("追击时保持在玩家上方多高。0 = 直接朝玩家飞（会贴脸）")]
        public float chaseHeight = 3.5f;
        [Tooltip("下坠结束后在低空硬直多久（秒）—— 这就是玩家的偷伤害窗口")]
        public float diveRecovery = 1.6f;
        [Tooltip("下坠【命中】后的低空硬直（命中时短一些，别惩罚成功的一方）")]
        public float diveRecoveryOnHit = 0.8f;

        [Header("Dive 眩晕（命中惩罚）")]
        [Tooltip("冲撞命中时给玩家施加的眩晕时长（秒）。0 = 不眩晕")]
        public float diveStunDuration = 1.2f;
        [Tooltip("待机悬浮幅度（正弦，由 idle 状态的 velocity 驱动）")]
        public float floatAmplitude = 0.3f;
        public float floatSpeed = 2f;
        [Tooltip("贴图默认朝向是否朝左：勾上则翻转逻辑取反（修正飞行朝向与贴图不吻合）")]
        public bool spriteFacesLeft = false;
        [Tooltip("是否启用障碍物遮挡视野（飞行怪在空中一般用不到，需要时勾上）")]
        public bool useVisionBlock = false;

        [Header("Fireball")]
        [Tooltip("火球触发距离（中远距离）")]
        public float fireballRange = 8f;
        [Tooltip("火球冷却（秒）——独立于冲撞冷却；测试期可用 1.5，正式调 3+")]
        public float fireballCooldown = 1.5f;
        [Tooltip("火球预制体（池空时兜底实例化用）")]
        public GameObject fireballPrefab;
        [Tooltip("火球贴图朝向修正（度）：贴图不是朝右时用，如 90 / -90 / 180")]
        public float fireballAngleOffset = 0f;
        private float nextFireballTime;

        [Header("Dive（Boss 下坠技能）")]
        [Tooltip("与玩家距离超过该值时触发下坠技能")]
        public float diveTriggerRange = 4f;
        [Tooltip("下坠前摇（秒）——期间地面显示预警红圈")]
        public float diveWindup = 2f;
        [Tooltip("下坠速度")]
        public float diveSpeed = 22f;
        [Tooltip("落地命中半径")]
        public float diveHitRadius = 1.2f;
        [Tooltip("落地伤害")]
        public int diveDamage = 15;
        [Tooltip("预警红圈预制体（可选；没配则只走逻辑无视觉）")]
        public GameObject warningCirclePrefab;

        // 专属状态（仿照基类 patrolState 的持有方式）
        public FlyingIdleState flyingIdle { get; private set; }
        public FlyingFlightState flyingFlight { get; private set; }
        public FlyingFireballState flyingFireball { get; private set; }
        public FlyingDiveState flyingDive { get; private set; }
        public FlyingRecoverState flyingRecover { get; private set; }

        /// <summary>
        /// 上一次下坠有没有打中玩家。
        /// 用来决定低空硬直时长：打中了短硬直（不惩罚成功的一方），
        /// 打空了长硬直（把失误暴露给玩家，形成"偷伤害"窗口）
        /// </summary>
        public bool LastDiveHit { get; set; }

        protected override void Awake()
        {
            base.Awake();
            // 飞行敌人：无重力（关键！base.Awake 之后才能拿到 rb）
            if (rb != null) rb.gravityScale = 0f;
        }

        protected override void Start()
        {
            base.Start();   // 会找玩家 + Initialize(patrolState)（基类默认）

            // 飞行专属状态初始化 + 切到飞行初始状态
            flyingIdle = new FlyingIdleState(this, StateMachine);
            flyingFlight = new FlyingFlightState(this, StateMachine);
            flyingFireball = new FlyingFireballState(this, StateMachine);
            flyingDive = new FlyingDiveState(this, StateMachine);
            flyingRecover = new FlyingRecoverState(this, StateMachine);
            StateMachine.Initialize(flyingIdle);
        }

        public override void UpdateVisuals()
        {
            // 翻转：按"玩家相对方向"翻转（死区防抖）——而不是 velocity.x
            // （velocity 在保持距离/追近时 x 会微正微负来回抖 → 原实现导致抽搐）
            if (player != null)
            {
                float sx = player.position.x - transform.position.x;
                if (Mathf.Abs(sx) > 0.2f)
                {
                    float dir = sx > 0f ? 1f : -1f;
                    // 贴图默认朝左时取反，修正"飞起来朝向跟贴图不吻合"
                    if (spriteFacesLeft) dir = -dir;
                    CheckFlip(dir);
                }
            }
            else
            {
                CheckFlip(rb != null ? rb.velocity.x : 0f);
            }
        }

        /// <summary>
        /// 飞行怪视野（双阈值滞回 + 可选遮挡）：
        /// 追击中允许追到 loseRange（12）才放弃，避免"发现 8 / 丢失 8"同值导致的边界抖动抽搐。
        /// </summary>
        public override bool CanSeePlayer()
        {
            if (player == null) return false;

            Vector2 from = transform.position;
            Vector2 to = player.position;
            float dist = Vector2.Distance(from, to);
            if (dist > loseRange) return false;

            // 可选：障碍物遮挡（勾了 useVisionBlock 才生效；飞行怪在空中，默认关）
            if (useVisionBlock && dist > 0.1f && visionBlockLayer.value != 0)
            {
                Vector2 dir = (to - from).normalized;
                RaycastHit2D hit = Physics2D.Raycast(from, dir, dist, visionBlockLayer);
                if (hit) return false;
            }
            return true;
        }

        /// <summary>火球独立冷却（避免吐球太频繁）</summary>
        public bool CanFireball()
        {
            return Time.time >= nextFireballTime;
        }

        public void RegisterFireball()
        {
            nextFireballTime = Time.time + fireballCooldown;
        }

        /// <summary>受击结束后回到飞行轨道（而不是基类的地面追击）</summary>
        public override EnemyState GetPostHurtState()
        {
            return player != null ? (EnemyState)flyingFlight : (EnemyState)flyingIdle;
        }

        /// <summary>攻击结束后回到飞行轨道（火球/冲撞/下坠打完都回飞行，不掉进地面追击）</summary>
        public override EnemyState GetCombatReturnState()
        {
            return player != null ? (EnemyState)flyingFlight : (EnemyState)flyingIdle;
        }

        protected override void Die()
        {
            // 死亡：可加飞行怪专属特效（粒子等），复用基类销毁
            base.Die();
        }
    }
}
