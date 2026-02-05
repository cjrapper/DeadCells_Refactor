# 重构 PlayerController 为分层状态机 (HSM)

## 核心架构设计
将原有的单体脚本 `PlayerController` 重构为分层状态机模式，以提高代码的可扩展性和可维护性。

### 1. 状态层级结构 (HSM)
- **Root (PlayerState)**: 基类，定义进入、退出、逻辑更新、物理更新和输入处理。
- **SuperState: GroundedState (父类)**: 处理地面通用行为（检测跳跃、冲刺、攻击）。
    - **SubState: PlayerIdleState**: 处理静止状态。
    - **SubState: PlayerMoveState**: 处理地面移动。
- **SuperState: InAirState (父类)**: 处理空中通用行为（左右移动、检测落地、滑墙逻辑）。
    - **SubState: PlayerJumpState**: 处理跳跃上升阶段。
    - **SubState: PlayerFallState**: 处理自由落体阶段。
- **SuperState: AbilityState (父类)**: 处理“播完即结束”的行为。
    - **SubState: PlayerDashState**: 冲刺逻辑。
    - **SubState: PlayerAttackState**: 攻击逻辑。
    - **SubState: PlayerWallJumpState**: 蹬墙跳逻辑。

### 2. 主要修改步骤
1.  **更新 [PlayerController.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerController.cs)**:
    - 移除原本的逻辑代码，将其作为“上下文(Context)”存储数据和组件引用。
    - 公开 `RB`, `Anim`, `SR`, `TR` 等组件的属性供状态访问。
    - 实例化 `PlayerStateMachine` 和所有状态类。
    - 在 `Update` 和 `FixedUpdate` 中调用当前状态的更新方法。
2.  **实现状态类**:
    - 在 `Assets/_Scripts/player/` 目录下填充/创建各状态类文件。
    - 确保子状态通过 `base.LogicUpdate()` 等方式继承父状态（SuperState）的逻辑。
3.  **保留手感优化**:
    - 将 **土狼时间 (Coyote Time)** 和 **跳跃缓冲 (Jump Buffer)** 逻辑整合进状态切换判断中。
    - 保留现有的平滑移动（Mathf.MoveTowards）和单向平台处理。

## 待创建/更新的文件列表
- [PLayerState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PLayerState.cs) (更新基类)
- [PlayerGroundedState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerGroundedState.cs) (实现父类)
- [PlayerIdleState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerIdleState.cs) (新子类)
- [PlayerMoveState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerMoveState.cs) (新子类)
- [PlayerInAirState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerInAirState.cs) (重命名并实现父类)
- [PlayerJumpState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerJumpState.cs) (新子类)
- [PlayerFallState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerFallState.cs) (新子类)
- [PlayerAbilityState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerAbilityState.cs) (实现父类)
- [PlayerDashState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerDashState.cs) (新子类)
- [PlayerAttackState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerAttackState.cs) (新子类)
- [PlayerWallJumpState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerWallJumpState.cs) (新子类)

请确认以上方案，完成后我将开始分步实施。
