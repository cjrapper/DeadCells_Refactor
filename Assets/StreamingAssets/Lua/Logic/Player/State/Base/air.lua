-- Logic/Player/State/Base/air.lua — 空中（上升 + 下落）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "air")
state.outputLocked = false

-- 空跳/Coyote 跳（从边缘掉落时补救）
state.ApplyJump = function(c)
    c:SetVelocityY(c.PlayerMovement.jumpForce)
    c.RigidBody.gravityScale = c.PlayerMovement.risingGravityScale
    c:PlayJump()   -- 安全播放入口
    c:SpawnDust(c.PlayerVFX and c.PlayerVFX.JumpDustPrefab)
end

-- MUTEX 互斥（冲刺）期间由 HFSM 每帧调用：锁定本层位移/技能输出，
-- 落地/贴墙检测不受影响（见 HFSM.LogicUpdate ②）
state.SetOutputLocked = function(c, locked)
    state.outputLocked = locked
end

state.Enter = function(c)
    state.outputLocked = false
    -- ground 已在切出前给好 jumpForce，速度向上 = 跳跃，向下/零 = 掉落
    if c.RigidBody.velocity.y > 0 then
        c.RigidBody.gravityScale = c.PlayerMovement.risingGravityScale
        c:PlayJump()
        c:SpawnDust(c.PlayerVFX and c.PlayerVFX.JumpDustPrefab)
    else
        c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
        c:PlayFall()
    end
end

state.LogicUpdate = function(c)
    -- 冲刺冷却计时（锁定期间也照常递减）
    if c.PlayerMovement.DashCooldownTimer > 0 then
        c.PlayerMovement.DashCooldownTimer =
            c.PlayerMovement.DashCooldownTimer - CS.UnityEngine.Time.deltaTime
    end

    if not state.outputLocked then
        -- ① 可变跳高：松开跳键 → 上升速度被削减（轻点跳=跳矮，按住跳=跳高，蔚蓝同款）
        --    每次只钳制不超过 jumpCutSpeed，松开瞬间生效一次后 vy 就 <= 上限，不再触发
        if not c.PlayerInput.JumpHeld and c.RigidBody.velocity.y > c.PlayerMovement.jumpCutSpeed then
            c:SetVelocityY(c.PlayerMovement.jumpCutSpeed)
        end

        -- ② 空中水平惯性：向目标速度渐进（MoveTowards），不是瞬间切换（手感有"转弯半径"）
        --    地面仍然是瞬间加速（指哪打哪），只有空中有惯性——这正是蔚蓝的手感核心之一
        local targetX = c.PlayerMovement.moveSpeed * c.InputX
        local newX = CS.UnityEngine.Mathf.MoveTowards(
            c.RigidBody.velocity.x, targetX,
            c.PlayerMovement.airAccel * CS.UnityEngine.Time.deltaTime)
        c:CheckFlip()
        c:SetVelocityX(newX)

        -- 上升/下降段重力切换
        local vy = c.RigidBody.velocity.y
        if vy > 0 and c.RigidBody.gravityScale ~= c.PlayerMovement.risingGravityScale then
            c.RigidBody.gravityScale = c.PlayerMovement.risingGravityScale
        elseif vy < 0 and c.RigidBody.gravityScale ~= c.PlayerMovement.fallingGravityScale then
            c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
        end

        -- Coyote Jump（离地缓冲窗口内按跳）
        if c.CoyoteTimeCounter > 0 and c.PlayerInput.JumpDown then
            state.ApplyJump(c)
            return nil
        end

        -- 空中冲刺
        if c.PlayerInput.DashDown and c.PlayerMovement.DashCooldownTimer <= 0 then
            return { layer = State.StateLayer.ACTIVE, name = "dash" }
        end

        -- 空中攻击（过冷却：频率统一由冷却控制）
        if c.PlayerInput.AttackDown and c:CanAttack() then
            return { layer = State.StateLayer.ACTIVE, name = "attack" }
        end
    end

    -- 落地（锁定期间也检测）
    if c:CheckGrounded() then
        c:SpawnDust(c.PlayerVFX and c.PlayerVFX.LandDustPrefab)
        return "ground"
    end

    -- 爬梯：空中撞进梯子范围 + 按上/下 → 进 ladder（贴墙滑墙本轮已移除）
    if c.PlayerClimb and c.PlayerClimb.IsOnLadder and c.PlayerInput.VerticalInput ~= 0 then
        return "ladder"
    end

    return nil
end

return state
