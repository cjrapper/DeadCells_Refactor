-- Logic/Player/State/Base/ground.lua — 地上（待机 + 移动）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "ground")
state.wasMoving = false

state.Enter = function(c)
    state.wasMoving = false
    c:SetVelocityX(0)
    c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
    if c.Animator then c.Animator:CrossFade("Idle", 0.1) end
end

state.LogicUpdate = function(c)
    local moving = c.InputX ~= 0

    if moving then
        if not state.wasMoving then
            state.wasMoving = true
            if c.Animator then c.Animator:CrossFade("Move", 0.1) end
        end
        c:CheckFlip()
        c:SetVelocityX(c.PlayerMovement.moveSpeed * c.InputX)
    else
        if state.wasMoving then
            state.wasMoving = false
            if c.Animator then c.Animator:CrossFade("Idle", 0.1) end
        end
        c:SetVelocityX(0)
    end

    -- 单向平台下落
    if c.PlayerInput.VerticalInput < 0 and c.PlayerInput.JumpDown then
        c:StartPlatformDrop()
        return nil
    end

    -- 跳跃：先给速度，air.Enter 看速度方向区分跳跃/掉落
    if c.PlayerInput.JumpDown then
        c:SetVelocityY(c.PlayerMovement.jumpForce)
        return "air"
    end

    -- 攻击 → ACTIVE 层（跨层）
    if c.PlayerInput.AttackDown then
        return { layer = State.StateLayer.ACTIVE, name = "attack" }
    end

    -- 冲刺 → ACTIVE 层（跨层）
    if c.PlayerInput.DashDown and c.PlayerMovement.DashCooldownTimer <= 0 then
        return { layer = State.StateLayer.ACTIVE, name = "dash" }
    end

    -- 离开地面
    if not c:CheckGrounded() then
        return "air"
    end

    return nil
end

return state
