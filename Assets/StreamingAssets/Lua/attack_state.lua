-- ============================================================
-- 攻击状态 - attack_state.lua
-- 从 C# AttackState.cs 逐行翻译而来，供学习参考
--
-- 导出「工厂函数」：每只敌人独立实例（timer / hasHit 在闭包内，互不污染）
-- ============================================================

return function()
    local state = {}
    state.name = "attack"

    -- 局部变量：状态内部的计时器、标记，工厂闭包内声明，每只敌人一份
    local timer = 0
    local hasHit = false
    local dashDir = 1     -- 冲刺方向（Enter 时锁定）

    -- ============ Enter：进入状态时调用一次 ============
    function state.Enter(enemy)
        -- 攻击动作持续多久
        timer = enemy.attackDuration

        -- 还没打到人
        hasHit = false

        -- 注册攻击冷却（C# 方法，用冒号调用）
        enemy:RegisterAttack()

        -- 冲刺：向玩家方向爆发位移，并锁定方向（冲撞不跟手变向）
        if enemy.player ~= nil then
            local dx = enemy.player.position.x - enemy.transform.position.x
            local dir = (dx > 0) and 1 or -1           -- Lua 的三元运算符写法
            dashDir = dir

            -- 设置冲刺速度（X轴爆发，Y轴保持原速）
            enemy.rb.velocity = CS.UnityEngine.Vector2(
                dir * enemy.lungeSpeed,                 -- X 方向冲刺
                enemy.rb.velocity.y                     -- Y 方向不变
            )
        end
    end

    -- ============ 局部函数：AABB 攻击判定 ============
    -- 注意：必须先定义再使用！Lua 词法作用域下，local 声明之后的代码才可见；
    -- 若声明在 LogicUpdate 后面，LogicUpdate 里的 DoAttack 会解析成全局 nil（前向引用坑）
    local function DoAttack(enemy)
        if enemy.player == nil then return false end

        -- 冲撞判定：检测点朝冲刺方向前移半个判定半径（"撞头"优先判定，更灵敏），
        -- 半径 = attackRange。IgnoreCollision 不影响 OverlapCircle 查询。
        -- 命中即停：撞到就清零速度，形成"冲撞"手感（而不是推着玩家跑）
        local center = enemy.transform.position
        center = CS.UnityEngine.Vector2(
            center.x + dashDir * enemy.attackRange * 0.5,
            center.y)
        local hit = CS.UnityEngine.Physics2D.OverlapCircle(
            center, enemy.attackRange, enemy.playerLayer)
        if hit ~= nil then
            -- 命中了！通过 IDamageable 接口造成伤害
            local dmgTarget = hit:GetComponent("DeadCells.Core.IDamageable")
            if dmgTarget ~= nil then
                local pos = enemy:GetBackCenter()        -- C# 方法
                -- TakeDamage(伤害值, 来源位置, 击退力度)
                dmgTarget:TakeDamage(enemy.damage, pos, 10)
                enemy.rb.velocity = CS.UnityEngine.Vector2(0, enemy.rb.velocity.y)  -- 撞到即停
                return true
            end
        end

        return false
    end

    -- ============ Update：每帧调用，return 下一个状态名 ============
    function state.LogicUpdate(enemy)
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

    -- ============ 返回状态实例（必须） ============
    return state
end
