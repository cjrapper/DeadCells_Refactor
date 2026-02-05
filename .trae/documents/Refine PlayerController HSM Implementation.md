# 优化 PlayerController 重构代码

## 核心修改内容
根据最新需求，进一步规范 `PlayerController` 的结构，确保其作为“状态机上下文”的职能清晰，并统一物理检测接口。

### 1. 完善 PlayerController.cs
- **组件访问**: 添加 `public Animator Anim { get; private set; }`，并确保 `RB`, `SR`, `TR` 等物理组件均为 public getter。
- **接口规范化**: 
    - 将 `IsGrounded` 属性改为 `public bool CheckGrounded()` 方法。
    - 将 `IsTouchingWall` 属性改为 `public bool CheckTouchingWall()` 方法。
- **逻辑保留**: 严格保留 `Update` 中的 `jumpBufferTimer` 和 `coyoteTimeTimer` 倒计时逻辑。
- **状态机调用**: 确保 `Update` 和 `FixedUpdate` 只负责核心计时和当前状态的生命周期调用。

### 2. 同步更新状态类
- 更新所有状态类（`PlayerGroundedState`, `PlayerInAirState`, `PlayerAbilityState` 等），将原本访问属性的逻辑改为调用方法：
    - `player.IsGrounded` -> `player.CheckGrounded()`
    - `player.IsTouchingWall` -> `player.CheckTouchingWall()`

## 待修改文件列表
- [PlayerController.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerController.cs)
- [PlayerGroundedState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerGroundedState.cs)
- [PlayerInAirState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerInAirState.cs)
- [PlayerAbilityState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerAbilityState.cs)

请确认该细化方案，确认后我将立即执行修改。
