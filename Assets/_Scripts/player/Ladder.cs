using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 梯子 —— 标记组件 + 提供"脚下还有没有梯子"的几何查询。
    /// 爬梯逻辑在 Lua 状态机（Logic/Player/State/Base/ladder.lua），C# 只提供判定。
    /// </summary>
    public class Ladder : MonoBehaviour
    {
        private Collider2D col;

        private void Awake()
        {
            // 缓存碰撞体（Awake 取一次，不在运行时反复 GetComponent）
            col = GetComponent<Collider2D>();
        }

        /// <summary>
        /// worldY 下方是否还有可攀爬的梯子段。
        /// 用途：Lua 侧判断"按 ↓ 到底能不能真的往下爬" —— 站在梯子底端的地面上时返回 false。
        /// 拿不到碰撞体就返回 true（判断不了就按旧行为"能爬"，不制造新的不能爬的情况）。
        /// </summary>
        public bool HasRungBelow(float worldY, float margin = 0.2f)
        {
            if (col == null) return true;
            return worldY - margin > col.bounds.min.y;
        }
    }
}
