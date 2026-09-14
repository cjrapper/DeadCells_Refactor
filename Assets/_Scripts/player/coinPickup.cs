using UnityEngine;
using DeadCells.Core;
namespace DeadCells.Player
{
    public class CoinPickup:MonoBehaviour
    {
        public int value = 1;
        public float magnetRadius = 2f;
        public float magnetSpeed = 8f;

        private Transform player;
        private SamplePool pool;
        private bool hasPool;

        private void Awake()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        private void Update()
        {
            if (player == null) return;
            float dist =Vector2.Distance(transform.position, player.position);
            if (dist < magnetRadius && dist > 0.05f)
            {
                Vector3 dir = (player.transform.position-transform.position).normalized;
                transform.position +=dir*(magnetSpeed*Time.deltaTime);
            }
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            // Unity 的 Tag 区分大小写：玩家的 Tag 是 "Player"（大写 P），
            // 写 "player" 永远匹配不上 → 金币永远不拾取（已踩坑两次，背下来）
            if (!other.CompareTag("Player")) return;
            var wallet = other.GetComponent<PlayerWallet>();
            if (wallet != null) wallet.AddCoins(value);

            // 拾取音（音高随机开得比较大 —— 连续捡一堆金币时是"叮叮叮"而不是同一个音重复）
            DeadCells.Core.Feedback.SoundAt(DeadCells.Data.SfxId.Coin, transform.position);

            if (hasPool && pool != null) pool.Return(gameObject);
            else Destroy(gameObject);
        }

        public void OnGetFromPool()
        {
            
        }

        public void AssignPool(SamplePool _pool)
        {
            pool = _pool;
            hasPool = pool != null;
        }
    }
}