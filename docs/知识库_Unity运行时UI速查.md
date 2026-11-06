# Unity 运行时创建 UI · 速查

> 最后更新：2026-11-05

> 用途：用代码建 UI 时随手查。
> 项目规范：**不拖拽、不手摆，全部代码生成**（见 CLAUDE.md）。

---

## 一、四件套（所有 UI 元素都是这四步）

```csharp
// ① 创建物体 + 挂组件（UI 必须有 RectTransform）
GameObject go = new GameObject("名字", typeof(RectTransform), typeof(Image));
// ② 挂到父级 —— 第二个参数必须是 false
go.transform.SetParent(parent, false);
// ③ 设布局（锚点/尺寸）—— 代码 new 出来的锚点是默认值，不设会跑到屏幕角落
RectTransform rt = go.GetComponent<RectTransform>();
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
// ④ 设外观
Image img = go.GetComponent<Image>();
img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
```

**记住这个骨架，换组件类型就是换 `typeof(...)`。**

---

## 二、三种最常用元素

### 纯色底板（Image）

```csharp
GameObject go = new GameObject("Bg", typeof(RectTransform), typeof(Image));
go.transform.SetParent(parent, false);
Image img = go.GetComponent<Image>();
img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
// 锚点铺满父级（同上）
```

> **`Image` 不给 `sprite` 也能显示** —— 它画一个纯色矩形，颜色就是 `color`。做底板完全够用。

### 文字（TextMeshProUGUI）

```csharp
GameObject go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
go.transform.SetParent(parent, false);
TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
text.text = "SHOP";          // ⚠️ 必须英文（字体没有中文字形，中文变方块）
text.fontSize = 30;
text.color = Color.white;
text.alignment = TextAlignmentOptions.Center;
```

### 按钮（Image + Button）

```csharp
GameObject go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
go.transform.SetParent(parent, false);
go.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.45f, 1f);
Button btn = go.GetComponent<Button>();
btn.onClick.AddListener(() => { /* 点击后干什么 */ });
```

**按钮的文字是它的子物体**，再对 `go.transform` 调一次创建文字的流程。

---

## 三、布局的两种常用姿势

### 铺满父级（背景、遮罩）

```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
```

### 锚定到父级中心，按坐标摆（列表项、按钮）

```csharp
rt.anchorMin = new Vector2(0.5f, 0.5f);
rt.anchorMax = new Vector2(0.5f, 0.5f);
rt.pivot     = new Vector2(0.5f, 0.5f);
rt.anchoredPosition = new Vector2(0f, -60f);   // 相对父级中心
rt.sizeDelta = new Vector2(300f, 40f);         // 宽高
```

> **不设中心锚点的话，`anchoredPosition` 的含义是不确定的** —— 你会以为自己写了 (0,0)，结果元素飞出屏幕。

---

## 四、踩过的坑（都是真金白银）

| 坑 | 后果 | 正解 |
|---|---|---|
| `SetParent(parent)` 没传 `false` | 位置跑偏、缩放被拉 | **必须传 `false`** |
| 忘了设 RectTransform 锚点 | UI 飞到屏幕角落，像"没显示" | 显式设锚点 |
| **用 `SetActive(false)` 整个面板来表达"隐藏"** | 面板的 `Update` 不跑、按键失效 | **只隐藏内容子物体，面板本体保持激活** |
| 文字写中文 | 显示成方块 | 运行时文字一律英文 |
| 父级链上没有 `Canvas` | 完全不渲染 | 确认父级能追到 Canvas |
| `[serializedfield]` 小写 | 编译不过 | `[SerializeField]`，两个大写字头 |

---

## 五、什么时候别自己写，用 `UIFactory`

项目里 `Assets/_Scripts/UIFactory.cs` 已经把上面这些封装好了：

| 方法 | 干什么 |
|---|---|
| `CreatePanel(parent, name, bgColor, bg)` | 建一个铺满父级的底板 |
| `CreateText(parent, name, content, size)` | 建文字（自动居中锚点） |
| `CreateButton(parent, name, label, onClick)` | 建按钮（自动带点击音） |
| `Center(rt, pos, size)` | 把 RectTransform 锚到父级中心 |
| `ClearChildren(parent)` | 清空子物体（刷新列表用） |

