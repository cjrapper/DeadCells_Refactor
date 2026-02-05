# Unity 学习与面试笔记

## 10. Platformer Feel (平台跳跃手感)
- **Coyote Time (土狼时间)**:
  - *定义*: 玩家离开地面的一瞬间（比如 0.1s 内），仍然允许起跳。
  - *原理*: 用一个计时器 `coyoteTimeCounter`，在地面时重置，离地后递减。只要 `counter > 0` 就可以跳。
  - *作用*: 极大提升手感，防止玩家在平台边缘因为按晚了几帧而掉下去。
- **Jump Buffer (跳跃预输入)**:
  - *定义*: 玩家在落地前一瞬间按下跳跃，落地时自动起跳。
  - *原理*: 按下跳跃键时设置 `jumpBufferCounter`，Update 中递减。落地时检查 `buffer > 0` 则起跳。
  - *作用*: 让操作感觉更流畅，不需要精确到帧的反应。

## 11. Tilemap (瓦片地图) & 物理优化
- **Tilemap Collider 2D**: 给每个瓦片添加独立碰撞体。
- **Composite Collider 2D (复合碰撞体)**:
  - *作用*: 将相邻的无数小碰撞体合并成一个大碰撞体，优化性能并解决物理 bug。
  - *关键设置*: 
    1. Tilemap Collider 2D 必须勾选 **Used By Composite**。
    2. Rigidbody 2D (自动添加) 的 **Body Type** 必须设为 **Static** (否则地图会掉下去)。
  - *常见 Bug*: **Ghost Collision (卡脚/粘墙)**。角色在平地上走不动或被绊倒，通常就是因为没开 Composite，导致角色卡在两个瓦片的微小接缝里。

## 12. Animation (动画系统)
- **Loop Time**:
  - *位置*: 动画文件 (.anim) 的 Inspector 中。
  - *作用*: 决定动画是否无限循环。**攻击/跳跃等单次动作必须取消勾选**，否则会抽搐或停不下来。
- **Entry & Default State**:
  - *Entry (入口)*: 状态机启动时的第一站。
  - *修改默认*: 右键任意 State -> **Set as Layer Default State** (变成橘黄色)。
- **Transitions (连线)**:
  - **Has Exit Time**:
    - *勾选*: 必须等当前动画播放完 (或播到 Exit Time 设定值) 才能切换。适用于 `Attack -> Idle` (打完收招)。
    - *不勾选*: 只要条件满足 (如 Trigger 触发) 立即切换。适用于 `Idle -> Attack` (立即出招)。

## 13. Advanced Movement (高级移动 - 蹬墙跳)
- **核心逻辑**:
  - **Wall Check**: 射线/OverlapCircle 检测前方是否有墙 (Layer: Ground)。
  - **Wall Slide (滑墙)**: 空中 + 贴墙 + 下落状态 -> 限制 `rb.velocity.y` 为一个小负值 (如 -2)。
  - **Wall Jump (蹬墙跳)**:
    - 施加反向力: `new Vector2(-wallDir * forceX, forceY)`。
    - **关键难点 (面试考点)**: **Control Lock (操作锁定)**。
      - *现象*: 如果不锁定，玩家按住墙方向键会瞬间抵消反向力，导致跳不远。
      - *解决*: 蹬墙跳后 0.1~0.2s 内，**完全屏蔽**水平移动输入 (`moveInput`)。

- **Input Priority (输入优先级)**:
  - `WallJump` > `RegularJump`。
  - 触发 WallJump 时，必须**清空** `CoyoteTime` 和 `JumpBuffer`，防止一键双跳 (斜跳后紧接着又判定一次直跳)。

## 14. One Way Platform (单向平台)
- **组件配置**:
  - **Platform Effector 2D**: 
    - *原理*: 控制碰撞体的有效角度。默认 `Surface Arc = 180` 表示只有上方碰撞有效，从而实现“从下往上跳能穿过，落下来能踩住”。
    - *必须勾选*: Collider 2D 组件上的 **Used By Effector**。
