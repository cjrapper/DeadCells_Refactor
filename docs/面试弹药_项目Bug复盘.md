# 面试弹药 —— 项目 Bug 复盘（15 个真实案例）

> 最后更新：2026-09-10

> 用法：**别背代码，背"现象 → 根因 → 修法"三句话。**
> 面试官问"你项目里遇到过什么难点"，你能讲出 3 个这种案例，就比 90% 的应届生强。
>
> 每个案例的**考点**是通用规律 —— 面试官真正想听的是这个，不是"我改了哪一行"。

---

## 一、序列化 / Unity 编辑器规则

### 1. 基类和子类不能有同名字段

**现象**：Inspector 报 `The same field name is serialized multiple times in the class or its parent class. This is not supported: Base(MonoBehaviour) keepDistance`，组件显示异常。

**根因**：我在基类 `Enemy` 里加了 `keepDistance`，而子类 `FlyingEnemy` 本来就有同名字段。C# 的字段隐藏（`new`）**对 Unity 的序列化系统无效** —— 它按名字匹配，撞名就拒绝序列化。

**修法**：基类字段改名 `minDistance`。加基类字段前先扫一遍所有子类。

**考点**：Unity 序列化是**基于字段名**的字符串匹配，不理解 C# 的继承隐藏规则。给基类加字段是"影响所有子类"的操作，要当成 API 变更来对待。

---

### 2. 代码里的常量覆盖了预制体里调好的配置（踩了 3 次）

**现象**：
- 火球飞出去朝向永远不对（预制体里明明转过）
- Boss 预警红圈变成正对摄像机的正圆（预制体里是微微斜着的贴地椭圆）
- 武器换上去角度总是歪的（预制体里调好了）

**根因**：三个地方都是同一个错误 —— **Instantiate 或赋值时写死了常量**：

```csharp
Instantiate(prefab, pos, Quaternion.identity);      // 把预制体的旋转清成 0
transform.localScale = Vector3.one * k;             // 把预制体的缩放清成 1
heldVisual.transform.localRotation = Quaternion.identity;  // 同上
root.localRotation = Quaternion.Euler(0, 0, -angle);       // 挥砍时写绝对角度
```

**修法**：
- 实例化用 `Instantiate(prefab, parent, false)` —— 第三个参数 `false` = 保留预制体自己的 local 变换
- 缩放/旋转做**相对运算**：`预制体缩放 × 系数`、`静止朝向 × 增量`

**考点**：这是"资源驱动"项目里最常见的 bug 类型 —— **代码和美术争夺同一个数值**。防御原则：**代码只做相对换算，绝不写绝对值**。这样美术改资源不用改代码。

**附加坑**：`Instantiate(obj, parent)` 两参数版默认 `worldPositionStays = true`（保持世界坐标），会把预制体摆到错误位置。这个默认值本身就很坑。

---

### 3. 序列化的字段被覆盖后静默失效

**现象**：死亡动画时好时坏，改过一次 Inspector 之后又没了。

**根因**：`PlayerHealth.deathAnimName` 序列化在**预制体**上。Animator 里的状态叫 `PlayerDeadthState`，但预制体上存的是 `Death`。名字对不上 → `Animator.HasState` 返回 false → **静默跳过死亡动画，只打一条警告**。

**修法**：不让玩家手动配对 —— 按 `配置名 → Death → Die → PlayerDeath → PlayerDeadthState` 依次试，试到就用并打日志。

**考点**：**预制体上的序列化值会覆盖 C# 里的默认值** —— 改代码默认值对已存在的实例无效（我踩过好几次：`keepDistance`、`levelSceneName`、`deathAnimName`）。排查这类问题要问："这个值是从代码来的还是从资源来的？"

---

### 4. 未激活的 GameObject 不跑 Update

**现象**：按 B 打不开背包，**完全没有任何报错**。

**根因**：`InventoryPanel.Awake()` 里 `gameObject.SetActive(false)` 把自己关了 —— 而"按 B 打开"的代码写在 `Update()` 里。**物体未激活时 Unity 不再调它的 Update**，所以那个键永远收不到。

