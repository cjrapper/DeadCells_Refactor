-- Logic/Player/State/Active/dash.lua — 冲刺（ACTIVE 层，互斥锁 BASE 输出）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.ACTIVE, "dash")
state.parallelMode = State.ParallelMode.MUTEX   -- 冲刺锁位移
state.startTime = 0

state.Enter = function(c)
    state.startTime = 0
    if c.Animator then c.Animator:CrossFade("Dash", 0) end
    c.PlayerMovement.DashCooldownTimer = c.PlayerMovement.dashCooldown
    local dir = c.transform.localScale.x > 0 and 1 or -1
    c:SetVelocity(dir * c.PlayerMovement.dashSpeed, 0)
    c.RigidBody.gravityScale = 0
end

state.Exit = function(c)
    if c.PlayerMovement then
        c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
    end
    c:SetVelocityX(0)
end

state.LogicUpdate = function(c)
    -- 冲刺结束 → 回 BASE 层（落地 ground / 空中 air，由 HFSM 判断）
    if c.PlayerMovement.dashTime and state.startTime >= c.PlayerMovement.dashTime then
        -- 跨层回 BASE：落地 ground / 空中 air
        if c:CheckGrounded() then
            return { layer = State.StateLayer.BASE, name = "ground" }
        else
            return { layer = State.StateLayer.BASE, name = "air" }
        end
    end
    state.startTime = state.startTime + CS.UnityEngine.Time.deltaTime
    return nil
end

return state
