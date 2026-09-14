-- ============================================================
-- 追击状态 - chase_state.lua
--
-- 对外接口（必须实现）：
--   本模块导出「工厂函数」：每只敌人独立调用一次，产出独立状态实例。
--   local state = factory()
--   state.name = "chase"
--   state.Enter(enemy) / state.LogicUpdate(enemy) / state.Exit(enemy)
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
return function()
    local state = {}
    state.name = "chase"

    function state.Enter(enemy)

    end

    function state.PhysicsUpdate(enemy)
        if enemy.player == nil then
            return
        end
        enemy:UpdateVisuals()

        -- 进入攻击距离：刹停！等待冷却 → 蓄力 → 攻击，不再贴脸推玩家
        -- （与 C# ChaseState 同款修复：之前冷却期间继续追击 = "推着玩家走"的真因）
        local attackRangeSqr = enemy.attackRange * enemy.attackRange
        local diff = enemy.transform.position - enemy.player.position
        if diff.sqrMagnitude <= attackRangeSqr then
            enemy.rb.velocity = CS.UnityEngine.Vector2(0, enemy.rb.velocity.y)
            return
        end

        local dir = (enemy.player.position - enemy.transform.position).normalized
        enemy.rb.velocity = CS.UnityEngine.Vector2(dir.x * enemy.chaseSpeed, enemy.rb.velocity.y)
    end

    function state.LogicUpdate(enemy)
        if enemy.player == nil then
            return "patrol"
        end
        if not enemy:CanSeePlayer() then
            return "patrol"
        end

        -- 冲刺无敌帧：玩家冲刺中不触发攻击（停下来等）
        local pc = enemy.player:GetComponent("DeadCells.Player.PlayerController")
        if pc ~= nil and pc.IsDashing then
            return nil
        end

        local diff = enemy.transform.position - enemy.player.position
        local distSqr = diff.sqrMagnitude
        local chaseRangeSqr = enemy.chaseRange * enemy.chaseRange
        local distFromStarX = math.abs(enemy.transform.position.x - enemy.startPos.x)
        if distSqr > chaseRangeSqr or distFromStarX > enemy.territoryRange then
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
end