**建议：自己手写一遍搞懂原理，之后就用工厂方法。** 手写是为了"知道里面发生了什么"，用工厂是为了"省时间"。

---

## 六、调试 UI 不显示的三层法

1. **`Update` 跑没跑** → 在这一帧都刷的日志，看 Console 滚不滚
2. **按键读到没** → 日志挪进 `if(GetKeyDown)` 里面
3. **`Open` 执行了吗** → 日志 + Hierarchy 里看物体是否 active

**日志打在"你怀疑的前一步"，不是打在结论上。**

---

## 七、生命周期速记

| 方法 | 什么时候跑 | 注意 |
|---|---|---|
| `Awake` | 物体第一次被加载时，**只跑一次** | 初始化引用、建子物体 |
| `OnEnable` | **每次** `SetActive(true)` 都跑 | 订阅事件放这儿 |
| `Update` | 每帧，**但物体 inactive 时不跑** | 按键检测放这儿会失效 |
| `OnDisable` | 每次 `SetActive(false)` | 取消订阅放这儿 |

---

## 八、四种锚点组合（覆盖 90% 的场景）

**记住这四张牌，写 UI 时挑一张。**

### ① 铺满父级（背景、遮罩）

```csharp
rt.anchorMin = Vector2.zero;     // (0,0)
rt.anchorMax = Vector2.one;      // (1,1)
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
```

### ② 顶部居中（标题）

```csharp
rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
rt.pivot     = new Vector2(0.5f, 1f);
rt.sizeDelta = new Vector2(宽, 高);
rt.anchoredPosition = new Vector2(0f, -20f);
```

### ③ 顶部 + 横向拉伸（列表的一行）

```csharp
rt.anchorMin = new Vector2(0f, 1f);
rt.anchorMax = new Vector2(1f, 1f);
rt.pivot     = new Vector2(0.5f, 1f);
rt.sizeDelta = new Vector2(0f, 行高);      // ★ x=0 表示"和父级一样宽"
rt.anchoredPosition = new Vector2(0f, -y);
```

### ④ 贴某一侧（行内的元素）

| 贴哪 | `anchorMin` = `anchorMax` | `pivot` | `anchoredPosition` |
|---|---|---|---|
| 左边 | `(0, 0.5)` | `(0, 0.5)` | `(左距, 0)` |
| 右边 | `(1, 0.5)` | `(1, 0.5)` | `(-右距, 0)` |
| 左上 | `(0, 1)` | `(0, 1)` | `(左距, -上距)` |

> **不变的规则**：`anchorMin == anchorMax` → 钉在一个点，`pivot` 决定元素往哪一边长。

---

## 九、UI 不显示：完整排查清单

**分两类查，别混着猜。**

### A 类：根本没建出来

| 查什么 | 怎么查 |
|---|---|
| **刷新代码跑了吗** | 在 `Refresh()` 第一行打日志，看 Console |
| **开局刷新了吗** | ⚠️ **`OnEnable` 注册了监听 ≠ 初始化过** |
| **事件签名对吗** | `AddListener<int>` 必须配 `Broadcast(x, int)`，**不匹配就静默失效** |
| **订阅的事件对吗** | 消耗品变化发的是 `InventoryChange`，不是 `CoinChange` |

### B 类：建了但看不见

| 查什么 | 怎么查 |
|---|---|
| 父物体激活吗 | Hierarchy 图标亮/灰 |
| 锚点设了吗 | Inspector 里看 Anchor / Pivot |
| `sizeDelta` 够大吗 | 太小会换行或被裁 |
| 文字内容非空吗 | `text` 是 `""` 就什么都不显示 |
| 颜色透明度 | `color.a == 0` / 全黑 → 看不见 |
| 被盖住了 | Hierarchy 越靠**下** = 越后画 = 越在上层 |

> **实战踩坑记录**：`Awake` 建了容器、`OnEnable` 注册了监听，
> **但没有一行代码在开局时调 `Refresh()`**
> → 结果是"只有深色背景，一个字都没有"，而且不报错。

### ★ 黄金模式：初始化 + 订阅，缺一不可

```
启动时：主动调一次 Refresh()        ← 保证初始状态正确
变化时：事件回调里调 Refresh()       ← 保证后续同步
```

