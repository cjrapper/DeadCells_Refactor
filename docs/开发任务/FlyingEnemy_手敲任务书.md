# 飞行敌人手敲任务书（FlyingEnemy）

> 最后更新：2026-09-09

> 目标：新增一只飞行敌人，状态：idle（悬浮待机）/ flight（飞行移动）/ attack（俯冲攻击）/
> hurt（受击，复用基类）/ death（死亡，走基类 Die）。
> 与史莱姆的本质区别：**不落地**（重力为 0）+ 立体追踪（X/Y 都朝玩家动）。
> 手敲提示：每段写完先编译（让 Unity 报错帮你），再敲下一段。

---

## ① 设计先想清楚（面试讲法）

> "飞行敌人我不落地：`gravityScale=0`，移动直接设 velocity（X 和 Y 都朝玩家），
> 待机时用正弦让它在空中轻微浮动（程序化动画，和史莱姆的 squash & stretch 同一套思路）。
> 状态机复用敌人的框架（Enemy → EnemyStateMachine → 各状态类），新敌人 = 新状态类。"

**行为状态图**：
```
idle（悬停 + 左右小幅漂移）
  ↓ 玩家进入 chaseRange（且视线未被遮挡）
flight（朝玩家追踪飞行，保持 ~2 单位距离）
  ↓ 距离 < attackRange × 1.5 且冷却好
attack（俯冲：先上抬 → 朝玩家冲击 → 离开）
  ↓ 攻击结束
flight（继续追）／玩家丢失 → idle
（受击任意时刻 → hurt → 回 flight；死亡 → Dashed/销毁）
```

---

## ② 文件清单（新建 5 个文件，全部放 `Assets/_Scripts/Enemy/`）

| 文件 | 内容 |
|------|------|
| `FlyingEnemy.cs` | 继承 Enemy：重力 0、悬浮动画、状态机初始化、玩家追踪参数 |
| `FlyingIdleState.cs` | 待机：悬停 + 正弦浮动 + 检测玩家进入 |
| `FlyingFlightState.cs` | 移动：朝玩家飞（保持距离衰减）+ **攻击判定**（远→火球 / 近→冲撞） |
| `FlyingFireballState.cs` | 火球攻击：朝玩家发射火球（复用 Projectile 弹丸） |
| `FlyingAttackState.cs` | （可选）专属俯冲；也可以直接复用基类 `AttackState`（冲撞）——**推荐复用** |

> 火球目标：**复用现有 Projectile 组件**（就是玩家 Fireball 用的那套：对象池 + Trigger
> 检测 + IDamageable），敌人的火球只是"换 targetLayer = 玩家层、换伤害值"。

---

## ③ 逐文件骨架（敲的时候按这个结构，注释是"为什么"）

### 3.1 FlyingEnemy.cs（核心类）

```csharp
using UnityEngine;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 飞行敌人 —— 不落地（gravityScale = 0），速度直接设（X/Y 立体追踪）。
    /// 状态：idle / flight / attack（hurt 和 death 复用基类）。
    /// </summary>
    public class FlyingEnemy : Enemy
    {
        [Header("Flying Settings")]
        public float idleRange = 8f;        // 玩家进入该距离 → 从 idle 切 flight
        public float attackTriggerRange = 3f;  // 玩家近到这个距离 → 俯冲攻击
        public float keepDistance = 2f;     // flight 时保持的距离（太近减速，太远加速）
        public float floatAmplitude = 0.3f; // 待机悬浮幅度（正弦）
        public float floatSpeed = 2f;       // 悬浮频率

        private float idleBaseY;            // 悬浮基线（Awake 记录起始 Y）

        protected override void Awake()
        {
            base.Awake();
            idleBaseY = transform.position.y;

            // 飞行敌人：无重力（关键！不然直接掉地上）
            if (rb != null) rb.gravityScale = 0f;

            // 初始化飞行专属状态（先写后两个状态文件，再回来删掉这行的 TODO）
            // 见 3.2/3.3/3.4，写完后在初始化列表补上 flyingIdle / flyingFlight / flyingAttack
            idleBaseY = transform.position.y;
        }

        // 程序化视觉：悬浮（sin 偏移视觉位置，不碰碰撞体）+ 翻转
        public override void UpdateVisuals()
        {
            // 悬浮：正弦上下浮动（视觉放在修改位置前 —— 用 sr 来表现，不动 transform？
            // 简单方案：直接微调 transform.position.y（碰撞体跟着动也没事，慢速小幅）
            float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, idleBaseY + offset, p.z);
        }

        protected override void Die()
        {
            // 死亡特效（可加粒子），复用基类销毁
            base.Die();
        }
    }
}
```

