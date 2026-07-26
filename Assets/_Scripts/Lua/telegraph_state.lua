-- ============================================================
-- 预警状态 - telegraph_state.lua
-- 
-- 对外接口（必须实现）：
--   state.name = "telegraph"
--   state.Enter(enemy) / state.LogicUpdate(enemy) / state.Exit(enemy)
--
-- C# Enemy 上可用的额外字段：
--   enemy.windupTime            -- 预警持续时间（秒）
--   enemy.alertSign             -- 警示标志 GameObject（可能为 nil）
--   enemy.alertSign:SetActive(bool)  -- 显示/隐藏
--   enemy.rb.velocity           -- 减速到 0
--
-- 典型逻辑：
--   1. Enter: 显示 alertSign，清零速度
--   2. LogicUpdate: 倒计时，到时间 → 返回 "attack"
--   3. Exit: 隐藏 alertSign
-- ============================================================
local state = {}
state.name = "telegraph"
local timer = 0

function state.Enter(enemy)
    timer = enemy.windupTime
    if enemy.alertSign ~= nil then
        enemy.alertSign:SetActive(true)
    end
    enemy.rb.velocity = CS.UnityEngine.Vector2(0, enemy.rb.velocity.y)
end

function state.LogicUpdate(enemy)
    timer = timer - CS.UnityEngine.Time.deltaTime
    if timer <= 0 then
        return "attack"
    end
    return nil
end

function state.Exit(enemy)
    if enemy.alertSign ~= nil then
        enemy.alertSign:SetActive(false)
    end
end

return state
