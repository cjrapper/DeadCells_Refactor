### 2026.01.26 开发日志：敌人程序化动画
**功能名称：** Q版史莱姆弹性动画 (Procedural Squash & Stretch)
**核心目的：** 解决白模美术表现力不足的问题，通过代码实现物理质感的动态反馈，提升游戏“生命力”。
####  技术实现要点：
1.  **正弦驱动 (Sine Wave)**
    *   **原理：** 利用 `Mathf.Sin(Time.time * frequency)` 生成一个在 -1 到 1 之间连续波动的信号。
    *   **应用：** 用这个信号作为形变的“驱动力”，控制缩放的幅度。
2.  **体积守恒 (Volume Preservation)**
    *   **物理直觉：** 当物体被拉长（Y轴变大）时，必须变细（X轴变小），反之亦然。
    *   **公式逻辑：** `Scale.y = 1 + factor`，`Scale.x = 1 - factor`。
3.  **平滑过渡 (Smooth Transition)**
    *   **问题：** 当敌人停止移动时，Sin 波形可能正处于形变状态，直接归零会造成画面跳变。
    *   **解决：** 使用 `Vector3.Lerp` 进行插值。
    *   **代码应用：**
        ```csharp
        // 每一帧让当前 Scale 向目标 Scale (1,1,1) 靠近 10%
        // Time.deltaTime * speed 决定了归位的快慢
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 5f);
        ```
#### 📺 效果展示：
*(此处插入你的视频/GIF)*




Lerp 在 Update 里用： 产生减速缓冲效果（适合相机、UI）。
MoveTowards 在 Update 里用： 产生匀速效果（适合子弹、电梯）