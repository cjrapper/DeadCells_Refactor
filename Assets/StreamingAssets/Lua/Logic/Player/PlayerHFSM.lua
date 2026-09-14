local HFSM  = require("Framework.Fsm.HFSM")
local State = require("Framework.Fsm.State")

-- 注册各层状态（注意：State.Base.ground 等文件要先建出来）
HFSM.Register(require("Logic.Player.State.Base.ground"))
HFSM.Register(require("Logic.Player.State.Base.air"))
HFSM.Register(require("Logic.Player.State.Base.ladder"))   -- 爬梯：纯 Lua 新增状态（C# 零改动）
-- 爬墙/蹭墙跳本轮暂不使用，文件保留（wall_slide / wall_jump 后续需要再注册）
-- HFSM.Register(require("Logic.Player.State.Base.wall_slide"))
-- HFSM.Register(require("Logic.Player.State.Base.wall_jump"))
HFSM.Register(require("Logic.Player.State.Active.attack"))
HFSM.Register(require("Logic.Player.State.Active.dash"))
HFSM.Register(require("Logic.Player.State.Abnormal.hurt"))

return HFSM   -- 把 HFSM 表返回，C# bridge 拿到它调 LogicUpdate