# Week 2 · 商店 + 背包（配置指南，代码已写好）

> 代码已全部完成并通过编译（本轮由 AI 代写，你只需在 Unity 里配置）。
> 设计决策：**商店不做场景切换，用 NPC 载体**（商业项目常规做法：靠近按 E → 弹面板）。
> 背包为简易版：武器走已有 weaponInventory（Q 切换），新增消耗品背包 + 面板。

## 一、代码清单（已交付）

| 文件 | 作用 |
|------|------|
| `_Scripts/Data/ShopItemData.cs` | 商品 SO：名字/图标/价格/类型（武器或消耗品）/效果 |
| `_Scripts/Data/ShopData.cs` | 商店 SO：商品列表（一张表配一个店） |
| `_Scripts/Player/PlayerInventory.cs` | 背包：加道具/使用道具（回血）/数量管理/事件广播 |
| `_Scripts/ShopNPC.cs` | NPC 交互：靠近按 E 开关商店 |
| `_Scripts/ShopPanel.cs` | 商店面板：动态生成商品行，钱不够置灰 |
| `_Scripts/InventoryPanel.cs` | 背包面板：按 B 开关，武器切换 + 消耗品使用 |
| `_Scripts/UIFactory.cs` | UI 工厂：运行时创建文本/按钮（无拖拽规范） |
| 改动：`PlayerCombat`（AddWeapon/SelectWeapon/CurrentIndex + 事件）、`PlayerHealth`（Heal）、`EventCenter`（+2 事件）、`AttackState`（冲撞灵敏度：判定点前移） | |

## 二、Unity 配置步骤（约 20 分钟）

### 1. 建商品资产
1. Project 窗口右键 → Create → Data → **Shop Item**，建 3 个：
   - `药水`：类型 Consumable、价格 15、效果 Heal、回血 30
   - `铁剑`：类型 Weapon、价格 50、weapon 拖 `Assets/Data/Weapons/IronSword.asset`
   - `火球法杖`：类型 Weapon、价格 80、weapon 拖 `FireballData.asset`
2. 右键 → Create → Data → **Shop Data**，命名 `新手商店`，把 3 个商品拖进 items 列表

### 2. 建商店 NPC
1. Hierarchy 新建空物体 → 改名 `ShopNPC`
2. 挂组件：`SpriteRenderer`（拖个商人/箱子贴图）+ `BoxCollider2D`（**勾 Is Trigger**）+ `ShopNPC`
3. ShopNPC 的 `shopData` 拖入 `新手商店`
4. 放到场景里玩家会路过的地方（比如出生点附近的地面）

### 3. 建 UI 面板
1. Canvas 下新建空物体 → 改名 `ShopPanel` → 挂 `ShopPanel` 脚本（代码会动态生成全部内容）
2. Canvas 下新建空物体 → 改名 `InventoryPanel` → 挂 `InventoryPanel` 脚本
3. （可选）给两个面板物体挂个半透明 Image 当背景，或者不管——内容是代码生成的，默认铺满

### 4. 玩家挂背包
1. 选中 Player → Add Component → `PlayerInventory`
2. 检查 Player 上已有：`PlayerWallet` ✓（上一轮挂的）

### 5. 测试（Play）
- 靠近 NPC → （如果有提示物体）提示出现 → 按 **E** → 商店面板弹出
- 买药水 → 金币减少、背包里药水 x1（面板上数量即时刷新）
- 买铁剑 → 武器栏多了铁剑（Q 切换或背包里点切换）
- 按 **B** → 背包面板：武器区（当前武器 ▶ 高亮，点击切换）、消耗品区（点击使用 → 回血 30 → 数量-1 → 满血时不可用）
- 钱不够时按钮置灰、价格变红

## 三、设计要点（面试可讲）

1. **商店也是数据驱动**：商品列表在 `ShopData` 资产里，加商品 = 建一个 ShopItemData 资产拖进列表，**代码零改动**——和你 Week 1 的掉落表同一套思想，可以打包讲"我的项目里数据驱动贯穿了掉落、商店、武器三个系统"
2. **购买链路**：`ShopPanel.TryBuy → PlayerWallet.TrySpend（失败直接 return）→ 发奖 → 广播事件刷新 UI`——钱包、战斗、UI 三个模块互不引用，全走事件
3. **UI 全部运行时创建**（UIFactory）：符合项目"无拖拽"规范，也演示了"用代码造 UI"的能力（Editor 工具方向的前置技能）
4. **为什么不做场景切换**：切场景要管场景生命周期/存档衔接/加载开销，NPC 商店是商业项目常规；将来随机关卡里"商店房间"就是带 NPC 的房间模板，天然复用
5. 已知 trade-off：消耗品效果目前只有 Heal（switch 分支），扩展新效果加一个枚举 + 一个 case 即可——"配置化效果系统"是 P3 的 Buff 系统前身

## 四、后续衔接
- Week 3 随机关卡：ShopNPC 可以做成"房间模板"的一部分
- 存档：PlayerInventory 的 slots 还没进 SaveData（ISaveable 未实现）——需要的话下轮加
