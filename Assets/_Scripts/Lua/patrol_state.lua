-- ============================================================
-- 巡逻状态 - patrol_state.lua
-- 
-- 对外接口（必须实现）：
--   local state = {}
--   state.name = "patrol"
--   function state.Enter(enemy)   -- 进入时调用，enemy 是 C# Enemy 对象
--   function state.LogicUpdate(enemy)  -- 每帧调用，return 下一个状态名 或 nil
--   function state.Exit(enemy)    -- 退出时调用
--   return state
--
-- C# Enemy 上可用的字段/方法（通过 enemy.xxx 访问）：
--   enemy.transform.position     -- 当前位置
--   enemy.rb.velocity            -- Rigidbody2D 速度
--   enemy.startPos               -- 初始位置（Vector3）
--   enemy.chaseRange             -- 追击范围
--   enemy.territoryRange         -- 领地范围
--   enemy.moveSpeed              -- 移动速度
--   enemy:CanSeePlayer()         -- 返回 bool
--   enemy:UpdateVisuals()        -- 更新动画/视觉效果
--   CS.UnityEngine.Time.time     -- 游戏运行时间
--   CS.UnityEngine.Vector2(x, y) -- 创建二维向量
--
-- 提示：可以做余弦振动巡逻，也可以自定义任何巡逻逻辑
local state = {}
state.name = "patrol"

function state.Enter(enemy)

end

function state.LogicUpdate(enemy)
    -- 没有玩家 → 继续巡逻
    if enemy.player == nil then
        return nil
    end
    -- 看不到玩家 → 继续巡逻
    if not enemy:CanSeePlayer() then
        return nil
    end
    -- 离出生点太远 → 不追，继续巡逻
    local distFromStart = math.abs(enemy.transform.position.x - enemy.startPos.x)
    if distFromStart > enemy.territoryRange * 0.5 then
        return nil
    end
    -- 玩家在追击范围内 → 切追击
    local dirToPlayer = enemy.player.transform.position - enemy.transform.position
    if dirToPlayer.sqrMagnitude < enemy.chaseRange * enemy.chaseRange then
        return "chase"
    end
    return nil
end

function state.PhysicsUpdate(enemy)
    enemy:UpdateVisuals()

    local toStartX = enemy.startPos.x - enemy.transform.position.x
    -- 离出生点太远 → 先走回去
    if math.abs(toStartX) > 4 then
        local dirX = toStartX > 0 and 1 or -1
        enemy.rb.velocity = CS.UnityEngine.Vector2(dirX * enemy.moveSpeed * 0.5, enemy.rb.velocity.y)
        return
    end

    -- 正常余弦波巡逻
    local patrolAmplitude = 3
    local maxSpeed = enemy.moveSpeed * 0.5
    if patrolAmplitude <= 0 or maxSpeed <= 0 then
        enemy.rb.velocity = CS.UnityEngine.Vector2(0, enemy.rb.velocity.y)
        return
    end
    local omega = maxSpeed / patrolAmplitude
    local velocityX = maxSpeed * math.cos(CS.UnityEngine.Time.time * omega)
    enemy.rb.velocity = CS.UnityEngine.Vector2(velocityX, enemy.rb.velocity.y)
end

function state.Exit(enemy)

end

return state