- **下跳机制 (Down + Jump)**:
  - *核心 API*: `Physics2D.IgnoreCollision(playerCollider, platformCollider, true)`。
  - *逻辑*:
    1. `OnCollisionEnter2D` 记录当前踩着的平台 (`oneWayPlatform`)。
    2. 按下跳组合键时，开启 IgnoreCollision (忽略碰撞)。
    3. 协程等待 0.2~0.5s 后，关闭 IgnoreCollision (恢复碰撞)。
  - *面试点*: 为什么不用 Trigger 或修改 Layer？(因为 IgnoreCollision 是点对点的，不会导致玩家掉穿地面或其他物体，更安全)。

## 15. One Way Platform Grounded Fix (单向平台落地判定优化)
- **问题**: 单向平台被 Ground 检测命中，导致角色“穿过平台时”被误判为落地，从而可以半空跳。
- **解决方案**:
  - 地面检测只勾 **Ground** Layer。
  - 单向平台单独建 Layer (如 `OneWayPlatform`)，用第二个 `OverlapCircle` 检测 `isOnOneWay`。
  - 跳跃判定使用 `isJumpable = isGrounded || isOnOneWay`，并在穿透时禁用 `isOnOneWay`。
- **面试点**: LayerMask 的“过滤”思想能有效避免误判和逻辑耦合。

## 20. Variable Gravity (变量重力)
- **原理**: 根据玩家的垂直速度（`velocity.y`）动态调整 `Rigidbody2D.gravityScale`。
- **配置**:
  - `jumpGravityScale` (上升重力): 较小（如 1.0），让跳跃感觉更轻盈，上升更高。
  - `fallGravityScale` (下降重力): 较大（如 2.5），让下落更迅速，减少滞空时的“漂浮感”。
- **优势**: 
  - 提升打击感和操作精确度。
  - 配合“长按跳得高，短按跳得低”逻辑，能实现极佳的平台跳跃手感。

## 21. Input-Lock during Wall Jump (蹬墙跳输入锁定)
- **问题**: 蹬墙跳时，如果玩家依然按住墙的方向键，状态机可能会立即将角色转回墙面，抵消掉向外的推力。
- **解决方案**: 
  - 在转向逻辑中添加判断：`if (currentState != WallJumpState)`。
  - 或者在蹬墙跳瞬间通过协程锁定 `MoveInput` 约 0.1s~0.2s。


## 16. Enemy Telegraph (敌人攻击前摇)
- **核心流程**: `Chase -> Telegraph -> Attack -> Chase`。
- **Telegraph 要点**:
  - 前摇期间停止移动、显示感叹号。
  - 协程延时结束后再决定是否进入 Attack。
  - 受击进入 Hurt 时需要中断前摇协程，防止状态被覆盖。
- **Attack 要点**:
  - 只在 `Time.time >= nextAttackTime` 时执行一次伤害，然后切回 Chase。

## 1. Physics 2D (物理系统)
- **OverlapCircle**: 
  - 用途：用于地面检测 (Ground Check) 或 攻击判定。
  - 语法：`Physics2D.OverlapCircle(point, radius, layerMask)`
  - *面试点*：相比 Raycast，它能检测一个圆形区域，更适合判定脚底是否接触地面，防止边缘判定失效。

## 17. Hierarchical State Machine (分层状态机 - HSM)
- **核心思想**: 将状态组织成树状结构。子状态可以继承父状态的通用逻辑（如在空中状态下都能移动）。
- **结构示例**:
  - `Root (PlayerState)`: 定义 `Enter`, `Exit`, `LogicUpdate`, `PhysicsUpdate` 等基础虚方法。
  - `SuperState (GroundedState)`: 处理地面通用输入（如跳跃、冲刺检测）。
    - `SubState (IdleState)`: 速度为 0。
    - `SubState (MoveState)`: 处理左右移动。
- **优势 (面试必考)**:
  - **减少代码冗余**: 不需要每个状态都写一遍移动输入检测。
  - **逻辑清晰**: 通过 `base.LogicUpdate()` 轻松复用父类行为。
  - **易于扩展**: 增加新功能（如二段跳）只需修改父类或增加子类，不影响现有逻辑。