**修法**：拆成两层 —— 自己保持激活只负责听按键，把可见 UI 放进 `Content` 子容器，隐藏的是子容器。

**考点**：Unity 的生命周期是**跟着 active 状态走的**。任何"用某个键/事件唤醒自己"的组件，都不能把自己 `SetActive(false)`。

---

## 二、2D 物理

### 5. 单数版 OverlapCircle 返回的是"任意一个"碰撞体（踩了 2 次）

**现象**：敌人冲撞"明明撞到了却没伤害"，而且是**概率性**的（时灵时不灵）。

**根因**：
```csharp
Collider2D hit = Physics2D.OverlapCircle(center, radius, layer);  // 单数版
IDamageable target = hit.GetComponent<IDamageable>();
```
- `OverlapCircle`（单数）只返回**其中一个**碰撞体，返回哪个**不确定**
- 玩家身上有多个碰撞体，`IDamageable`（PlayerHealth）挂在**根节点**上 → 子碰撞体用 `GetComponent` 找不到

**修法**：
```csharp
int n = Physics2D.OverlapCircleNonAlloc(center, radius, buffer, layer);
for (...) {
    IDamageable t = buffer[i].GetComponentInParent<IDamageable>();  // 往上层找
    ...
}
```

**考点**：**概率性失效的 bug 比必现的难查得多**。角色身上通常有多个碰撞体（本体 + 攻击判定 + 拾取范围），所以判定一定要"扫全部 + 往父物体找接口"。

**更大的教训**：这个坑我在 `AttackState` 里修过一次、还写了注释标"这是根因"，**但同一个逻辑在 `FlyingDiveState` 里又抄了一遍**，没跟着改。→ **同类判定散落在多处就是隐患，应该抽成公共工具函数**（`EnemyHitUtil.TryDamagePlayer(...)`），改一次全改到。

---

### 6. Unity 2D 物理的回调不做父子冒泡

**现象**：门的交互触发器做好了、玩家走到门前，**没提示、按 E 也没反应，一个报错都没有**。

**根因**：`OnTriggerEnter2D` **只发给挂着碰撞体的那个 GameObject**。项目里把交互触发器做成了子物体（门本体放挡路的实心碰撞体，子物体放 Trigger 当交互范围）→ 脚本在父物体上，**永远收不到子物体触发器的回调**。

**对比**：3D 物理**会**把消息转发给 Rigidbody 所在的物体，2D **不会** —— 这个差异非常反直觉。

**修法**：不依赖触发回调，每帧自己做重叠检测：
```csharp
Physics2D.OverlapBoxNonAlloc(trigger.bounds.center, trigger.bounds.size, 0f, buffer);
```
（用 NonAlloc 版，不产生 GC）

**考点**："视觉/挡路用一个碰撞体、交互范围用另一个"是 2D 游戏的标准搭法，所以**必须知道 2D 不回传父子**。用代码主动查询反而更解耦：交互范围和物理形状可以完全不同。

---

## 三、状态机 / 游戏逻辑

### 7. 状态机抖动：两个状态的转换条件同时成立

**现象**：站在梯子底端按 S，状态在 `ground` 和 `ladder` 之间**每秒切换 60 次**。

**根因**：
```lua
-- ground：进入条件
if IsOnLadder and VerticalInput ~= 0 then return "ladder" end
-- ladder：退出条件
if CheckGrounded() and VerticalInput <= 0 then return "ground" end
```
在梯子底端的地面上按 S 时，**这两条同时为真** → 每帧来回切。

**代价**（很多人以为"切就切呗，能有多大开销"）：
- `Animator.CrossFade` 每秒被调 120 次 → 动画永远停在过渡的第一帧
- `Enter/Exit` 里的重力设置每帧在 0 和 3.5 之间横跳 → 物理表现不可预测
- 以后在 `Enter/Exit` 里加音效/粒子/事件 → 每帧触发 = 音效爆音、对象池爆炸

**修法**：让条件**互斥** —— 进入梯子前问一句"这一按真的能爬吗"（↑ 总能上；↓ 只有脚下还有梯子时才进）。

**考点**：**状态机的进入条件和退出条件不能在同一帧同时成立**，否则必定抖动。通用手段：条件互斥、加迟滞（hysteresis，双阈值）、或最小驻留时间。

