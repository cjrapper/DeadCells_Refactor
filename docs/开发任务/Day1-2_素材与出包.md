# Day 1-2 · 素材 + Unity 检查 + 出包

> 目的：先把"能玩的包"做出来，简历能甩链接。三天内完成，别拖。

## 步骤一：下素材（30 分钟）

Kenney 官网 https://kenney.nl/assets （全部免费商用，GitHub README 注明来源）

| 需要 | 下载包 | 内容 |
|------|--------|------|
| 角色 | `Toon Characters 1` | 卡通角色 sprite sheet |
| 怪物 | `Monsters Monsters-UFO` 或 `Toon Monsters` | 替代史莱姆 |
| 地形 | `Pixel Platformer` | tileset |
| UI | `UI Pack (Pixel)` 或 `UI Pack 2` | 按钮/面板/血条 |
| 音效 | `Interface Sounds` + `Digital Audio` | 点击/拾取/打击 |

下载 → 解压 → 拖进 `Assets/Art/Kenney/` 下（没有就新建）。

## 步骤二：Unity 检查（20 分钟）

1. 打开项目，等编译完
2. Console 无报错（之前改的代码应该全绿）
3. 菜单 `Tools/Lua/Verify PlayerHFSM` 跑一遍，7 个状态加载 OK
4. Play 试玩 1 分钟：移动/跳/冲刺/攻击/受击都正常
   - 重点验证：冲刺有冷却、落地前按跳会自动跳（跳跃缓冲）、受击有 Hurt 动画

> 如果有报错/异常，把 Console 截图或报错文本贴给我，先修再往下走。

## 步骤三：替换素材（1-2 小时，可后置到 Week 1）

1. 选中贴图 → Inspector 调 `Sprite Mode = Multiple` → `Sprite Editor` → `Slice`（按帧尺寸）→ Apply
2. 拖到 Player/Enemy 的 SpriteRenderer 上替换，Animation 窗口重新拉帧（参考旧动画的时间）
3. UI：替换 Canvas 下的 Image 源图（注意九宫格：Sprite Editor 里设 Border）

## 步骤四：Build 出包（1 小时）

1. `File → Build Settings`
2. 确认 `_Scenes/SampleScene` 在 Scenes In Build 列表里（没有就 Add Open Scenes）
3. `Player Settings` → 公司名/产品名填好（简历链接要用）
4. `Target Platform = Windows` → `Build` → 选个输出文件夹
5. 双击生成的 exe，能玩 → 打包成 zip
6. **上传网盘/itch.io**（https://itch.io 注册免费，网页能传 zip）→ 拿到下载链接

## 步骤五：投递（每天 20 分钟，别停）

- 简历项目描述里加一行：**"可试玩 Demo（Windows 包）：<链接>"**
- 每天投 1-2 家：BOSS 直聘 / 牛客 / 公司官网
- 投递话术（打招呼用）：
  > "您好，我是北语数媒大三学生，主投 Unity 客户端。独立开发了 2D 动作平台游戏（HFSM 三层状态机 + xLua 热更新 + 行为树），有战斗策划实习经验（Excel 导表 + 怪物 AI 配置 + 战斗手感调优）。可实习 3 个月以上，一周内到岗。这是我的试玩包：<链接>，简历在附件，期待您的回复。"

## 验收清单
- [ ] Console 无报错
- [ ] Play 手感正常（冲刺冷却/跳跃缓冲/受击都 OK）
- [ ] Windows 包能玩，有 zip + 下载链接
- [ ] 简历已加 Demo 链接，已投 ≥1 家
