-- Logic/Player/State/Active/attack.lua — 攻击（ACTIVE 层，与 BASE 并行）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.ACTIVE, "attack")
state.parallelMode = State.ParallelMode.ALLOW   -- 移动攻击可并行
state.startTime = 0

state.Enter = function(c)
    state.startTime = 0
    if c:CheckGrounded() then c:SetVelocityX(0) end
    c:Attack()
end

state.LogicUpdate = function(c)
    local swingDuration = c.PlayerCombat.swingDuration or 0.25
    if state.startTime >= swingDuration + 0.1 then
        -- 攻击结束 → 回 BASE 层（落地 ground / 空中 air）
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
