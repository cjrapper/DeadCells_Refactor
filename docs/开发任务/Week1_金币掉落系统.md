# Week 1 · 金币掉落系统（手敲任务书）

> 目标：击杀敌人 → 掉落金币 → 走近拾取 → 右上角金币数 +N。全程手敲，别复制粘贴。
> 完成后你能对着面试官讲：**"我用数据驱动做了掉落表，权重随机 + 对象池 + 事件解耦"**。

---

## ① 这个系统的面试故事（先想清楚再敲）

> "掉落系统我用 **ScriptableObject 掉落表**做的：敌人不写死掉什么，而是引用一张 LootTable，
> 表里配置掉落条目（物品、权重、数量范围），击杀时按**权重随机** Roll 一条。加新掉落物
> 只改配置不改代码——这就是**数据驱动**。金币生成走**对象池**（和弹丸/残影同一套），
> 拾取通过 **Trigger + 玩家 Tag 判定**，金币变化走**事件中心**广播给 UI，敌人不知道 UI 存在。"

一句话总结：**配置管"掉什么"，代码管"怎么掉"，UI 管"怎么显示"，三者互不引用。**

## ② 架构（新增 5 个文件 + 改 4 个文件）

```
[新] LootTable.cs        ScriptableObject：掉落表（权重/数量范围 + Roll 方法）
[新] CoinPickup.cs       金币预制体组件：磁吸 + 拾取 + 回收
[新] PlayerWallet.cs     玩家钱包组件：金币数 + 广播事件
[改] Enemy.cs            Die() 里接入掉落
[改] PoolManager.cs      新增 Coin 池类型
[改] EventCenter.cs      新增 CoinChange 事件枚举
[改] UIManager.cs        金币文本（transform.Find 自动获取）
[手配] 场景：金币 Text、LootTable asset、金币 Prefab（挂 CoinPickup）
```

## ③ 分步代码（按顺序敲）

### Step 1 · LootTable.cs（数据驱动核心）🎯考点：权重随机算法、SO 数据驱动

