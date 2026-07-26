-- ============================================================
-- 追击状态 - chase_state.lua
-- 
-- 对外接口（必须实现）：
--   state.name = "chase"
--   state.Enter(enemy) / state.Update(enemy) / state.Exit(enemy)
--
-- C# Enemy 上可用的额外字段：
--   enemy.player                 -- 玩家 Transform（可能为 nil）
--   enemy.player.position        -- 玩家位置
--   enemy.chaseSpeed             -- 追击速度
--   enemy.attackRange            -- 攻击范围
--   enemy:CanAttack()            -- 攻击冷却就绪？
--   enemy.transform.localScale   -- 用于翻转朝向
--
-- 典型逻辑：
--   1. 玩家为 nil → 返回 "patrol"
--   2. 距离 > chaseRange → 返回 "patrol"  
--   3. 距离 < attackRange*1.5 且可攻击 → 返回 "telegraph"
--   4. 否则 → 向玩家移动 + 翻转朝向，返回 nil
local state={}
state.name="chase"

function state.Enter(enemy)
    
end

function state.PhysicsUpdate(enemy)
    if enemy.player ==nil then
        return
    end
    enemy:UpdateVisuals()
    local dir = (enemy.player.position - enemy.transform.position).normalized
    enemy.rb.velocity = CS.UnityEngine.Vector2(dir.x*enemy.chaseSpeed,enemy.rb.velocity.y)
end 

function state.LogicUpdate(enemy)
    if enemy.player ==nil then
        return "patrol"
    end
    if not enemy:CanSeePlayer() then
        return "patrol"
    end
    local diff = enemy.transform.position-enemy.player.position
    local distSqr = diff.sqrMagnitude
    local chaseRangeSqr = enemy.chaseRange * enemy.chaseRange
    local distFromStarX = math.abs(enemy.transform.position.x -enemy.startPos.x)
    if distSqr>chaseRangeSqr or distFromStarX>enemy.territoryRange then
        return "patrol"
    end
    local attackRangeSqr = enemy.attackRange * enemy.attackRange
    if distSqr < attackRangeSqr and enemy:CanAttack() then
        return "telegraph"
    end
    return nil
end 

function state.Exit(enemy)
    
end 

return state