> 🎯 注意点：**gravityScale=0 要在 Awake 里 base.Awake() 之后**（base Awake 拿到 rb）。
> 悬浮用 `transform.position` 直接改 Y —— 这种"运动学移动"对飞行敌人 OK（不依赖物理碰撞
> 推挤），面试可以讲"飞行敌人我用运动学移动，史莱姆用物理,两种手感的取舍"。

### 3.2 FlyingIdleState.cs（待机）

```csharp
using UnityEngine;

namespace DeadCells.Enemy
{
    /// <summary>待机：空中悬停（正弦浮动由 UpdateVisuals 做），玩家进入范围 → flight</summary>
    public class FlyingIdleState : EnemyState
    {
        public FlyingIdleState(Enemy enemy, EnemyStateMachine sm) : base(enemy, sm) { }

        public override void Enter()
        {
            // 待机：停住（悬停靠 UpdateVisuals 的正弦）
            if (enemy.rb != null)
                enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
        }

        public override void LogicUpdate()
        {
            if (enemy.player == null) return;
            if (!enemy.CanSeePlayer()) return;

            float dist = ((FlyingEnemy)enemy).idleRange;
            if ((enemy.transform.position - enemy.player.position).magnitude < dist)
                stateMachine.ChangeState(flyingFlightState());   // 见 3.5 的错误提示
        }
    }
}
```

> ⚠️ 上面 `flyingFlightState()` 是**示意**：基类 Enemy 没有这个属性！
> 正确做法：像 Enemy.cs 那样在 FlyingEnemy 里暴露状态属性：

### 3.3 FlyingFlightState.cs（飞行移动 + 攻击判定）

```csharp
using UnityEngine;

namespace DeadCells.Enemy
{
    /// <summary>飞行：朝玩家追踪（X/Y 都动），保持 keepDistance；距离决定攻击手段</summary>
    public class FlyingFlightState : EnemyState
    {
        public FlyingFlightState(Enemy enemy, EnemyStateMachine sm) : base(enemy, sm) { }

        public override void PhysicsUpdate()
        {
            if (enemy.player == null) return;
            enemy.UpdateVisuals();

            Vector2 dir = (enemy.player.position - enemy.transform.position).normalized;
            float dist = Vector2.Distance(enemy.transform.position, enemy.player.position);

            // 太近减速、太远加速 —— 用 keepDistance 做"速度曲线"而不是死冲
            float speed = ((FlyingEnemy)enemy).keepDistance > dist
                ? enemy.chaseSpeed * 0.4f
                : enemy.chaseSpeed;

            enemy.rb.velocity = dir * speed;
        }

        public override void LogicUpdate()
        {
            if (enemy.player == null) { stateMachine.ChangeState(enemy.patrolState); return; }
            if (!enemy.CanSeePlayer()) { stateMachine.ChangeState(enemy.patrolState); return; }

            float dist = Vector2.Distance(enemy.transform.position, enemy.player.position);

            // 攻击手段取舍（两种攻击）：
            // ① 贴脸（< attackRange×1.5）→ 冲撞（复用基类 AttackState，冲刺+命中即停）
            if (dist < enemy.attackRange * 1.5f && enemy.CanAttack())
            {
                stateMachine.ChangeState(enemy.attackState);
                return;
            }
            // ② 中远距离（< fireballRange）→ 火球（吐球前先蓄力预警）
            FlyingEnemy flying = (FlyingEnemy)enemy;
            if (dist < flying.fireballRange && enemy.CanAttack())
            {
                stateMachine.ChangeState(flying.flyingFireball);
                return;
            }
        }
    }
}
```

> 💡 关键选择：**攻击手段按距离分层**——"贴脸用冲撞、中距离吐火球"是飞行敌人/法师类
> 怪物的标准 AI（防止近战怪和远程怪的行为割裂）。面试讲"我的飞行敌人在距离上做攻击选集"。

### 3.4 FlyingFireballState.cs（火球攻击，复用 Projectile）

