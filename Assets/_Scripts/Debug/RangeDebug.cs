using UnityEngine;

namespace DeadCells.Core
{
    /// <summary>
    /// 战斗范围调试的配置 —— 按住 F1（或菜单 Tools/战斗范围显示 常显）显示所有范围。
    ///
    /// 绘制全部在 Assets/Editor/RangeDebugOverlay.cs 里做（Scene 视图，编辑和运行模式都有效）：
    ///   之前运行时走 MonoBehaviour.OnDrawGizmos —— 但 Gizmos 在 Game 视图里
    ///   要额外打开右上角的 Gizmos 按钮才显示，而且编辑模式用 Input.GetKey 又拿不到按键，
    ///   两个毛病叠在一起就是"按了没反应"。统一走编辑器叠加层就没这些事。
    ///
    /// 这个类只放配置，游戏逻辑不依赖它，也不引用 UnityEditor。
    /// </summary>
    public static class RangeDebug
    {
        public const KeyCode ToggleKey = KeyCode.F1;

        /// <summary>常显开关（菜单 Tools/战斗范围显示 控制）—— 按键被吞时的兜底</summary>
        public static bool Forced;

        /// <summary>按住按键 或 菜单打开 == 显示</summary>
        public static bool Visible => Forced || Input.GetKey(ToggleKey);

        // ==================== 配色 ====================
        public static readonly Color Territory = new Color(0.35f, 0.9f, 0.45f);   // 绿：巡逻领地
        public static readonly Color Chase     = new Color(1f, 0.85f, 0.25f);     // 黄：追击/发现
        public static readonly Color Attack    = new Color(1f, 0.35f, 0.35f);     // 红：攻击
        public static readonly Color Vision    = new Color(0.45f, 0.75f, 1f);     // 蓝：视野/保持距离
        public static readonly Color Far       = new Color(0.8f, 0.5f, 1f);       // 紫：远距离阈值
        public static readonly Color Player    = new Color(0.4f, 1f, 0.9f);       // 青：玩家
    }
}
