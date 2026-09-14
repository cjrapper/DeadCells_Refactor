using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Data
{
    /// <summary>
    /// 音效 id —— 要加音效就在这里加一项，然后去 SoundLibrary 资产里拖 clip。
    /// 注意：新增枚举项要加在【末尾】，中间插会让已有资产的 id 全部错位。
    /// </summary>
    public enum SfxId
    {
        // 玩家动作
        Jump, Land, Dash, AttackSwing, LaserFire, Heal,
        // 战斗
        Hit, PlayerHurt, PlayerDeath, EnemyHurt, EnemyDeath,
        // Boss
        Fireball, DiveWarn, DiveImpact,
        // 交互
        Coin, ChestOpen, DoorOpen, DoorLocked, Pickup,
        // UI / 流程
        UIClick, Victory, GameOver,
    }

    /// <summary>
    /// 音效表 —— 所有音效 clip 集中在这一个资产里拖，不用满场景找组件拖。
    /// 没配的 id 就是静音（不报错），所以可以先跑起来再慢慢补素材。
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Data/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SfxId id;

            [Tooltip("可以放多个：每次随机播一个，避免同一个音反复听腻")]
            public AudioClip[] clips;

            [Range(0f, 1f)] public float volume = 1f;

            [Tooltip("音高随机范围（±）：0.08 ≈ 每次音高略有不同，打击音尤其需要")]
            [Range(0f, 0.5f)] public float pitchJitter = 0.08f;
        }

        [Header("音效表")]
        public List<Entry> sfx = new();

        [Header("背景音乐")]
        public AudioClip bgm;
        [Range(0f, 1f)] public float bgmVolume = 0.4f;

        private Dictionary<SfxId, Entry> map;

        /// <summary>查一条音效配置；没配或没 clip 返回 null（调用方静默跳过）</summary>
        public Entry Find(SfxId id)
        {
            if (map == null)
            {
                map = new Dictionary<SfxId, Entry>();
                foreach (Entry e in sfx)
                {
                    if (e == null || e.clips == null || e.clips.Length == 0) continue;
                    map[e.id] = e;   // 重复配同一个 id 时，后面的覆盖前面的
                }
            }
            return map.TryGetValue(id, out Entry found) ? found : null;
        }

        // 编辑器里改了资产 → 清缓存，避免"配了却不生效、要重启才生效"
        private void OnValidate() => map = null;

        // 新建资产时自动把所有 id 铺成一张空表，照着拖 clip 就行
        private void Reset()
        {
            sfx.Clear();
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
                sfx.Add(new Entry { id = id });
        }
    }
}
