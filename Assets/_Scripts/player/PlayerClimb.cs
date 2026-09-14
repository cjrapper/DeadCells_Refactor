using UnityEngine;
using XLua;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家爬梯收集器 —— 玩家碰撞体与 Ladder 触发范围重叠时记录当前梯子。
    /// Lua 状态机通过 c.PlayerClimb.IsOnLadder 判断是否可爬。
    /// </summary>
    [LuaCallCSharp]
    public class PlayerClimb : MonoBehaviour
    {
        private Ladder currentLadder;
        private Collider2D bodyCollider;

        /// <summary>是否在梯子触发范围内（Lua 状态机查询）</summary>
        public bool IsOnLadder => currentLadder != null;

        /// <summary>
        /// 脚下的梯子是否还有往下爬的余量（脚底已经贴到梯子底端时为 false）。
        /// 这一条是防"ground ↔ ladder 状态机抖动"的关键：
        /// 如果站在梯子底端按 ↓ 也算"能往下爬"，ground 会立刻切进 ladder，
        /// 而 ladder 又立刻因"触地且按着 ↓"切回 ground —— 两者每帧互相打架。
        /// </summary>
        public bool HasLadderBelow
        {
            get
            {
                if (currentLadder == null) return false;
                // 用"脚底"而不是"身体中心"去比：站在地面上时脚底 ≈ 地面高度 ≈ 梯子底端
                float feetY = bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y;
                return currentLadder.HasRungBelow(feetY);
            }
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (currentLadder != null) return;
            Ladder ladder = other.GetComponent<Ladder>();
            if (ladder != null)
                currentLadder = ladder;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // 只清自己的梯子（多梯子相邻时互不干扰）
            if (currentLadder != null && other.GetComponent<Ladder>() == currentLadder)
                currentLadder = null;
        }
    }
}
