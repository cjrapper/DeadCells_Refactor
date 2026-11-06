using UnityEngine;

using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Player;

namespace DeadCells.Interaction
{
    /// <summary>
    /// 宝箱 —— 数据驱动掉落：箱子里掉什么由 LootTable 资产决定（和敌人掉落同一套）。
    /// 交互：按 E 开（或 autoOpen 碰到就开），开一次，换贴图 + 播金币。
    /// </summary>
    public class TreasureChest : MonoBehaviour
    {
        [Header("表现")]
        public Sprite openSprite;        // 开箱后的贴图

        [Header("头顶提示")]
        [Tooltip("玩家靠近时在箱子正上方显示的文字（留空 = 不显示）")]
        public string promptText = "Press E";

        [Header("掉落（数据驱动：填一个金币 LootTable 资产）")]
        public LootTable lootTable;

        [Header("音效")]
        public AudioClip openSound;    // 开箱音效（没配就是静音，UI/箱子的"不明显"一半是没音效）

        [Header("交互")]
        [Tooltip("true = 玩家碰到自动开；false = 按 E 开")]
        public bool autoOpen = false;

        private SpriteRenderer sr;
        private Renderer cachedRenderer;   // 缓存：用来算"头顶"位置，不能每帧 GetComponent
        private bool isOpened;
        private bool playerNear;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            cachedRenderer = GetComponentInChildren<Renderer>();
        }

        /// <summary>提示锚点 = 贴图顶边中点（用包围盒算，贴图多大都能顶在头上，不用逐个配高度）</summary>
        private Vector3 PromptAnchor
        {
            get
            {
                if (cachedRenderer != null)
                    return new Vector3(transform.position.x, cachedRenderer.bounds.max.y, 0f);
                return transform.position + Vector3.up * 1.2f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerNear = true;
            if (autoOpen) Open();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerNear = false;
            InteractionPrompt.Hide(this);
        }

        private void Update()
        {
            if (!playerNear) return;

            // 已经开过的箱子不再提示（否则站在旁边会一直显示 "Press E"）
            if (!isOpened && !string.IsNullOrEmpty(promptText))
                InteractionPrompt.Show(this, promptText, PromptAnchor);

            if (!autoOpen && Input.GetKeyDown(KeyCode.E))
                Open();
        }

        private void Open()
        {
            if (isOpened) return;
            isOpened = true;

            // 1. 换开箱贴图
            if (sr != null && openSprite != null) sr.sprite = openSprite;

            // 2. 关提示（本帧起 Update 不再上报，提示框下一帧自动隐藏）
            InteractionPrompt.Hide(this);

            // 3. 掉落（和敌人同一套掉落表 + 金币池）
            SpawnLoot();

            // 4. 音效：统一走 SoundLibrary（openSound 是旧字段，保留兼容；
            //    两个都配会播两次，迁移完就把 openSound 清空）
            Feedback.SoundAt(SfxId.ChestOpen, transform.position);
            if (openSound != null)
                AudioSource.PlayClipAtPoint(openSound, transform.position);

            Debug.Log("[TreasureChest] 宝箱已打开");
        }

        private void SpawnLoot()
        {
            if (lootTable == null) return;
            var entry = lootTable.Roll();
            if (entry == null || entry.itemPrefab == null) return;

            int count = Random.Range(entry.minAmount, entry.maxAmount + 1);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0.3f, 0.8f), 0f);
                SpawnItem(entry.itemPrefab, transform.position + offset);
            }
        }

        private void SpawnItem(GameObject prefab, Vector3 pos)
        {
            // 优先走金币池（和 Enemey.SpawnLoot 同款降级逻辑：池不可用就 Instantiate）
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
    }
}
