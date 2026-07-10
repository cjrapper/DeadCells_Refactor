-- ============================================================
-- 受伤状态 - hurt_state.lua
-- 
-- 对外接口（必须实现）：
--   state.name = "hurt"
--   state.Enter(enemy) / state.Update(enemy) / state.Exit(enemy)
--
-- 典型逻辑：
--   1. Enter: 设置 hardTime = 0.3，调 enemy:UpdateVisuals()
--   2. Update: 倒计时，到时间 → 有玩家返 "chase"，无玩家返 "patrol"
--   3. Exit: 无需操作
--
-- 提示：敌人受击闪红由 C# Enemy.TakeDamage() 里的 FlashRed() 协程处理，
--   Lua 端只需负责硬直计时。
local state = {}
state.name="hurt"
local hurtTimer
local hurtDuration=0.3

function state.Enter(enemy)
    hurtTimer=hurtDuration
    enemy:UpdateVisuals()
    
end
function state.Update(enemy)
    hurtTimer=hurtTimer-CS.UnityEngine.Time.deltaTime
    if hurtTimer<=0 then
        if enemy.player ~=nil then
            return "chase"
        else 
            return "patrol"
        end
    end
    return nil
end

return state