**只做后者 → 开局是空的；只做前者 → 后续不更新。**

---

## 十、把重复的锚点代码封起来

**每次写 UI 都要重复 4~6 行锚点，这是最大的疲劳来源。**

**抽一个自己的方法：**

```csharp
private RectTransform CreateRow(string text, float y)
{
    TextMeshProUGUI t = UIFactory.CreateText(content, "Row", text, 18);
    RectTransform rt = t.rectTransform;
    rt.anchorMin = new Vector2(0f, 1f);
    rt.anchorMax = new Vector2(0f, 1f);
    rt.pivot     = new Vector2(0f, 1f);
    rt.sizeDelta = new Vector2(200f, 24f);
    rt.anchoredPosition = new Vector2(8f, -y);
    return rt;
}
```

**之后每加一行就只是一行调用：**

```csharp
CreateRow("POTION x3", 0f);
CreateRow("SWORD ▶",  26f);
CreateRow("KEY x2",   52f);
```

> **这才是写 UI 的正确姿势** —— 不是每次都手写锚点，而是**抽工具**。
> 一次写对，后面全是复制一行调用。

---

## 十一、为什么 UI 让人头大（心理账）

**因为 UI 不是"逻辑"，是"配置"。**

| 写逻辑 | 写 UI |
|---|---|
| 有对错，算错就错 | 没有对错，只有"看起来对不对" |
| 一次写对 | 改一个数字跑一次 |
| 靠推理 | 靠试 |

**三个减负的办法：**

1. **背下那四种锚点组合**（第八节）—— 90% 的场景就这四种
2. **抽模板方法**（第十节）—— 把重复的锚点封装掉
3. **先写死，再接数据** —— `CreateRow("TEST", 0)` 能显示，再换成 `CreateRow($"KEY x{keys.keys}", 0)`

> **一次只改一个变量。**
> 从"写死"到"读数据"是一小步；从零到"显示出来"是一大步。

---

# 十二、UGUI 的整体结构：一个 UI 要能显示，需要什么东西

## 12.1 四件套（缺一不可）

```
场景
├── Canvas                     ← 必须。所有 UI 都必须挂在 Canvas 下
│   ├── CanvasScaler           ← 分辨率适配（Canvas 会自动带）
│   ├── GraphicRaycaster       ← 让 UI 能被点击（Canvas 会自动带）
│   └── 你的 UI 元素...
└── EventSystem                ← 必须。没有它，UI 收不到任何点击
    └── StandaloneInputModule  ← 自动带
```

**四个东西各管什么：**

| 东西 | 管什么 | 没有会怎样 |
|---|---|---|
| **Canvas** | 决定 UI 怎么渲染、在哪个排序层 | UI 完全不显示 |
| **CanvasScaler** | 分辨率变化时怎么缩放 | UI 在不同分辨率下大小乱套 |
| **GraphicRaycaster** | 把鼠标/触摸位置翻译成"点到了哪个 UI" | 按钮点不动 |
| **EventSystem** | 事件分发中枢 | 所有 UI 交互失效 |

> **最容易漏的是 `EventSystem`** —— 它不在 Canvas 下，是场景里独立的一个物体。
> 症状很典型：**按钮画得出来，但点了没反应**。

## 12.2 CanvasScaler 的两种常用模式

| 模式 | 效果 | 什么时候用 |
|---|---|---|
| **Constant Pixel Size** | UI 大小固定像素 | 只在一种分辨率上跑 |
| **Scale With Screen Size** | 按参考分辨率等比缩放 | **绝大多数游戏用这个** |

**选 Scale With Screen Size 时两个关键参数：**
- **Reference Resolution**：设计稿分辨率（比如 1920×1080）
- **Match**：0 = 只按宽度缩放，1 = 只按高度缩放，0.5 = 折中

> 你的项目现在用哪种，**去场景里点开 Canvas 看一眼**。

---

# 十三、RectTransform 完整解剖

> 这一节是 UGUI 的核心。**理解它，UI 就不神秘了。**

## 13.1 六个关键属性