新建 `Assets/_Scripts/Data/LootTable.cs`（Data 目录没有就建）：

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Data
{
    /// <summary>
    /// 掉落表 —— 数据驱动：敌人"掉什么"由配置决定，代码只负责"怎么掉"。
    /// </summary>
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Combat/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class LootEntry
        {
            public GameObject itemPrefab;   // 掉落物预制体（金币/药水...）
            [Range(0f, 100f)] public float weight = 1f;   // 权重：越大越容易掉
            public int minCount = 1;        // 数量范围
            public int maxCount = 1;
        }

        public List<LootEntry> entries = new();

        /// <summary>按权重随机 Roll 一条掉落，返回 null 表示本次不掉落</summary>
        public LootEntry Roll()
        {
            if (entries.Count == 0) return null;

            // 1. 算总权重
            float total = 0f;
            foreach (var e in entries) total += e.weight;
            if (total <= 0f) return null;

            // 2. 随机一个 [0, total) 的点，按权重区间落袋（线性扫描）
            float r = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            foreach (var e in entries)
            {
                acc += e.weight;
                if (r <= acc) return e;
            }
            return entries[entries.Count - 1];  // 浮点误差兜底
        }
    }
}
```

> 🎯 面试追问"权重随机怎么实现"：**总权重归一化 + 随机点落在哪个区间**，O(n) 遍历，n 很小（掉落表就几条）。追问"如果表有 1 万条怎么办"：答"前缀和 + 二分，O(logn)——但游戏掉落表一般个位数，线性扫描足够，**先量后优**"。

### Step 2 · CoinPickup.cs（拾取 + 磁吸）

新建 `Assets/_Scripts/Player/CoinPickup.cs`：

```csharp
using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Player
{
    /// <summary>
    /// 金币：磁吸（距离近了自动飞向玩家）+ 触发拾取 + 对象池回收。
    /// </summary>
    public class CoinPickup : MonoBehaviour
    {
        public int value = 1;           // 面值（配 Prefab 时定）
        public float magnetRadius = 2f; // 磁吸半径
        public float magnetSpeed = 8f;  // 磁吸速度

        private Transform player;
        private SamplePool pool;        // 所属对象池（回收用）
        private bool hasPool;

        private void Awake()
        {
            // 缓存玩家引用（Update 里不 Find）
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void Update()
        {
            // 磁吸：玩家进入半径后加速飞向玩家（Update 里不 new，只改位置）
            if (player == null) return;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < magnetRadius && dist > 0.05f)
            {
                Vector3 dir = (player.position - transform.position).normalized;
                transform.position += dir * (magnetSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // 加钱（事件解耦：UI 订阅 CoinChange，金币不知道 UI 存在）
            var wallet = other.GetComponent<PlayerWallet>();
            if (wallet != null) wallet.AddCoins(value);

            // 回收：有池回池，没池销毁
            if (hasPool && pool != null) pool.Return(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>从对象池取出时调用（Init 逻辑放这，不放 Awake）</summary>
        public void OnGetFromPool()
        {
            // 重置状态：位置由 Spawn 方设置，这里清干净上次的残留
        }

        public void AssignPool(SamplePool p)
        {
            pool = p;
            hasPool = pool != null;
        }
    }
}
```

### Step 3 · PlayerWallet.cs（钱包 + 事件广播）

新建 `Assets/_Scripts/Player/PlayerWallet.cs`：

```csharp
using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家钱包 —— 金币数据 + 变化事件。UI 通过事件中心订阅，不直接引用。
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        public int Coins { get; private set; }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            EventCenter.Instance?.Broadcast(
                EventCenter.EventType.CoinChange.ToString(), Coins);
        }

        public bool TrySpend(int amount)
        {
            if (Coins < amount) return false;   // 钱不够
            Coins -= amount;
            EventCenter.Instance?.Broadcast(
                EventCenter.EventType.CoinChange.ToString(), Coins);
            return true;                        // 商店系统 Week 2 会用到
        }
    }
}
```

### Step 4 · 接入：EventCenter / PoolManager / Enemy（三处小改）

**4a. `EventCenter.cs`** 枚举加一项：

```csharp
public enum EventType { PlayerHealthChange, PlayerStateChange, PlayerDead, PlayerHurt, CoinChange }
```

**4b. `PoolManager.cs`** 三处改：
1. 枚举加 `Coin,`（Projectile 后面）
2. 加字段：`[SerializeField] private SamplePool coinPool;`
3. `GetPool` switch 加：`PoolType.Coin => coinPool,`
4. `OnValidate` 加：`if (coinPool == null) coinPool = FindPoolByName("CoinPool");`

> 敲的时候注意：**枚举、字段、switch、OnValidate 四个地方都要改**，漏一个编译不过——这正是"中心化对象池"的代价，面试可以说"新增池类型要动 4 处，所以商业项目会改成字典注册制"（加分点）。

**4c. `Enemy.cs` Die() 接入掉落**（在 `protected virtual void Die()` 里、`Destroy(gameObject)` 之前）：

```csharp
// 掉落：由 LootTable 配置驱动（数据驱动，不写死掉什么）
if (lootTable != null)
{
    var entry = lootTable.Roll();
    if (entry != null && entry.itemPrefab != null)
    {
        int count = Random.Range(entry.minCount, entry.maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            // 出生位置：敌人位置 + 随机小偏移（避免叠成一坨）
            Vector3 offset = new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(0.2f, 0.6f), 0f);
            SpawnLoot(entry.itemPrefab, transform.position + offset);
        }
    }
}
```

在 `Enemy.cs` 加字段和私有方法：

```csharp
[Header("Loot")]
public LootTable lootTable;      // 掉落表（Inspector 拖，或代码注入）

