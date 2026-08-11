-- ============================================================
-- Framework/Fsm/HFSM.lua — 层级状态机调度器（三层并行版）
-- 依赖：State.lua（枚举 + 工厂）。本文件只做调度，不认识玩家。
--
-- 层语义：
--   BASE（常驻层）：永远有一个 current，初始化时自动设为第一个注册状态
--   ACTIVE/ABNORMAL（瞬态层）：默认 current=nil，触发才激活；
--     内部跨层切换时自动清空本层（Exit + current=nil），再切目标层
--
-- 返回约定：状态 LogicUpdate 返回
--   nil              → 停留
--   "stateName"      → 同层切换
--   {layer=, name=}  → 跨层切换（瞬态层切走后自动清空）
-- ============================================================
local State = require("Framework.Fsm.State")

local layers = {
    [State.StateLayer.BASE]     = { current = nil, states = {} },
    [State.StateLayer.ACTIVE]   = { current = nil, states = {} },
    [State.StateLayer.ABNORMAL] = { current = nil, states = {} },
}

-- 注册：状态 → 按 layer 归类。仅 BASE 层自动设初始状态
function Register(state)
    local L = layers[state.layer]
    L.states[state.name] = state
    if L.current == nil and state.layer == State.StateLayer.BASE then
        L.current = state
    end
end

-- 切换：同层内，旧 Exit → 新 Enter
-- 支持 name=nil：清空该层（瞬态层结束）
function ChangeState(layer, name, core)
    local L = layers[layer]
    if L.current and L.current.Exit then L.current.Exit(core) end
    L.current = name and L.states[name] or nil
    if L.current and L.current.Enter then L.current.Enter(core) end
end

-- 解析状态返回值：nil / string / table，统一分发
local function ResolveNext(ownerLayer, next, core)
    if next == nil then return false end
    if type(next) == "table" then
        -- 跨层切换：瞬态层切走后先清空本层
        if ownerLayer ~= State.StateLayer.BASE then
            ChangeState(ownerLayer, nil, core)   -- Exit + current=nil
        end
        ChangeState(next.layer, next.name, core)
    else
        ChangeState(ownerLayer, next, core)
    end
    return true
end

function LogicUpdate(core)
    -- ① ABNORMAL 独占：有受击/死亡，BASE+ACTIVE 全冻结
    local abnormal = layers[State.StateLayer.ABNORMAL]
    if abnormal.current then
        ResolveNext(State.StateLayer.ABNORMAL,
            abnormal.current.LogicUpdate(core), core)
        return   -- 无论是否切换，本帧结束
    end

    -- ② BASE 先跑（检测落地/贴墙/跳跃输入；可跨层切 ACTIVE）
    local base = layers[State.StateLayer.BASE]
    local switched = ResolveNext(State.StateLayer.BASE,
        base.current.LogicUpdate(core), core)

    -- ③ ACTIVE 并行：BASE 刚切进 ACTIVE 的帧跳过，避免同帧误跑
    local active = layers[State.StateLayer.ACTIVE]
    if active.current and not switched then
        if active.current.parallelMode == State.ParallelMode.MUTEX then
            if base.current.SetOutputLocked then
                base.current.SetOutputLocked(core, true)
            end
        end
        local next = active.current.LogicUpdate(core)
        if base.current.SetOutputLocked then
            base.current.SetOutputLocked(core, false)
        end
        ResolveNext(State.StateLayer.ACTIVE, next, core)
    end
end

function PhysicsUpdate(core)
    local abnormal = layers[State.StateLayer.ABNORMAL]
    if abnormal.current then
        if abnormal.current.PhysicsUpdate then
            abnormal.current.PhysicsUpdate(core)
        end
        return
    end
    local base = layers[State.StateLayer.BASE]
    if base.current and base.current.PhysicsUpdate then
        base.current.PhysicsUpdate(core)
    end
    local active = layers[State.StateLayer.ACTIVE]
    if active.current and active.current.PhysicsUpdate then
        active.current.PhysicsUpdate(core)
    end
end

function GetCurrent(layer)
    local cur = layers[layer].current
    return cur and cur.name or "none"
end

return {
    LogicUpdate = LogicUpdate,
    PhysicsUpdate = PhysicsUpdate,
    Register = Register,
    ChangeState = ChangeState,
    GetCurrent = GetCurrent,
}