```
        ┌─────────────── 父级 ───────────────┐
        │                                     │
        │   anchorMin ●                       │
        │             ╎                       │
        │             ╎  anchorMax ●          │
        │             ╎             ╎         │
        │           ┌─╂─────────────╂─┐       │
        │           │ ╎   元素      ╎ │       │
        │           │ ╎      ↑      ╎ │       │
        │           │ ╎    pivot    ╎ │       │
        │           └─╂─────────────╂─┘       │
        │             ╎             ╎         │
        └─────────────────────────────────────┘
```

| 属性 | 是什么 | 归一化坐标 |
|---|---|---|
| `anchorMin` / `anchorMax` | **锚点** —— 元素"挂"在父级的哪个位置 | `(0,0)`=左下，`(1,1)`=右上 |
| `pivot` | **轴心** —— 元素自己的"原点"在哪 | `(0,0)`=左下，`(1,1)`=右上 |
| `sizeDelta` | **尺寸**（含义随锚点变，见下） | 像素 |
| `anchoredPosition` | **轴心**相对**锚点**的偏移 | 像素 |
| `offsetMin` / `offsetMax` | 左下/右上角相对锚点的偏移（拉伸模式用） | 像素 |

## 13.2 核心规则：锚点重合 or 分开

**这一条搞懂，UGUI 就通了一半。**

### 情况 A：`anchorMin == anchorMax`（锚点重合 = 钉在一个点）

```
锚点 (0.5, 1) = 父级顶部中间那个点
```

- **元素大小固定**（`sizeDelta` 就是实际宽高）
- `anchoredPosition` = **轴心**离那个点的距离
- 父级改变大小 → 元素**跟着那个点移动**，但**大小不变**

**例子**：标题
```csharp
anchorMin = anchorMax = (0.5, 1);
pivot = (0.5, 1);
sizeDelta = (200, 40);
anchoredPosition = (0, -20);
```
→ 200×40 的框，**顶边**在父级顶边下方 20 像素，水平居中。

### 情况 B：`anchorMin ≠ anchorMax`（锚点分开 = 拉伸）

```
anchorMin (0,0) ──┐
                  ├── 元素跟着父级一起拉伸
anchorMax (1,1) ──┘
```

- **元素大小跟着父级变**
- `sizeDelta` 的含义变成 **"和父级的差值"**
  - `sizeDelta.x = 0` → **宽度 = 父级宽度**
  - `sizeDelta.x = -40` → 宽度比父级小 40
- `anchoredPosition` 一般不用（用 `offsetMin/offsetMax` 更直观）

## 13.3 完整算例（拿纸跟着算一遍）

**父级 600 × 400。**

### 例 1：左上角一个小框

```csharp
anchorMin = anchorMax = (0, 1);    // 重合 → 钉在父级左上
pivot     = (0, 1);                // 轴心在元素左上
sizeDelta = (200, 100);
anchoredPosition = (10, -20);
```

**结果**：
- 元素宽 **200**、高 **100**（锚点重合，sizeDelta 就是实际尺寸）
- 元素**左上角**在父级左上角**往右 10、往下 20**
- 所以占据范围：x 从 10 到 210，y 从 -20 到 -120

### 例 2：四边留边的全屏框

```csharp
anchorMin = (0, 0);                // 分开 → 拉伸
anchorMax = (1, 1);
offsetMin = (10, 20);              // 左下角 → 父级左下 + (10, 20)
offsetMax = (-10, -20);            // 右上角 → 父级右上 + (-10, -20)
```

**结果**：
- 宽度 = 600 - 10 - 10 = **580**
- 高度 = 400 - 20 - 20 = **360**
- 此时 `sizeDelta` 会显示成 `(-20, -40)` —— **这就是"和父级的差值"**

> **验证方法**：设完去 Inspector 看 Width/Height 对不对，一眼就知道锚点算得对不对。

## 13.4 三个"位置"别搞混

| 属性 | 相对谁 | 什么时候用 |
|---|---|---|
| `position` | **世界坐标** | 一般不用（UI 都在 Canvas 里） |
| `localPosition` | **父物体** | 偶尔用 |
| **`anchoredPosition`** | **锚点** | **UI 布局基本都用这个** |

## 13.5 `sizeDelta` 的两种含义（最容易搞错）

| 锚点状态 | `sizeDelta` 的意思 |
|---|---|
| 重合 | **实际宽高** |
| 分开（拉伸） | **和父级的差值**（0 = 一样大） |

