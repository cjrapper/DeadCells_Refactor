-- Logic/Player/State/Base/wall_slide.lua — 贴墙下滑
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "wall_slide")

state.Enter = function(c)
    c:PlayFall()   -- 安全播放入口
end

state.LogicUpdate = function(c)
    -- 蹬墙跳（JumpDown → wall_jump）
    if c.PlayerInput.JumpDown then
        return "wall_jump"
    end

    -- 离开墙 / 落地 / 按反方向 → 回 ground 或 air
    local movingAway = c.InputX ~= 0 and c.InputX ~= c.transform.localScale.x
    if not c:CheckTouchingWall() or c:CheckGrounded() or movingAway then
        if c:CheckGrounded() then
            return "ground"
        else
            return "air"
        end
    end

    -- 贴墙缓降
    c:SetVelocity(c.RigidBody.velocity.x, -c.PlayerMovement.wallSlideSpeed)
    return nil
end

return state