private void SpawnLoot(GameObject prefab, Vector3 pos)
{
    // 优先对象池，池不可用就 Instantiate（和弹丸同一套降级逻辑）
    SamplePool pool = PoolManager.Instance?.GetPool(PoolType.Coin);
    if (pool != null)
    {
        GameObject go = pool.Get();
        if (go != null)
        {
            go.transform.position = pos;
            var pickup = go.GetComponent<CoinPickup>();
            if (pickup != null) pickup.AssignPool(pool);
            return;
        }
    }
    Instantiate(prefab, pos, Quaternion.identity);
}
```

记得 `Enemy.cs` 顶部加 `using DeadCells.Data;`。

### Step 5 · UIManager.cs 金币文本（遵守项目规范：Awake 自动获取）

`UIManager.cs` 加字段 + Awake 获取 + Start 订阅 + OnDestroy 退订 + 更新方法：

```csharp
// 字段区（在 gameOverPanel 下面）
private Text coinText;   // using UnityEngine.UI 已有

// Awake 里（healthBarFill 获取后面加）
coinText = canvas.Find("CoinText")?.GetComponent<Text>();

// Start 里（现有订阅后面加）
EventCenter.Instance.AddListener<int>(
    EventCenter.EventType.CoinChange.ToString(), UpdateCoinText);

// 新方法
public void UpdateCoinText(int coins)
{
    if (coinText != null) coinText.text = coins.ToString();
}

// OnDestroy 里（现有退订后面加）
EventCenter.Instance.RemoveListener<int>(
    EventCenter.EventType.CoinChange.ToString(), UpdateCoinText);
```

> 🎯 面试点：**UI 引用全部 Awake 自动获取 + 事件订阅放 Start（等 EventCenter 就绪）+ OnDestroy 退订**——三条都是你踩过的坑，主动讲。

### Step 6 · 场景配置（Unity 里手动做，30 分钟）

1. **金币 Prefab**：新建空 GameObject → 挂 SpriteRenderer（用 Kenney 金币图）+ CircleCollider2D（勾 **Is Trigger**）+ `CoinPickup` 组件 → 存成 Prefab `Assets/Prefabs/Coin.prefab`
2. **LootTable 资产**：右键 → Create → Combat → Loot Table → 命名 `SlimeLoot` → 加一个条目：Coin prefab、weight 10、min 1、max 3
3. **敌人关联**：场景里 SlimeEnemy（或它的 Prefab）→ Inspector 拖 `lootTable = SlimeLoot`
4. **PlayerWallet**：Player 物体上 Add Component → PlayerWallet
5. **CoinText**：Canvas 下新建 Text（改名 `CoinText`，右上角锚点），字体大小 24
6. **CoinPool**：`-Pools-` 下建空物体改名 `CoinPool`，挂 `SamplePool`，prefab = Coin，prewarm 10
7. **PoolManager 关联**：PoolManager 的 OnValidate 会自动找到 CoinPool（没有就手动拖）

## ④ 验收标准

- [ ] 杀一只史莱姆 → 掉 1-3 个金币（数量随机）
- [ ] 走近金币 → 自动磁吸飞过来 → 碰到加钱
- [ ] 右上角 CoinText 实时 +N
- [ ] 连续杀 20 只：Console 无报错、无 GC 尖峰（Profiler 看一眼）
- [ ] 金币用完（池空）不崩，继续掉

## ⑤ 常见坑

1. **Trigger 不触发**：Collider 没勾 Is Trigger，或两个碰撞体一个没 Collider
2. **金币不磁吸**：Player 的 Tag 不是 "Player"（检查 Player 物体 Tag）
3. **UI 不更新**：CoinText 名字/层级和 transform.Find 路径不一致；订阅没成功（EventCenter 初始化顺序）
4. **池子串池**：PoolType 枚举、字段、switch、OnValidate 四个地方漏改其一 → 编译错误或拿错池
5. **金币堆叠**：偏移随机范围太小 → 调大 offset 范围

## ⑥ 做完后自问（面试模拟）

- 权重随机为什么用"随机点落区间"而不是"Random.value < weight/total"逐个独立判断？（后者多次判断会让高权重条目被低权重抢先，分布不均）
- 为什么金币走对象池而 Boss 掉落的神器可以不走？（频率决定，神器一次性）
- 如果策划要"精英怪必掉 2 倍金币"，改哪里？（LootTable 加一个 multiplier 字段或精英怪单独一张表——**只改配置/引用，不改代码**，这就是数据驱动的说服力）
