using System.Collections.Generic;
using System;
using UnityEngine;
namespace DeadCells.Data
{
    [CreateAssetMenu(fileName = "NewLootTable",menuName = "Combat/Loot Table")]
    public class LootTable:ScriptableObject
    {
        [Serializable]
        public class LootEntry
        {
            public GameObject itemPrefab;
            [Range(0f, 100f)] public float weight = 1f;
            public int  minAmount =1;
            public int  maxAmount =1;
        }
        public List<LootEntry> entries = new List<LootEntry>();

        public LootEntry Roll()
        {
            if (entries.Count == 0) return null;
            float total = 0;
            foreach (var e in entries) total += e.weight;
            float r = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            foreach (var e in entries)
            {
                acc += e.weight;
                if (r <= acc) return e;
            }
            return entries[entries.Count - 1];
        }
    }
}