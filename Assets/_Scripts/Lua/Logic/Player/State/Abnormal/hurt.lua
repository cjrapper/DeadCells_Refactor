-- Logic/Player/State/Abnormal/hurt.lua — 受击（ABNORMAL 层独占）
-- 注意：目前 C# PlayerController.Update 里已有 IsHurting 短路，
--       本状态作为 ABNORMAL 层的占位，真正的受击驱动在 C# 侧。
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.ABNORMAL, "hurt")
state.startTime = 0

state.Enter = function(c)
    state.startTime = 0
    if c.Animator then c.Animator:CrossFade("Hurt", 0.05) end
end

state.LogicUpdate = function(c)
    -- 受击结束条件：PlayerHealth 不再处于受击状态
    if c.PlayerHealth and not c.PlayerHealth.IsHurting then
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