---

### 8. 硬直 ≠ 无敌：一个标志位承担了两种语义

**现象**：贴着敌人时"一直被受击"，看起来永远站不起来。

**根因**：`IsHurting` 一个变量同时表示两件事：
- 受击硬直（不能操作），0.2 秒
- 受击无敌（不会再被打），也是 0.2 秒

两者时长被绑死 → **硬直一结束就立刻能再被打** → 站在任何持续判定里（贴脸近战、陷阱、滞留弹幕）就是"硬直刚结束又被硬直"。

**修法**：拆成两个时长 —— `hurtDuration`（硬直）+ `invincibleDuration`（i-frames，更长），无敌期间半透明闪烁。

**考点**：**不要把两种语义塞进一个标志位**。一旦合在一起，时长就被绑死，改一个必然影响另一个。这是动作游戏"必须有 i-frames"的工程原因。

---

### 9. StopCoroutine 不执行协程末尾的清理代码

**现象**：死亡时尸体一闪一闪、或者永久停在半透明状态。

**根因**：无敌闪烁 `BlinkRoutine` 在玩家被打死时还在跑。`StopCoroutine` 会停掉它，**但协程最后一行的"还原颜色"不会执行** —— 协程没有 `finally` 语义。

**修法**：调用方补一次还原，并缓存原始颜色（`baseSpriteColor`）。

**考点**：**"中途掐断的协程"必须由调用方补偿还原**。颜色、无敌帧、IgnoreCollision 这类"退出路径不唯一"的资源是游戏里最常见的泄漏来源。规范做法：把还原逻辑收敛到一个 `ResetState()`，正反两条路径都调它。

---

### 10. 动画状态名对不上 → 静默卡住

**现象**：Boss 受击动画"反复播放"、卡在受击姿势里不出来。

**根因**：`FlyingFlightState.AnimName` 写的是 `"Fly"`，但 Animator 控制器里的状态叫 `FlyIdle`。`PlayStateAnim` 找不到状态 + 没配回退动画 → **什么都不播**，Animator 停在上一段动画上。

**修法**：① 代码里的名字对齐 Animator；② **给 `PlayStateAnim` 加警告** —— 状态和回退都找不到时打一条明确日志（同一个名字只警告一次）。

**考点**：**"什么都不做"是最危险的失败模式**，因为它不报错。防御手段是**在关键路径上加主动校验和日志**，把"运行时没反应"变成"启动就告诉你怎么改"。

---

### 11. 死亡动画不播：normalizedTime 在循环状态上是累加的

**现象**：死亡动画看不到，直接弹结算。

**根因**：用 `normalizedTime >= 1` 判断"动画播完了"。但死亡状态是**循环**的，而且 Animator 从 Idle 过来时已经跑了很久 —— **循环动画的 normalizedTime 是累加值，早就 > 1 了** → 第一帧就判定"播完"。

**修法**：先等状态真正成为当前状态，再读 `GetCurrentAnimatorStateInfo(0).length` 拿 clip 真实时长，按 **unscaledDeltaTime** 数满这个时长。

**考点**：顿帧会把 `timeScale` 压到 0，所以**演出/暂停相关的计时必须用 unscaled 时间**。另外 `normalizedTime` 的语义在循环和非循环状态上不一样。

---

### 12. 单例写成实例属性 → 场景重载后资源被提前释放

**现象**：死亡重开之后报 `this lua env had disposed`，人物完全不能动。

**根因**：`LuaManager.Instance` 写成了**实例属性**（不是 `static`）。场景重载时新的 `LuaManager` 的 `Instance` 恒为 null → 不自我销毁 → `new` 一个新 `LuaEnv` 覆盖静态字段，随后它被销毁时 `this == Instance` 成立 → **把玩家正在用的 LuaEnv Dispose 掉了**。

**修法**：`public static LuaManager Instance`；`OnDestroy` 里加 `if (this != Instance) return;`。

**考点**：**单例引用必须是 static**。跨场景存活的对象（DontDestroyOnLoad）和场景内的冗余实例之间的关系，是"重开后状态错乱"类 bug 的高发区。

