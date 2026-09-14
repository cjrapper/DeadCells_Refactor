-- Logic/Player/State/Active/dash.lua — 冲刺（ACTIVE 层，互斥锁 BASE 输出）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.ACTIVE, "dash")
state.parallelMode = State.ParallelMode.MUTEX   -- 冲刺锁位移
state.startTime = 0

state.Enter = function(c)
    state.startTime = 0
    -- 进入冲刺立即进入冷却（ground/air 每帧递减）
    c.PlayerMovement.DashCooldownTimer = c.PlayerMovement.dashCooldown
    c:PlayDash()   -- 安全播放入口

    -- 冲刺无敌帧：敌人不锁攻击、物理穿过敌人
    c.IsDashing = true
    c:SetDashInvincible(true)

    -- 冲刺方向：
    local dir = c.transform.localScale.x > 0 and 1 or -1
    c:SetVelocity(dir * c.PlayerMovement.dashSpeed, 0)
    c.RigidBody.gravityScale = 0
end

state.Exit = function(c)
    -- 取消冲刺无敌帧（恢复与敌人的碰撞）
    c.IsDashing = false
    c:SetDashInvincible(false)

    -- 退出冲刺：恢复重力为配置值
    if c.PlayerMovement then
        c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
    end
    c:SetVelocityX(0)
end

state.LogicUpdate = function(c)
    if state.startTime >= c.PlayerMovement.dashTime then
        -- 冲刺结束：落地回 ground，空中回 air
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