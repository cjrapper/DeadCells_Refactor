# Unity 学习与面试笔记

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