---

## 四、渲染 / 打包

### 13. `.lua` 不是 Unity 资源类型 → 打包后全丢

**现象**：编辑器里一切正常，**打包后人物完全不动**。

**根因**：`LuaManager` 从文件系统读 `Application.dataPath/_Scripts/Lua/...`。而 `.lua` 不是 Unity 认识的资源类型，放在 `Assets/` 下会被当成 `DefaultAsset`，**根本不会被打进包**。打包后 `Application.dataPath` 变成 `<游戏>_Data`，里面没有那个目录 → 所有 `require` 失败 → 玩家状态机初始化不了。

**修法**：Lua 移到 `Assets/StreamingAssets/Lua/`（这里面的文件**原样拷进包**），用 `Application.streamingAssetsPath` 读 —— **编辑器和打包后走同一份代码**。

**考点**：**"编辑器能跑"和"包能跑"是两件事**。凡是运行时读文件的功能（配置表、Lua、Mod），都要问："这个文件会进包吗？打包后这个路径还在吗？"xLua 热更新的标准做法就是把脚本放 StreamingAssets。

---

### 14. 2D 光照能不能生效，取决于三层链路

**现象**：加了灯、参数也调了，**就是没反应**，而且完全不报错。

**根因**（三层缺一不可）：
1. Graphics Settings 挂对 URP 资产
2. Renderer 是 2D Renderer（`Renderer2D.asset`，要配好 Light Blend Styles）
3. **Sprite / Tilemap 用 `Sprite-Lit-Default` 材质** ← 最容易漏的一层

另外两个专有坑：
- **`Apply To Sorting Layers`**：灯只影响勾中的排序层，把 sprite 换到别的排序层 = 隐身不受光
- **相机清屏色不受 2D 光照影响**：想"地图外很黑"要单独改 `Camera.Background`，加多少灯都没用

**修法**：三层逐一核对；排序层变化时补勾；清屏色单独设。

**考点**：**2D 光照的作用范围是"渲染器"，不是"画面"** —— 对相机清屏色、UI、粒子都不生效。"让画面变黑"要拆成两件事：画面底子（相机背景/后处理）和 场景受光（Light2D + 受光材质）。

---

### 15. `fillAmount` 只在 Image Type = Filled 时生效，且不报错

**现象**：血条不动，`fillAmount` 每帧都在写，就是没变化。

**根因**：`Image.type` 不是 `Filled`（是 Simple/Sliced）。**类型不对时 `fillAmount` 完全无效，Unity 不报任何错。**

**修法**：① 场景里把 Type 改成 Filled；② **在 `RefreshUI` 里主动校验一次配置并打日志**，把"运行时没反应"变成"启动就告诉你怎么改"。

同类：血条不满是因为底条和填充条用了**同一个左轴心 sprite**，但只给填充条做了偏移 → 两条错开半个身位，看着就像"没满"。

**考点**：**"配置错了但代码照跑"的问题最费时间**。工程上的对策是**初始化时主动校验关键配置**，而不是等玩家发现。

---

## 总结：这 15 个案例的通用规律（面试可以直接说）

| 规律 | 案例 |
|---|---|
| **代码只做相对运算，绝不覆盖资源里的绝对值** | 2、3 |
| **序列化按字段名匹配，预制体值覆盖代码默认值** | 1、3 |
| **生命期跟着 active 状态走** | 4 |
| **概率性失效 = 只处理了"其中一个"** | 5 |
| **2D 物理回调不冒泡** | 6 |
| **状态转换条件必须互斥（或带迟滞）** | 7 |
| **一个标志位只承担一种语义** | 8 |
| **中途掐断的协程要调用方补偿还原** | 9 |
| **"什么都不做"是最危险的失败模式 → 关键路径要主动校验+日志** | 10、15 |
| **演出/暂停计时必须用 unscaled 时间** | 11 |
| **单例引用必须 static** | 12 |
| **"编辑器能跑 ≠ 包能跑"** | 13 |
| **渲染特性有"作用范围"边界** | 14 |
| **同类逻辑散落多处 = 隐患，要抽公共函数** | 5 |
