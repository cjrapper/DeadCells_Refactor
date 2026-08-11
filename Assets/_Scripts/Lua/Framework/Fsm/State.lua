-- ============================================================
-- Framework/Fsm/State.lua — 状态工厂 + 枚举
-- 枚举挂在模块上：require 后通过 State.StateLayer / State.ParallelMode 访问
-- ============================================================
local M = {}

-- 层级枚举：决定并行归属
M.StateLayer = { BASE = 1, ACTIVE = 2, ABNORMAL = 3 }

-- 并存策略：ACTIVE 状态与 BASE 并行时的行为
M.ParallelMode = { ALLOW = 2, MUTEX = 3 }   -- CONFIG=1 预留

-- 状态工厂：产出标准状态 table
function M.NewState(layer, name)
    return {
        layer = layer,
        name  = name,
        parallelMode = M.ParallelMode.ALLOW,   -- 默认并行，dash 等改成 MUTEX
        Enter = function(core) end,
        Exit  = function(core) end,
        LogicUpdate  = function(core) return nil end,
        PhysicsUpdate = function(core) end,
        SetOutputLocked = function(core, locked) end,
    }
end

return M
