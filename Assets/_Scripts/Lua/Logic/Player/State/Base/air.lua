-- Logic/Player/State/Base/air.lua — 空中（上升 + 下落）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "air")

-- 空跳/Coyote 跳（从边缘掉落时补救）
state.ApplyJump = function(c)
    c:SetVelocityY(c.PlayerMovement.jumpForce)
    c.RigidBody.gravityScale = c.PlayerMovement.risingGravityScale
    if c.Animator then c.Animator:CrossFade("Jump", 0.1) end
    c:SpawnDust(c.PlayerVFX and c.PlayerVFX.JumpDustPrefab)
end

state.Enter = function(c)
    -- ground 已在切出前给好 jumpForce，速度向上 = 跳跃，向下/零 = 掉落
    if c.RigidBody.velocity.y > 0 then
        c.RigidBody.gravityScale = c.PlayerMovement.risingGravityScale
        if c.Animator then c.Animator:CrossFade("Jump", 0.1) end
        c:SpawnDust(c.PlayerVFX and c.PlayerVFX.JumpDustPrefab)
    else
        c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
        if c.Animator then c.Animator:CrossFade("Fall", 0.1) end
    end
end

state.LogicUpdate = function(c)
    -- 水平移动（空中可操控）
    c:CheckFlip()
    c:SetVelocityX(c.PlayerMovement.moveSpeed * c.InputX)

    -- 上升/下降段重力切换
    local vy = c.RigidBody.velocity.y
    if vy > 0 and c.RigidBody.gravityScale ~= c.PlayerMovement.risingGravityScale then
        c.RigidBody.gravityScale = c.PlayerMovement.risingGravityScale
    elseif vy < 0 and c.RigidBody.gravityScale ~= c.PlayerMovement.fallingGravityScale then
        c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
    end

    -- 落地
    if c:CheckGrounded() then
        c:SpawnDust(c.PlayerVFX and c.PlayerVFX.LandDustPrefab)
        return "ground"
    end

    -- 贴墙（面向墙 + 按墙方向）
    if c:CheckTouchingWall() and c.InputX == c.transform.localScale.x then
        return "wall_slide"
    end

    -- Coyote Jump（离地缓冲窗口内按跳）
    if c.CoyoteTimeCounter > 0 and c.PlayerInput.JumpDown then
        state.ApplyJump(c)
        return nil
    end

    -- 空中冲刺
    if c.PlayerInput.DashDown and c.PlayerMovement.DashCooldownTimer <= 0 then
        return { layer = State.StateLayer.ACTIVE, name = "dash" }
    end

    -- 空中攻击
    if c.PlayerInput.AttackDown then
        return { layer = State.StateLayer.ACTIVE, name = "attack" }
    end

    return nil
end

return state
