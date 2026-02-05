## 1. 修复逻辑丢失 (空中冲刺、滑墙、蹬墙跳)
### 1.1 开启空中冲刺
- 在 [PlayerInAirState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerInAirState.cs) 的 `LogicUpdate` 中添加对 `LeftShift` 键的检测，使玩家在空中也能切换到 `DashState`。

### 1.2 完善滑墙与蹬墙跳逻辑
- **实现 [PlayerWallSlideState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerWallSlideState.cs)**: 将原本硬编码在空中状态的滑墙逻辑独立出来，处理滑墙速度限制，并监听跳跃键以触发 `WallJumpState`。
- **更新 [PlayerController.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerController.cs)**: 声明并初始化 `WallSlideState` 实例。
- **重构 [PlayerInAirState.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PlayerInAirState.cs)**: 当检测到玩家贴墙且正在下落时，从 `InAirState` (或其子类) 切换到 `WallSlideState`。

## 2. 修复拖影 (Ghost Effect)
### 2.1 脚本层面修复
- 确保 [PLayerEffect.cs](file:///d:/Desktop/Dead_refactor/angryBirds_Refactor/Assets/_Scripts/player/PLayerEffect.cs) 中的 `ghostPool` 引用正确，并检查 `rb.velocity.x > 5f` 的触发阈值是否合适。

### 2.2 Unity 编辑器排查 (需手动操作)
- **解决 "Missing Script" 报错**:
    1. 选中场景中的 **GohstPool** 对象，重新挂载 `SamplePool.cs` 脚本。
    2. 将 `Assets/Prefabs/Capsule.prefab` 拖入 `SamplePool` 的 **Prefab** 槽位。
    3. 打开 `Capsule.prefab` 预制体，移除任何显示为 `Missing (Mono Behaviour)` 的组件。

## 3. 任务清单 (TODO)
- [ ] 在空中状态添加冲刺输入检测
- [ ] 完成 `PlayerWallSlideState` 脚本编写
- [ ] 在 `PlayerController` 中集成滑墙状态
- [ ] 优化状态切换逻辑，确保蹬墙跳和滑墙顺畅衔接
- [ ] 更新 API 笔记

是否按照此方案开始修复？