local HFSM  = require("Framework.Fsm.HFSM")
local State = require("Framework.Fsm.State")

-- 注册各层状态（注意：State.Base.ground 等文件要先建出来）
HFSM.Register(require("Logic.Player.State.Base.ground"))
HFSM.Register(require("Logic.Player.State.Base.air"))
HFSM.Register(require("Logic.Player.State.Base.wall_slide"))
HFSM.Register(require("Logic.Player.State.Base.wall_jump"))
HFSM.Register(require("Logic.Player.State.Active.attack"))
HFSM.Register(require("Logic.Player.State.Active.dash"))
HFSM.Register(require("Logic.Player.State.Abnormal.hurt"))

return HFSM   -- 把 HFSM 表返回，C# bridge 拿到它调 LogicUpdate