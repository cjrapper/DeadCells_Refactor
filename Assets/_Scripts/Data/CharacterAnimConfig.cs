using UnityEngine;

namespace DeadCells.Data
{
    /// <summary>
    /// 角色动画名配置（ScriptableObject）—— 数据驱动管理动画状态名：
    /// 代码用"逻辑键"（Idle/Attack/Hurt...）请求动画，具体状态名由配置决定。
    /// 好处：换皮 / 多怪物共用同一套代码逻辑，只换配置，动画名不再硬编码。
    /// 配合 AnimatorOverrideController（共用状态机模板 + 每角色替换 AnimationClip）使用最佳。
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterAnimConfig", menuName = "Data/Character Anim Config")]
    public class CharacterAnimConfig : ScriptableObject
    {
        [Header("通用")]
        public string idle = "Idle";
        public string move = "Move";
        public string jump = "Jump";
        public string fall = "Fall";
        public string dash = "Dash";

        [Header("战斗")]
        public string attack = "Attack";
        public string hurt = "Hurt";
        public string death = "Death";
        public string laser = "Laser";
        public string fireball = "Fireball";
        public string dive = "Dive";

        [Header("地面敌人 AI")]
        public string patrol = "Patrol";
        public string chase = "Chase";
        public string telegraph = "Telegraph";

        [Header("飞行敌人 AI")]
        public string flyIdle = "FlyIdle";
        public string fly = "Fly";

        /// <summary>把逻辑键解析成实际动画状态名（未配置的键原样返回）</summary>
        public string Resolve(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            switch (key)
            {
                case "Idle": return idle;
                case "Move": return move;
                case "Jump": return jump;
                case "Fall": return fall;
                case "Dash": return dash;
                case "Attack": return attack;
                case "Hurt": return hurt;
                case "Death": return death;
                case "Laser": return laser;
                case "Fireball": return fireball;
                case "Dive": return dive;
                case "Patrol": return patrol;
                case "Chase": return chase;
                case "Telegraph": return telegraph;
                case "FlyIdle": return flyIdle;
                case "Fly": return fly;
                default: return key;
            }
        }
    }
}
