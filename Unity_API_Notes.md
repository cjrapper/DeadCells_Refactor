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

## 1. Physics 2D (物理系统)
- **OverlapCircle**: 
  - 用途：用于地面检测 (Ground Check) 或 攻击判定。
  - 语法：`Physics2D.OverlapCircle(point, radius, layerMask)`
  - *面试点*：相比 Raycast，它能检测一个圆形区域，更适合判定脚底是否接触地面，防止边缘判定失效。

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