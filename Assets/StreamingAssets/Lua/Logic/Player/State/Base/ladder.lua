-- Logic/Player/State/Base/ladder.lua — 爬梯（BASE 层）
-- 判定：PlayerClimb.IsOnLadder（C# 侧收集当前梯子引用）
-- 纯 Lua 新增状态示例：C# 零改动，只加本文件 + PlayerHFSM.lua 一行 Register = 热更新演示！
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "ladder")

state.Enter = function(c)
    -- 梯子上无重力、速度清零
    c.RigidBody.gravityScale = 0
    c:SetVelocity(0, 0)
    c:PlayFall()   -- 暂无 Climb 动画，用 Fall 占位（安全播放入口）
end

state.LogicUpdate = function(c)
    -- ① 离开梯子范围 → 回到 ground/air
    if not c.PlayerClimb.IsOnLadder then
        if c:CheckGrounded() then
            return "ground"
        else
            return "air"
        end
    end

    -- ② 触地自动脱离攀爬（商业游戏惯例：顺着梯子下到地面就结束攀爬状态，
    --    不需要玩家再按别的键；仍按住"上"时保持攀爬，方便连续往上爬）
    if c:CheckGrounded() and c.PlayerInput.VerticalInput <= 0 then
        return "ground"
    end

    -- ③ 竖直攀爬（上/下键），不按就悬停
    local climbSpeed = c.PlayerMovement.climbSpeed
    if c.PlayerInput.VerticalInput ~= 0 then
        c:SetVelocityY(c.PlayerInput.VerticalInput * climbSpeed)
    else
        c:SetVelocityY(0)
    end

    -- ④ 跳离梯子（缓冲也支持：梯子上按跳立即反应）
    if c.JumpBufferTimer > 0 then
        c:SetVelocityY(c.PlayerMovement.jumpForce * 0.7)   -- 爬梯起跳稍矮（也吃跳跃截断）
        return "air"
    end

    return nil
end

state.Exit = function(c)
    -- 恢复下落重力
    c.RigidBody.gravityScale = c.PlayerMovement.fallingGravityScale
end

return state
