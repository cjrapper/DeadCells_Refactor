-- ============================================================
-- 攻击状态 - attack_state.lua
-- 从 C# AttackState.cs 逐行翻译而来，供学习参考
-- ============================================================

-- 每个状态文件最终 return 一个 table，包含 name / Enter / Update / Exit
local state = {}
state.name = "attack"

-- 局部变量：状态内部的计时器、标记，用 local 声明不会被其他状态污染
local timer = 0
local hasHit = false

-- ============ Enter：进入状态时调用一次 ============
function state.Enter(enemy)
    -- 攻击动作持续多久
    timer = enemy.attackDuration

    -- 还没打到人
    hasHit = false

    -- 注册攻击冷却（C# 方法，用冒号调用）
    enemy:RegisterAttack()

    -- 冲刺：向玩家方向爆发位移
    if enemy.player ~= nil then
        -- 计算玩家方向
        local dx = enemy.player.position.x - enemy.transform.position.x
        local dir = (dx > 0) and 1 or -1           -- Lua 的三元运算符写法

        -- 设置冲刺速度（X轴爆发，Y轴保持原速）
        enemy.rb.velocity = CS.UnityEngine.Vector2(
            dir * enemy.lungeSpeed,                 -- X 方向冲刺
            enemy.rb.velocity.y                     -- Y 方向不变
        )
    end
end

-- ============ Update：每帧调用，return 下一个状态名 ============
function state.Update(enemy)
    -- 倒计时
    timer = timer - CS.UnityEngine.Time.deltaTime   -- 注意 CS.UnityEngine.Time，不是 UnityEngine.Time

    -- 攻击判定只做一次，防止每帧都命中
    if not hasHit then
        hasHit = DoAttack(enemy)                    -- 把判定逻辑抽成局部函数，保持 Update 干净
    end

    -- 时间到了就退出
    if timer <= 0 then
        if enemy.player ~= nil then
            return "chase"                           -- 玩家还在，继续追
        else
            return "patrol"                          -- 玩家丢了，回去巡逻
        end
    end

    -- return nil 表示保持当前状态不变
    return nil
end

-- ============ Exit：退出状态时调用一次 ============
function state.Exit(enemy)
    -- 停止冲刺惯性
    enemy.rb.velocity = CS.UnityEngine.Vector2(0, enemy.rb.velocity.y)
end

-- ============ 局部函数：AABB 攻击判定 ============
function DoAttack(enemy)
    if enemy.player == nil then return false end

    -- Lua 里 GetComponent 用字符串参数
    -- xLua 会自动找泛型版本，但你也可以直接传类型名
    local playerCol = enemy.player:GetComponent("UnityEngine.Collider2D")
    if playerCol == nil then return false end

    -- 获取玩家的包围盒
    local playerBounds = playerCol.bounds

    -- 敌人的攻击包围盒：优先用 bodyCollider，没有就用 attackPos
    local attackBounds
    if enemy.bodyCollider ~= nil then
        attackBounds = enemy.bodyCollider.bounds
    elseif enemy.attackPos ~= nil then
        -- 自己构建一个 Bounds（圆心 + 半径 → 正方形）
        local r = enemy.attackRange
        local center = enemy.attackPos.position
        attackBounds = CS.UnityEngine.Bounds(
            center,
            CS.UnityEngine.Vector3(r * 2, r * 2, 0)
        )
    else
        return false
    end

    -- AABB 重叠检测：两个矩形的 min/max 交叉即命中
    if attackBounds.min.x <= playerBounds.max.x
       and attackBounds.max.x >= playerBounds.min.x
       and attackBounds.min.y <= playerBounds.max.y
       and attackBounds.max.y >= playerBounds.min.y then

        -- 命中了！通过 IDamageable 接口造成伤害
        local dmgTarget = enemy.player:GetComponent("AngryBirds.Core.IDamageable")
        if dmgTarget ~= nil then
            local pos = enemy:GetBackCenter()        -- C# 方法
            -- TakeDamage(伤害值, 来源位置, 击退力度)
            dmgTarget:TakeDamage(enemy.damage, pos, 10)
            return true
        end
    end

    return false
end

-- ============ 最终 return state table（必须） ============
return state