## 18. Object Pooling (对象池模式)
- **场景**: 频繁生成和销毁的对象（如子弹、残影、粒子特效）。
- **原理**: 
  - 启动时预先创建一批对象（Prewarm）。
  - 需要时从池中取（`SetActive(true)`）。
  - 用完后归还池中（`SetActive(false)`），而不是 `Destroy`。
- **关键代码 (SamplePool)**:
  - `Queue<GameObject> pool`: 使用队列存储闲置对象。
  - `Get()`: 判空则 `Instantiate`，否则 `Dequeue`。
  - `Return()`: `Enqueue` 并重置对象状态。
- **面试点**: 为什么要用对象池？（减少垃圾回收 GC 压力，避免频繁分配内存导致的瞬间卡顿）。

## 19. Ghost Trail Effect (残影特效)
- **实现方案**:
  - 每隔一定时间从对象池取出一个残影。
  - **关键步骤**: 将当前主角的 `Sprite` 赋值给残影的 `SpriteRenderer`。
  - **动画处理**: 使用协程或 DoTween 让残影的 Alpha 值随时间递减到 0，然后归还池中。
- **触发机制**: 通常在速度超过一定阈值（如冲刺或蹬墙跳）时开启计时器生成。

## 24. Effect System Decoupling & Control (特效系统解耦与控制)
- **问题**: 仅靠速度判断生成残影（Ghost Effect）不精确，且容易受数值配置影响（如速度刚好在阈值边缘时抖动）。
- **解决方案**:
  - **接口化**: 在 `PLayerEffect` 中提供 `SetEffectActive(bool)` 接口。
  - **状态机介入**: 
    - 在 `DashState.Enter` 时调用 `SetEffectActive(true)`。
    - 在 `DashState.Exit` 时调用 `SetEffectActive(false)`。
  - **容错处理**: 在 `Awake` 中使用 `GameObject.Find()` 自动寻找丢失的对象池引用，增强系统的鲁棒性。
- **面试点**: 为什么不直接在 State 里写生成代码？（解耦。状态机只负责逻辑切换，特效脚本负责表现细节，方便后期更换表现方式而无需改动逻辑）。


// 25. 创建样式对象，并从默认的 "Box" 样式继承属性
// GUI.skin.box 是 Unity 内置的一个半透明黑色圆角矩形样式
GUIStyle style = new GUIStyle(GUI.skin.box); 

// 2. 自定义修改
style.alignment = TextAnchor.UpperLeft; // 文字左上对齐
style.fontSize = 18;                    // 字体变大（默认好像是12或13）
style.normal.textColor = Color.white;   // 设置普通状态下的文字颜色
style.padding = new RectOffset(10, 10, 10, 10); // 设置内边距 (左,右,上,下)

// 3. 应用样式
// 在绘制 Box 或 Label 时，把 style 作为最后一个参数传进去
GUI.Box(new Rect(10, 10, 200, 140), info, style);
- **Rigidbody2D**:
  - `velocity`: 直接修改速度，适合跳跃或瞬间移动。
  - `AddForce(force, ForceMode2D.Impulse)`: 施加瞬间力（如击退、爆炸）。
  - `gravityScale`: 控制重力倍率。冲刺 (Dash) 时设为 0 可防止下坠。
  - *坑点*：修改 transform.position 会无视物理碰撞，建议尽量操作 Rigidbody。

- **Physics Material 2D (物理材质)**:
  - 用途：解决“粘墙”问题。
  - 设置：Friction (摩擦力) 设为 0。
  - 代码动态创建：`new PhysicsMaterial2D("NoFriction") { friction = 0f }`。

## 2. Coroutines (协程)
- **核心概念**:
  - 允许函数暂停执行 (`yield return`)，稍后继续。
  - 必须返回 `IEnumerator`。
  - 启动：`StartCoroutine(MethodName())`。
- **常用等待**:
  - `yield return new WaitForSeconds(t)`: 受 Time.timeScale 影响（游戏暂停时会停）。
  - `yield return new WaitForSecondsRealtime(t)`: 不受时间缩放影响（用于做 HitStop 顿帧）。