> **所以判断一个元素"为什么大小不对"，第一件事是看它的锚点是重合还是分开。**

---

# 十四、布局组件（元素多了别手摆）

## 14.1 三个 Layout Group

| 组件 | 排列 |
|---|---|
| `HorizontalLayoutGroup` | 从左往右 |
| `VerticalLayoutGroup` | 从上往下 |
| `GridLayoutGroup` | 网格（行列） |

**关键参数：**
- `Spacing` —— 元素间距
- `Padding` —— 四边内边距
- `Child Alignment` —— 整体对齐
- `Child Force Expand` —— 是否让子元素撑满

**用法**：父物体挂它，子元素**不用管位置**，按添加顺序自动排。

## 14.2 `ContentSizeFitter` —— 让容器自动变大

挂在**容器**上，设 `Vertical Fit = Preferred Size` → **容器高度跟着内容自动长**。

**典型组合**：
```
Scroll View
└── Viewport（带 Mask）
    └── Content（VerticalLayoutGroup + ContentSizeFitter）
        ├── 项目1
        ├── 项目2
        └── ...
```

## 14.3 `LayoutElement` —— 给单个元素指定尺寸要求

挂在**子元素**上，设 `Preferred Width / Height`、`Min`、`Flexible`。

配合 Layout Group 用 —— 让 Layout Group 知道"这个元素想要多大"。

## 14.4 什么时候用布局组件

| 情况 | 用什么 |
|---|---|
| 元素少（2~3 个）、位置固定 | **手动设锚点**（简单直接） |
| 元素数量会变、要滚动 | **Layout Group + ContentSizeFitter** |
| 网格状（背包格子） | **GridLayoutGroup** |

> **布局组件有代价**：它每帧可能重算（Layout Rebuild），元素多时耗性能。
> **元素少就别用**，手动摆更快。

---

# 十五、滚动视图（ScrollRect）

## 15.1 标准结构

```
Scroll View（ScrollRect）
├── Viewport（Image + Mask 或 RectMask2D）
│   └── Content（Layout Group + ContentSizeFitter）
│       └── 一堆项目
└── Scrollbar（可选）
```

- **`ScrollRect`** 挂在根上，`Content` 字段指向 `Content`
- **`Viewport`** 决定"能看见哪一块"，超出的被裁掉
- **`Content`** 是真正的内容容器，里面放项目

## 15.2 关键设置

| 组件 | 设置 |
|---|---|
| `ScrollRect` | `Horizontal=false` / `Vertical=true`；`Movement Type=Elastic` |
| `Viewport` | 加 **`RectMask2D`**（性能好）或 `Mask` |
| `Content` | 锚点**顶部拉伸**、`pivot=(0.5,1)`；加 `ContentSizeFitter` 垂直=Preferred Size |

## 15.3 现在需要吗

**你现在不需要** —— 物品就几个，直接列出来就行。

**什么时候需要**：物品/道具超过 8~10 个，一屏放不下时。

---

# 十六、Mask 与 RectMask2D（复习 + 实操）

| | `Mask` | `RectMask2D` |
|---|---|---|
| 原理 | **模板测试** | CPU 算矩形裁剪 |
| 形状 | **任意**（靠一张图） | **只能矩形** |
| 性能 | 打断合批 | **不打断合批** |

**实操建议**：**滚动视图、列表裁剪一律用 `RectMask2D`**；只有"异形遮罩"（圆形头像）才用 `Mask`。

---

# 十七、UI 性能（面试常问）

## 17.1 三个关键概念

| 概念 | 是什么 |
|---|---|
| **DrawCall** | CPU 让 GPU 画一次 |
| **合批 (Batching)** | 把多个 DrawCall 合并成一次 |
| **Rebuild** | UI 元素变化时重建它的网格 |

## 17.2 UGUI 合批的五个条件

1. 同一个 Canvas
2. 同一个材质/贴图（**同一张图集**）
3. 渲染顺序连续（Hierarchy 里相邻，中间不夹别的）
4. 没有被 `Mask` 打断
5. Sorting Layer / Order in Layer 相同

## 17.3 最常见的性能杀手

| 做法 | 后果 |
|---|---|
| **每帧改 UI 文字** | 触发 Rebuild，CPU 飙升 |
| 图集不统一 | 合批失败，DrawCall 翻倍 |
| 用 `Mask` 做列表裁剪 | 打断合批 |
| Canvas 里放大量元素 | 任意一个变化 → **整个 Canvas 重建** |