```csharp
using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 火球攻击 —— 朝玩家方向发射弹丸（复用玩家的 Projectile 组件：
    /// Trigger 命中检测 + IDamageable + 对象池，敌人侧只换 targetLayer 和伤害）。
    /// 发射前短蓄力（加感叹号提示，给玩家反应时间）。
    /// </summary>
    public class FlyingFireballState : EnemyState
    {
        private float timer;

        public FlyingFireballState(Enemy enemy, EnemyStateMachine sm) : base(enemy, sm) { }

        public override void Enter()
        {
            timer = enemy.windupTime;         // 蓄力时间（复用 windupTime 字段）
            if (enemy.alertSign != null) enemy.alertSign.SetActive(true);
        }

        public override void LogicUpdate()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                FireFireball();
                stateMachine.ChangeState(enemy.player != null
                    ? enemy.chaseState
                    : enemy.patrolState);     // 吐完回追击
            }
        }

        private void FireFireball()
        {
            if (enemy.player == null) return;

            // 朝玩家方向（含 Y 的斜向弹道）
            Vector2 dir = ((Vector2)(enemy.player.position - enemy.transform.position)).normalized;

            // 从玩家侧的弹丸池拿（Projectile 组件通用）；池没有就 Instantiate 火球预制体
            SamplePool pool = PoolManager.Instance?.GetPool(PoolType.Projectile);
            GameObject proj = pool != null ? pool.Get() : null;

            if (proj != null)
            {
                proj.transform.position = enemy.transform.position;
                // 旋转 = 朝玩家方向（弹丸沿 transform.right 飞）
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                proj.transform.rotation = Quaternion.Euler(0, 0, angle);

                Projectile p = proj.GetComponent<Projectile>();
                if (p != null)
                {
                    p.Setup(enemy.laserDamage, 5f, enemy.playerLayer);  // 伤害/击退/目标层=玩家层
                    p.AssignPool(pool);
                    p.OnGetFromPool();
                }
            }
            else
            {
                // 兜底：直接实例化（任务书示例省略，参照玩家的 RangedWeapon.Attack 写法）
            }
        }

        public override void Exit()
        {
            if (enemy.alertSign != null) enemy.alertSign.SetActive(false);
        }
    }
}
```

> 🎯 需要给 FlyingEnemy 补两个字段 + 一个状态属性（Inspector 可调）：
> ```csharp
> [Header("Fireball")]
> public float fireballRange = 8f;      // 火球触发距离
> public FlyingFireballState flyingFireball { get; private set; }
> // Awake 初始化区加：
> flyingFireball = new FlyingFireballState(this, StateMachine);
> ```
> 弹丸速度/生命值在 Projectile 预制体上（Inspector 可调，20f 左右）。

### 3.4 FlyingEnemy.cs 补上初始化（写完状态类后回来）

```csharp
// FlyingEnemy 里加属性（仿照 Enemy.cs 的 patrolState 写法）：
public FlyingIdleState flyingIdle { get; private set; }
public FlyingFlightState flyingFlight { get; private set; }

// Awake 初始化区（base.Awake() 之后）：
flyingIdle = new FlyingIdleState(this, StateMachine);
flyingFlight = new FlyingFlightState(this, StateMachine);

// Start 改为：StateMachine.Initialize(flyingIdle);   （基类 Start 里 Initialize(patrolState) ——
// 覆写 Start 前先看 Enemy.Start 的实现，调用 base.Start() 后再改状态：
// StateMachine.Initialize(flyingIdle);  ← 在 base.Start() 之后重新初始化即可
```

> ⚠️ 记得检查 Enemy.Start 是 `protected virtual`（是的话覆写前先 `base.Start()`）。

### 3.5 状态属性都挂上后，FlyingIdle / FlyingFlight 里的切换改成：

```csharp
// FlyingIdleState.LogicUpdate：
stateMachine.ChangeState(((FlyingEnemy)enemy).flyingFlight);
// FlyingFlightState 攻切换：
stateMachine.ChangeState(enemy.attackState);   // enemy.attackState 是基类的（冲撞攻击）
```

---

## ④ 配置（Unity 里做）

1. 新建 Prefab（或复制史莱姆改成飞行外观）：`FlyingEnemy` 脚本 + Rigidbody2D（**gravityScale = 0，Constraints 冻结旋转**）+ Collider2D + SpriteRenderer（翅膀/飞行怪素材）
2. Inspector：`chaseRange` 5 / `attackRange` 1.5 / `playerLayer`= playerLayer（或留 0 走兜底）/ `alertSign` 配感叹号（可复用史莱姆的）
3. 放进场景的空中位置（别放地面上，悬空）

## ⑤ 验收

- [ ] 敌人悬空（不掉落、不落地）
- [ ] 玩家靠近 → 追着飞（有明显纵深：Y 也动）
- [ ] 贴脸 → 冲撞攻击（感叹号后冲撞）
- [ ] 打它 → 闪红 + 硬直 0.5s → 继续追
- [ ] 打死 → 销毁 + 掉金币（lootTable 配了就掉）

## ⑥ 面试考点总结

1. **重力 0 + 运动学移动**：飞行怪不依赖物理推挤，velocity 直接控制（史莱姆靠物理手感、飞行怪靠运动学——两种方案对比是加分项）
2. **复用状态**：攻击/hurt/death 全复用基类状态，新敌人只写 idle/flight 两个"移动行为"——"状态机框架的价值"就是这句话
3. **保持距离曲线**（太近减速太远加速）：比"无脑死冲"高级，体现 AI 手感意识
