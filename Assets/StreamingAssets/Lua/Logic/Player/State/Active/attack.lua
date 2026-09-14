-- Logic/Player/State/Active/attack.lua — 攻击（ACTIVE 层，与 BASE 并行）
-- 两种武器形态：
--   ① 普通武器（近战挥刀等）：挥刀（swingDuration）→ 后摇（RecoveryTime）→ 回 BASE；
--      长按攻击且冷却就绪 → 后摇结束后无缝连击（近战不能高频：后摇在，连击有节奏）
--   ② 持续武器（激光 channel）：按住持续发射（FireInterval 一枪）→ 松开进后摇 recovery → 回 BASE
-- ★ 武器切换检测：攻击中途换武器（Q）→ 整段攻击重置为新武器段（不残留旧段/旧动画）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.ACTIVE, "attack")
state.parallelMode = State.ParallelMode.ALLOW   -- 移动攻击可并行
state.startTime = 0
state.channelPhase = "none"    -- "none" | "channel" | "recovery"
state.fireTimer = 0
state.recoverTimer = 0
state.enteredWeapon = nil      -- 进入攻击时的武器引用（检测切换用）

state.Enter = function(c)
    state.startTime = 0
    state.enteredWeapon = c.PlayerCombat and c.PlayerCombat.currentWeapon
    if c:CheckGrounded() then c:SetVelocityX(0) end

    local weapon = c.PlayerCombat and c.PlayerCombat.currentWeapon
    if weapon and weapon.IsChannelAttack then
        state.channelPhase = "channel"
        state.fireTimer = 0          -- 立刻发射第一发
        state.recoverTimer = 0
        -- 动画职责归属：只在此处播一次（持续武器进入时），发射过程不再动动画
        -- （之前每帧 CrossFade/IsName 检查导致动画被重置/延迟，别再放回发射逻辑里）
        -- 走安全入口：武器配的动画名在 Animator 不存在时静默跳过
        if weapon.ChannelAnimName ~= "" then
            c:PlayAttack(weapon.ChannelAnimName)
        end
    else
        state.channelPhase = "none"  -- 普通武器：不播 Animator（近战靠挥刀表现）
    end

    -- 攻击期间标记（PlayerController 用它禁用武器切换）
    if c.PlayerCombat then c.PlayerCombat.IsAttacking = true end

    c:Attack()
end

-- 攻击状态退出：清标记（攻击结束才恢复切换武器能力）
state.Exit = function(c)
    if c.PlayerCombat then c.PlayerCombat.IsAttacking = false end
end

state.LogicUpdate = function(c)
    local weapon = c.PlayerCombat and c.PlayerCombat.currentWeapon

    -- ★ 武器切换检测：攻击状态里换了武器 → 整段重开（旧段完全重置，动画不残留）
    if weapon ~= nil and weapon ~= state.enteredWeapon then
        state.enteredWeapon = weapon
        state.startTime = 0
        state.fireTimer = 0
        state.recoverTimer = 0
        state.channelPhase = weapon.IsChannelAttack and "channel" or "none"
        c:Attack()
        return nil
    end

    -- ============ 持续武器：channel / recovery 两阶段 ============
    if state.channelPhase ~= "none" then
        if state.channelPhase == "channel" then
            if c.PlayerInput.AttackHeld then
                state.fireTimer = state.fireTimer - CS.UnityEngine.Time.deltaTime
                if state.fireTimer <= 0 then
                    c:Attack()
                    state.fireTimer = weapon and weapon.FireInterval or 0.15
                end
                -- 动画不再在此处重置（Enter 播一次后持续保持）
            else
                -- 松开攻击键 → 后摇（播结束动画）
                if weapon ~= nil then weapon:PlayRecoveryAnim(c) end
                state.channelPhase = "recovery"
                state.recoverTimer = weapon and weapon.RecoveryTime or 0.3
            end
        elseif state.channelPhase == "recovery" then
            state.recoverTimer = state.recoverTimer - CS.UnityEngine.Time.deltaTime
            if state.recoverTimer <= 0 then
                state.channelPhase = "none"
                if c:CheckGrounded() then
                    return { layer = State.StateLayer.BASE, name = "ground" }
                else
                    return { layer = State.StateLayer.BASE, name = "air" }
                end
            end
        end
        return nil
    end

    -- ============ 普通武器：挥刀 + 后摇，长按自动连击 ============
    local swingDuration = c.PlayerCombat.swingDuration or 0.25
    local recoveryTime = weapon and weapon.RecoveryTime or 0.3   -- 近战后摇（不可取消）
    if state.startTime >= swingDuration + recoveryTime then
        -- 后摇结束后：长按仍在按住且冷却好 → 无缝连击；否则回 BASE
        if c.PlayerInput.AttackHeld and c:CanAttack() then
            state.startTime = 0
            c:Attack()
            return nil
        end
        if c:CheckGrounded() then
            return { layer = State.StateLayer.BASE, name = "ground" }
        else
            return { layer = State.StateLayer.BASE, name = "air" }
        end
    end
    state.startTime = state.startTime + CS.UnityEngine.Time.deltaTime
    return nil
end

return state