- **实战应用**:
  - Dash (冲刺)：冲刺 -> 等待时间 -> 结束冲刺 -> 等待冷却。
  - HitStop (打击顿帧)：时间暂停 -> 等待真实时间 -> 恢复时间。

## 3. Finite State Machine (有限状态机)
- **写法**: 使用 `enum State { Patrol, Chase, Attack }` 配合 `switch-case`。
- **最佳实践**:
  - **Update**: 只处理状态切换条件的判断 (Input, Distance Check)。
  - **FixedUpdate**: 只处理该状态下的物理行为 (Velocity, Movement)。
  - *面试点*：为什么要分离？防止物理逻辑在每一帧运行频率不一致；逻辑解耦清晰。

## 4. 面向对象与接口 (Interface)
- **IDamageable**:
  - 定义：`interface IDamageable { void TakeDamage(...); }`
  - 优势：解耦。攻击者不需要知道对面是 Player 还是 Enemy，只要 GetComponent<IDamageable> 存在即可攻击。
  - *面试点*：这是“多态”在游戏开发中的典型应用。

## 5. 常用数学与逻辑
- **Mathf.Sin(Time.time)**: 生成 -1 到 1 的正弦波，常用于简单的巡逻移动或悬浮动画。
- **Vector2.Distance(a, b)**: 计算两点距离，比 `(a-b).magnitude` 写法更简洁。

## 6. UGUI 系统
- **Canvas (画布)**:
  - 所有 UI 元素必须在 Canvas 下。
  - **Scale With Screen Size**: 做手游/多分辨率适配必选设置，Reference Resolution 设为设计稿尺寸（如 1920x1080）。
- **Image (Fill 模式)**:
  - 制作血条/进度条神器。
  - 设置 `Image Type: Filled`，调整 `Fill Amount` (0.0~1.0) 即可控制显示进度。
- **RectTransform**:
  - **Anchor (锚点)**: 决定 UI 相对父级的位置。
  - *技巧*: 按住 `Alt + Shift` 点击锚点面板，可以同时设置锚点和位置（如 Stretch/Stretch 用于铺满）。

## 7. Design Pattern (设计模式) - 单例模式 (Singleton)
- **用途**: 用于全局唯一的管理类（如 UIManager, GameManager）。
- **核心代码**:
  ```csharp
  public static UIManager instance;
  void Awake() {
      if (instance == null) instance = this;
      else Destroy(gameObject);
  }
  ```
- **好处**: 其他脚本可以直接通过 `UIManager.instance.Method()` 调用，无需在 Inspector 中拖拽引用。
- **面试点**: 什么时候用单例？（全局管理、跨场景数据）。缺点是什么？（高耦合、生命周期难管理）。

## 22. Combat System Optimization (战斗系统优化)
- **Attack Cooldown (攻击冷却)**:
  - **实现**: 使用 `nextAttackTime = Time.time + cooldown`。
  - **状态机结合**: 在 `LogicUpdate` 中根据 `CanAttack()` 判定是否允许切入 `AttackState`。
- **Air Attack (空中攻击)**:
  - **逻辑**: 在 `InAirState` 或其子类中添加攻击按键检测。
  - **体验**: 空中攻击通常不应重置垂直速度，或者仅在第一击时有轻微的滞空效果（通过修改 `gravityScale` 实现）。

## 23. Inspector Configuration Checklist (配置检查清单)
- **Wall Movement**:
  - `Wall Slide Speed`: 必须大于 0 (推荐 2~4)，否则会卡在墙上。
  - `Wall Jump Force`: 必须设置 X 和 Y 分量 (推荐 10, 12)，否则无法弹开。
- **Detection Points**:
  - `Front Check`: 必须位于角色前方且偏移量适中，建议不要复用攻击判定点以防冲突。


