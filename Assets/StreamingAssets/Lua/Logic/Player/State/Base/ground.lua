-- Logic/Player/State/Base/ground.lua — 地上（待机 + 移动）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "ground")
state.wasMoving = false
state.outputLocked = false

state.Enter = function(c)
    state.wasMoving = false
    state.outputLocked = false
    c:SetVelocityX(0)
    c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
    c:PlayIdle()   -- 安全播放入口（状态名在 Animator 不存在时静默跳过，不报 GotoState 错）
end

-- MUTEX 互斥（冲刺）期间由 HFSM 每帧调用：锁定本层位移/技能输出，
-- 落地检测不受影响（见 HFSM.LogicUpdate ②）
state.SetOutputLocked = function(c, locked)
    state.outputLocked = locked
end

state.LogicUpdate = function(c)
    -- 冲刺冷却计时（锁定期间也照常递减）
    if c.PlayerMovement.DashCooldownTimer > 0 then
        c.PlayerMovement.DashCooldownTimer =
            c.PlayerMovement.DashCooldownTimer - CS.UnityEngine.Time.deltaTime
    end

    if not state.outputLocked then
        -- 爬梯：在梯子范围内 + 纵向意图 + "这一按真的能爬"
        --   ↑/W → 总能上梯
        --   ↓/S → 只有脚下还有梯子时才进（在空中，或梯子还没爬到底）
        -- ★ 不能写成 VerticalInput ~= 0 就进：站在梯子底端的地面上按 ↓ 时，
        --   ladder.lua 的"触地自动脱离"（grounded 且 ↓）会立刻把状态切回 ground，
        --   而 ground 又立刻切进 ladder —— 两者每帧互相打架，一秒切 60 次（状态机抖动）。
        --   根因是"进入条件"和"退出条件"在同一帧同时成立；转换条件必须互斥（或带迟滞）。
        if c.PlayerClimb and c.PlayerClimb.IsOnLadder then
            local v = c.PlayerInput.VerticalInput
            if v > 0 then
                return "ladder"
            elseif v < 0 and (not c:CheckGrounded() or c.PlayerClimb.HasLadderBelow) then
                return "ladder"
            end
        end

        local moving = c.InputX ~= 0

        if moving then
            if not state.wasMoving then
                state.wasMoving = true
                c:PlayMove()
            end
            c:CheckFlip()
            c:SetVelocityX(c.PlayerMovement.moveSpeed * c.InputX)
        else
            if state.wasMoving then
                state.wasMoving = false
                c:PlayIdle()
            end
            c:SetVelocityX(0)
        end

        -- 单向平台下落（消费跳跃缓冲：下落不算跳跃，否则缓冲窗口内会自动起跳）
        if c.PlayerInput.VerticalInput < 0 and c.PlayerInput.JumpDown then
            c:StartPlatformDrop()
            c:ConsumeJumpBuffer()
            return nil
        end

        -- 跳跃：JumpBuffer 预输入（落地前 0.2s 内按跳也算）+ 本帧按键
        if c.JumpBufferTimer > 0 then
            c:SetVelocityY(c.PlayerMovement.jumpForce)
            return "air"
        end

        -- 攻击 → ACTIVE 层（跨层）。CanAttack：单点/长按都要过冷却（攻击频率 = 冷却，
        -- 与点击方式无关——修"单点绕过后摇"的不合理感）
        if c.PlayerInput.AttackDown and c:CanAttack() then
            return { layer = State.StateLayer.ACTIVE, name = "attack" }
        end

        -- 冲刺 → ACTIVE 层（跨层）
        if c.PlayerInput.DashDown and c.PlayerMovement.DashCooldownTimer <= 0 then
            return { layer = State.StateLayer.ACTIVE, name = "dash" }
        end
    end

    -- 离开地面（锁定期间也检测：互斥中落地/离地切换是物理事实）
    if not c:CheckGrounded() then
        return "air"
    end

    return nil
end

return state
