using UnityEngine;
using System.Collections.Generic;

namespace DeadCells.Save
{
    [System.Serializable]
    public class SaveData
    {
        public string slotName = "未命名存档";
        public string saveTime;
        public int playTimeSeconds;

        //属性
        public int maxHealth = 100;
        public int currentHealth = 100;

        public float posX, posY, posZ;
        public string sceneName;

        //武器
        public List<string> weaponNames = new();
        public int currentWeaponIndex;
    }
}