## 8. Game Feel (游戏手感/打击感)
- **Juice (多汁感)**: 指通过视觉/听觉反馈让游戏更“爽”的技术美术手段。即便没有美术素材，也可以通过以下方式极大提升打击感：
  - **Screen Shake (屏幕震动)**: 最核心手段。
  - **Hit Stop (顿帧)**: 攻击命中瞬间暂停 0.05~0.1s，模拟阻力。
  - **Knockback (击退)**: 物理力反馈。
  - **Flash (闪白)**: 视觉反馈。
  - **Particles (粒子)**: 模拟火花/血液。

## 9. Cinemachine (虚拟相机系统)
- **Impulse System (震动系统)**:
  - **Source (震动源)**: 也就是发出震动的一方 (如 Player 受伤时)。组件 `Cinemachine Impulse Source`。
    - *关键设置*: `Impulse Shape` (Bump/Recoil) 定义波形；`GenerateImpulse(force)` 发送信号。
  - **Listener (监听者)**: 也就是相机 (Virtual Camera)。扩展组件 `Cinemachine Impulse Listener`。
    - *原理*: 监听特定 Channel 的震动信号并施加到相机位置。
  - *面试点*: 为什么用 Cinemachine 震动而不用代码写 `transform.position` 抖动？(因为 Cinemachine 会接管相机位置，手动修改会被覆盖；且 Impulse 系统更平滑、支持多源混合)。

## 25. Animation: SetBool vs CrossFade (动画状态切换)
- **SetBool / SetTrigger**:
  - *原理*: 修改 Animator 中的参数，依赖 Animator 窗口中手动连线 (Transitions) 和条件 (Conditions) 来切换状态。
  - *缺点*: 
    - **逻辑割裂**: 代码里写了一半逻辑 (`SetBool`)，Animator 里藏了一半逻辑 (连线条件)。
    - **蜘蛛网**: 状态多了以后连线会极其复杂 (Any State 连所有)。
    - **延迟**: 默认的 Transition 即使设为 0 也有微小开销。
- **CrossFade (推荐)**:
  - *原理*: `animator.CrossFade("StateName", transitionDuration)`。
  - *优势*: 
    - **完全代码控制**: 不需要连线，不需要参数，直接指名道姓播放哪个状态。
    - **适合 FSM**: 状态机脚本（如 `Enter()`）直接决定播放什么动画，逻辑高度集中。
    - **性能略优**: 省去了 Animator 每帧检测 Transition 条件的开销。
  - *面试点*: 为什么在复杂动作游戏中推荐 CrossFade？(为了逻辑解耦和精确控制)。

## 26. Developer Debug Mode (开发者调试模式)
- **OnGUI**:
  - Unity 最古老的 UI 系统，性能一般但非常适合做**Debug 工具**。
  - `GUI.Label(rect, text, style)`: 快速在屏幕上打印变量。
- **实战应用**:
  - 显示当前状态机状态 (`StateMachine.CurrentState`)。
  - 显示物理变量 (Velocity, Grounded)。
  - **开关控制**: 使用 `Input.GetKeyDown(KeyCode.BackQuote)` (波浪号键) 切换 `showDebugInfo` 布尔值，避免正式包里一直显示。

## 27. HFSM (Hierarchical Finite State Machine, 分层状态机)
- **疑问**: "我现在的状态机是分层的吗？"
- **解答**: **是的！**
  - **继承即分层**: 我们通过 C# 的 **类继承 (Inheritance)** 实现了分层。
    - **Root**: `PlayerState` (基类)
    - **Super State (父状态)**: `PlayerGroundedState` (处理地面共性), `PlayerInAirState` (处理空中共性), `PlayerAbilityState` (处理技能共性)。
    - **Sub State (子状态)**: `PlayerIdleState`, `PlayerJumpState` 等。
  - **工作原理**: 
    - 当你在 `PlayerIdleState` 中调用 `LogicUpdate()` 时，它会先执行 `base.LogicUpdate()` (即 `PlayerGroundedState` 的逻辑)。
    - 这就是分层状态机的核心：**父状态处理通用逻辑，子状态处理特定逻辑**。
  - *区别*: 这种写法叫 "Code-based HFSM" (基于代码的分层)，有些插件（如 NodeCanvas）是 "Graph-based HFSM" (基于图的分层)，原理一样，表现形式不同。
