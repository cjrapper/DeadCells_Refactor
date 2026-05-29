# 🗡️ Project Dash (2D 动作平台跳跃游戏)

> 本项目是由个人独立开发的 Unity 2D 动作平台跳跃 Demo，主要用于底层逻辑架构与游戏手感调优的实践。

## 🎮 游戏实机演示 (Game Demo)
![demo-ezgif com-optimize](https://github.com/user-attachments/assets/113adab2-8321-4740-b5d1-5cc8124bfc62)



## 🛠️ 技术栈 (Tech Stack)
*   **游戏引擎：** Unity 2022.x (2D URP)
*   **编程语言：** C#
*   **架构模式：** FSM 有限状态机、行为树 (Behavior Tree)、事件总线 (Event Bus)、对象池
*   **物理与表现：** Rigidbody2D, TrailRenderer, UGUI, Cinemachine, TextMeshPro

## ✨ 核心特性与架构 (Core Features & Architecture)

### 1. 有限状态机 (FSM) 角色控制
摒弃了臃肿的 `if-else` 面条代码，采用**接口+类**的形式实现了独立的内部状态机。
*   已实现状态：`Idle`, `Move`, `Jump`, `Fall`, `Dash`, `Attack`, `WallSlide`, `WallJump`。
*   优势：严格遵循开闭原则与单一职责，状态间逻辑彻底解耦，极大地提升了新技能扩展的安全性与可读性。

### 2. 硬核平台跳跃手感调优
深度拆解《蔚蓝》等经典平台跳跃游戏手感，在代码层面实现了以下机制：
*   **土狼时间 (Coyote Time)：** 离开平台边缘后的短暂时间内仍允许起跳，提升玩家容错率。
*   **跳跃预输入 (Jump Buffering)：** 落地前提前按下跳跃键，落地后会自动立刻起跳。
*   **动态重力缩放：** 上升时重力较小（轻盈），下落时重力倍增（扎实）。

### 3. 战斗系统
*   **近战武器 (SwordWeapon)：** `OverlapCircleNonAlloc` 圆形范围检测，支持伤害、击退。
*   **远程武器 (RangedWeapon)：** 从对象池取出弹丸，配置速度、伤害、射程后发射。
*   **HitStop 顿帧：** 命中时短暂冻结 `Time.timeScale`，增强打击反馈感。
*   **ScriptableObject 武器数据：** 武器参数（伤害、冷却、射程、击退力）统一存储在 `WeaponData` 中，不硬编码。
*   **IDamageable 接口：** 统一伤害处理，角色和敌人都实现同一接口，战斗逻辑解耦。

### 4. 敌人 AI 系统
*   **抽象基类 Enemy：** 可扩展的敌人框架，支持多种敌人类型。
*   **敌人状态机：** Patrol（巡逻）→ Chase（追击）→ Telegraph（预警）→ Attack（攻击）→ Hurt（受击），五个独立状态类。
*   **Q版史莱姆弹性动画：** 正弦驱动的程序化 Squash & Stretch，体积守恒 + 平滑过渡。

### 5. 行为树 (Behavior Tree)
独立实现了一套轻量级行为树框架，用于更灵活的 AI 行为编排：
*   **组合节点：** `SequenceNode`（AND 逻辑）、`SelectorNode`（OR 逻辑）。
*   **叶子节点：** `ActionNode`（执行具体行为）、`ConditionNode`（条件判断）。
*   **Blackboard 黑板：** 所有节点共享的键值对数据中心，实现数据与逻辑解耦。
*   **示例敌人 (DummyEnemy)：** 用行为树驱动 —— `Selector(Sequence(玩家在范围内? → 追击), 待机)`。

### 6. 事件中心 (EventCenter)
*   单例事件总线，`Dictionary<string, Action>` 实现模块间通信。
*   支持无参事件和 `int,int` 参数事件，UI、血量、死亡等模块通过事件解耦，不直接引用。

### 7. 对象池 (Object Pool) 性能优化
针对高频触发的**冲刺残影、落地扬尘、弹丸**等，自主实现了 `Queue` 队列对象池模块。
*   将高频的 `Instantiate/Destroy` 转化为 `SetActive(true/false)` 的循环复用。
*   实现了资源的 O(1) 复杂度存取，从源头上切断了堆内存碎片的产生，避免了 GC (垃圾回收) 导致的主线程掉帧卡顿。

### 8. 渲染与物理表现
*   规范化 `Sorting Layer` 解决 2D 渲染穿模问题。
*   基于多摄像机实现动态视差背景 (Parallax Scrolling) 效果。
*   **Cinemachine 屏幕震动：** 受击时触发 Impulse 震屏，强化打击反馈。
*   **URP 2D Renderer：** 使用 URP 渲染管线，支持 2D 光照系统。

*   -----

### 2026.01.26 开发日志：敌人程序化动画

Q版史莱姆弹性动画通过正弦驱动 + 体积守恒 + 平滑过渡实现物理质感的动态反馈。
详见 `SlimeEnemy.cs` 和 `DummyEnemy.cs` 中的 `UpdateVisual()` 实现。

### 2026.05.28 开发日志：行为树系统

新增 `Assets/_Scripts/Node/` 目录，实现了一套轻量级行为树框架：
*   **核心节点：** `BehaviourNode`（抽象基类）、`ActionNode`（动作）、`ConditionNode`（条件）
*   **组合节点：** `SequenceNode`（顺序执行）、`SelectorNode`（选择执行）
*   **Blackboard：** 共享数据黑板，支持泛型读写
*   **DummyEnemy：** 用行为树构建了一个完整敌人 AI 示例
