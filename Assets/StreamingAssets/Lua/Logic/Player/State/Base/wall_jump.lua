-- Logic/Player/State/Base/wall_jump.lua — 蹬墙跳（短暂过渡，随即回 air）
local State = require("Framework.Fsm.State")

local state = State.NewState(State.StateLayer.BASE, "wall_jump")
state.startTime = 0

state.Enter = function(c)
    state.startTime = 0
    c:PlayJump()   -- 安全播放入口

    -- 朝墙的反方向蹬出
    local jumpDir = -c.transform.localScale.x
    c.RigidBody.velocity = CS.UnityEngine.Vector2(0, 0)   -- 清空速度再施力
    c.RigidBody:AddForce(CS.UnityEngine.Vector2(
        c.PlayerMovement.wallJumpForce.x * jumpDir,
        c.PlayerMovement.wallJumpForce.y), CS.UnityEngine.ForceMode2D.Impulse)
    c:Flip()
end

state.LogicUpdate = function(c)
    if state.startTime >= c.PlayerMovement.wallJumpTime then
        return "air"
    end
    state.startTime = state.startTime + CS.UnityEngine.Time.deltaTime
    return nil
end

return state
