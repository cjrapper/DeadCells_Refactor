-- ============================================================
-- 敌人状态机调度器 - enemy_fsm.lua
-- 角色等同于 C# 的 EnemyStateMachine：管理当前状态、切换状态、每帧驱动
-- Bridge 通过全局函数名找到这5个函数，所以它们必须是全局的
-- ============================================================

-- 内部变量（不暴露给外部）
local states = {}           -- { ["patrol"] = patrol_state表, ["chase"] = chase_state表, ... }
local currentState = nil    -- 当前状态的表引用
local enemy = nil           -- C# Enemy 对象引用

-- ============================================================
-- Init：Bridge 在 Start 时调用一次，传入 Enemy 对象和状态模块列表。
-- 状态模块导出工厂函数（require 后调用一次），每只敌人独立实例，
-- 避免多只敌人共享同一状态表的计时器/标记。
-- ============================================================
function Init(e, moduleList)
    enemy = e
    for _, moduleName in ipairs(moduleList) do
        local factory = require(moduleName)   -- 加载 patrol_state.lua 等，得到工厂函数
        local stateModule = factory()         -- 每只敌人独立的状态实例
        states[stateModule.name] = stateModule -- 用 state.name 做 key，如 states["patrol"]
    end
end

-- ============================================================
-- ChangeState：切换状态。Bridge 初始切 patrol，各状态 Update 返回字符串时也走这里
-- ============================================================
function ChangeState(name)
    if currentState and currentState.Exit then
        currentState.Exit(enemy)    -- 旧状态退场
    end
    currentState = states[name]     -- 换人
    if currentState and currentState.Enter then
        currentState.Enter(enemy)   -- 新状态登场
    end
end

-- ============================================================
-- LogicUpdate：Bridge 每帧 Update 调用。调度当前状态的思考逻辑
-- ============================================================
function LogicUpdate()
    if currentState and currentState.LogicUpdate then
        local nextState = currentState.LogicUpdate(enemy)
        if nextState ~= nil then
            ChangeState(nextState)     -- 状态自己决定切换（如 patrol 发现玩家 → "chase"）
        end
    end
end

-- ============================================================
-- PhysicsUpdate：Bridge 每帧 FixedUpdate 调用。调度当前状态的物理移动
-- ============================================================
function PhysicsUpdate()
    if currentState and currentState.PhysicsUpdate then
        currentState.PhysicsUpdate(enemy)
    end
end

-- ============================================================
-- GetCurrentStateName：调试用，返回当前状态名字符串
-- ============================================================
function GetCurrentStateName()
    if currentState and currentState.name then
        return currentState.name
    end
    return "none"
end
