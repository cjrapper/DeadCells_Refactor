-- Logic/Player/State/Abnormal/hurt.lua — 受击（ABNORMAL 层独占）
-- 由 C# 侧 PlayerHealth 受击事件驱动（EventCenter → PlayerLuaBridge → ChangeState(ABNORMAL,"hurt")）
-- 退出语义：return {}（仅清空本层）→ 回到 BASE 当前状态（受击前在哪就回哪）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.ABNORMAL, "hurt")
state.startTime = 0

state.Enter = function(c)
    state.startTime = 0
    c:PlayHurt()   -- 安全播放入口（状态名不存在时静默跳过，不再报 GotoState 错）
end

state.LogicUpdate = function(c)
    -- 受击结束条件：PlayerHealth 不再处于受击状态
    if c.PlayerHealth and not c.PlayerHealth.IsHurting then
        return { }   -- 清空 ABNORMAL 层，恢复 BASE 层运行
    end
    state.startTime = state.startTime + CS.UnityEngine.Time.deltaTime
    return nil
end

return state