## 17.4 优化手段

- **只在数据真变化时刷新**（这就是"事件驱动刷新"的价值）
- **拆分 Canvas**：静态的（血条框）和动态的（数字）分开放 → 动数字不重建整个
- 同图集的元素放在相邻位置
- 列表用 `RectMask2D` 而不是 `Mask`

---

# 十八、两种刷新模式的完整例子

## 模式 A：固定元素 —— 场景建好，代码只改内容

**适合：血条、金币数、分数** —— 数量永远 1 个，位置固定。

```csharp
// 场景里已经有：Canvas/CoinText（一个 TextMeshProUGUI）
private TextMeshProUGUI coinText;

void Awake()
{
    coinText = transform.Find("CoinText").GetComponent<TextMeshProUGUI>();
}

void OnEnable()
{
    EventCenter.Instance.AddListener<int>(EventCenter.EventType.CoinChange.ToString(), UpdateCoinText);
}

void OnDisable()
{
    EventCenter.Instance.RemoveListener<int>(EventCenter.EventType.CoinChange.ToString(), UpdateCoinText);
}

void UpdateCoinText(int value)
{
    coinText.text = value.ToString();     // ← 只改文字，不重建
}
```

**优点**：零 GC、零 Rebuild、简单。
**缺点**：UI 元素得在场景里摆好（项目规范允许 `transform.Find`）。

## 模式 B：动态列表 —— 代码创建，清空重建

**适合：物品列表、武器列表、商品列表** —— 数量会变。

```csharp
void Refresh()
{
    // ① 清空（永远倒序！）
    for (int i = content.childCount - 1; i >= 0; i--)
        Destroy(content.GetChild(i).gameObject);

    // ② 重建
    float y = 0f;
    foreach (var slot in inventory.Slots)
    {
        CreateRow($"{slot.itemData.itemName} x{slot.count}", y);
        y += 26f;
    }
}
```

**优点**：数量随便变，代码简单。
**缺点**：每次全重建（元素多时费性能）。

## 怎么选

| 问题 | 用哪个 |
|---|---|
| 数量固定、永远 1 个？ | **A**（场景建 + 改文字） |
| 数量会变（0~N）？ | **B**（代码建 + 重建） |

> **两个都对，用错场景才错。**
> 你今天给金币做的那套（场景建 + 挂 Text + 改文字）**是标准做法，别改**。

---

# 十九、你项目里的四套 UI 做法对照

| 面板 | 模式 | 为什么 |
|---|---|---|
| `UIManager`（血条/金币） | A 固定元素 | 位置固定，只改数字 |
| `ShopPanel` | B 动态列表 | 商品数量可变 |
| `PlayerHUD` | B 动态列表 | 物品/武器数量可变 |
| `EnemyHealthBar` | 代码建（每个敌人一个） | 跟着敌人走 |

**看懂这张表，你就知道"什么时候该用哪种"。**

---

# 二十、一页速查（打印贴墙上那种）

```
【UI 能显示的前提】
Canvas 存在 + 父级链能追到 Canvas + 元素 active + 颜色不是透明/黑

【点击能响的前提】
Canvas 上有 GraphicRaycaster + 场景里有 EventSystem

【RectTransform 四条】
锚点重合 → sizeDelta = 实际大小
锚点分开 → sizeDelta = 和父级的差值
pivot  → 元素往哪边长
anchoredPosition → 轴心离锚点多远

【四种锚点组合】
铺满    (0,0)→(1,1)     + offset 全 0
顶中    (0.5,1) 重合     pivot (0.5,1)
顶拉伸  (0,1)→(1,1)      pivot (0.5,1)  sizeDelta.x=0
贴侧    (x,0.5) 重合     pivot (x,0.5)

【刷新模式】
数量固定 → 场景建好 + 改文字
数量变化 → 代码建 + 倒序清空重建
启动时刷新一次 + 变化时再刷新（缺一不可）

【永远别忘】
父级要铺满全屏，否则 (0,1) 不是屏幕左上角
运行时文字只能 ASCII
清空永远倒序
订阅和广播的参数类型必须一致
```
