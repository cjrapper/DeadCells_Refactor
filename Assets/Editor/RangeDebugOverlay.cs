using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using DeadCells.Core;
using DeadCells.Enemy;
using DeadCells.Player;

/// <summary>
/// 战斗范围调试叠加层 —— 按住 ` 键，在 Scene 视图里画出所有敌人 + 玩家的范围。
///
/// 为什么用 SceneView.duringSceneGui 而不是 MonoBehaviour.OnDrawGizmos：
///   ① OnDrawGizmos 里用 Input.GetKey 在【编辑模式】拿不到按键（Game 视图没焦点就没输入），
///      所以"按住键才显示"在编辑模式下根本不生效；
///   ② OnDrawGizmosSelected / CustomEditor.OnSceneGUI 都要求【先选中物体】，
///      怪物一多就没法横向对比；
///   ③ SceneView 的键盘事件（KeyDown/KeyUp）在编辑模式下是可靠的。
///
/// 只在编辑模式绘制 —— 运行模式交给各脚本的 OnDrawGizmos，避免同一个圈画两遍。
/// 这个文件在 Assets/Editor 下，不会进正式包。
/// </summary>
[InitializeOnLoad]
public static class RangeDebugOverlay
{
    private static bool held;

    /// <summary>菜单兜底：按键被吞掉时用这个</summary>
    [MenuItem("Tools/战斗范围显示 %#r")]
    private static void ToggleForced()
    {
        RangeDebug.Forced = !RangeDebug.Forced;
        SceneView.RepaintAll();
        Debug.Log(RangeDebug.Forced
            ? "[RangeDebug] 战斗范围：常显（菜单再点一次取消）"
            : "[RangeDebug] 战斗范围：关闭");
    }

    [MenuItem("Tools/战斗范围显示 %#r", true)]
    private static bool ToggleForcedValidate() => true;

    private static bool Showing => held || RangeDebug.Forced;

    static RangeDebugOverlay()
    {
        SceneView.duringSceneGui += OnSceneGui;
        EditorApplication.update += PollKeyFallback;
        AssemblyReloadEvents.beforeAssemblyReload += () => held = false;
    }

    // ==================== 按键状态 ====================

    /// <summary>优先用 Scene 视图的键盘事件（编辑模式下最可靠）</summary>
    private static void OnSceneGui(SceneView view)
    {
        Event e = Event.current;
        if (e != null)
        {
            if (e.type == EventType.KeyDown && e.keyCode == RangeDebug.ToggleKey)
            {
                SetHeld(true);
                e.Use();          // 吃掉按键，避免 Scene 视图拿它做别的事
            }
            else if (e.type == EventType.KeyUp && e.keyCode == RangeDebug.ToggleKey)
            {
                SetHeld(false);
                e.Use();
            }
        }

        if (!Showing) return;

        DrawAll();
        view.Repaint();       // 按住期间持续重绘（否则 Scene 视图不动就不刷新）
    }

    /// <summary>兜底：焦点在 Game 视图时 Scene 视图收不到键盘事件，这里轮询一次</summary>
    private static void PollKeyFallback()
    {
        bool now = Input.GetKey(RangeDebug.ToggleKey);
        if (now != held) SetHeld(now);
    }

    private static void SetHeld(bool value)
    {
        if (held == value) return;
        held = value;
        SceneView.RepaintAll();
        Debug.Log(value
            ? $"[RangeDebug] 显示战斗范围（松开 {RangeDebug.ToggleKey} 隐藏；菜单 Tools/战斗范围显示 可常显）"
            : "[RangeDebug] 隐藏战斗范围");
    }

    // ==================== 绘制 ====================

    private static void DrawAll()
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        foreach (Enemy e in Object.FindObjectsOfType<Enemy>())
        {
            if (e == null) continue;
            DrawEnemy(e);
        }

        PlayerCombat pc = Object.FindObjectOfType<PlayerCombat>();
        if (pc != null) DrawPlayer(pc);
    }

    private static void DrawEnemy(Enemy e)
    {
        Vector3 c = e.transform.position;

        // 领地：以出生点为圆心（编辑模式下 startPos 可能是 (0,0,0)，退回当前位置）
        Vector3 home = Application.isPlaying ? e.startPos : e.transform.position;
        Disc(home, e.territoryRange, RangeDebug.Territory, $"领地 {e.territoryRange}");

        Disc(c, e.chaseRange, RangeDebug.Chase, $"追击 {e.chaseRange}");

        // 攻击范围挂在 attackPos 上（和实际判定点一致）
        Vector3 ap = e.attackPos != null ? e.attackPos.position : c;
        Disc(ap, e.attackRange, RangeDebug.Attack, $"攻击 {e.attackRange}");

        // 视野高度：上下两条横线
        Line(c + Vector3.left * 0.6f + Vector3.up * e.visionHeight,
             c + Vector3.right * 0.6f + Vector3.up * e.visionHeight,
             RangeDebug.Vision, $"视野高度 ±{e.visionHeight}");
        Line(c + Vector3.left * 0.6f - Vector3.up * e.visionHeight,
             c + Vector3.right * 0.6f - Vector3.up * e.visionHeight,
             RangeDebug.Vision);

        if (e.laserTriggerRange > 0f)
            Disc(c, e.laserTriggerRange, RangeDebug.Far, $"激光触发 {e.laserTriggerRange}");

        // 飞行敌人专属阈值
        if (e is FlyingEnemy f)
        {
            Disc(c, f.idleRange, RangeDebug.Chase, $"发现 {f.idleRange}");
            Disc(c, f.loseRange, RangeDebug.Far, $"丢失 {f.loseRange}");
            Disc(c, f.keepDistance, RangeDebug.Vision, $"保持距离 {f.keepDistance}");
            Disc(c, f.fireballRange, RangeDebug.Attack, $"火球 {f.fireballRange}");
            Disc(c, f.diveTriggerRange, RangeDebug.Territory, $"下坠触发 {f.diveTriggerRange}");
            Disc(c, f.diveHitRadius, RangeDebug.Attack, $"下坠命中 {f.diveHitRadius}");
            Disc(c, f.returnHomeDistance, RangeDebug.Territory, $"回巢阈值 {f.returnHomeDistance}");
        }
    }

    private static void DrawPlayer(PlayerCombat pc)
    {
        Transform origin = pc.attackOrigin != null ? pc.attackOrigin : pc.transform;

        float range = pc.currentWeapon != null ? pc.currentWeapon.attackRange : 0f;
        Disc(origin.position, range, RangeDebug.Attack, range > 0f ? $"玩家攻击 {range}" : null);

        // 冲刺距离（冲刺速度 × 时长）—— 调跳跃/过坑时很有用
        PlayerMovement mv = pc.GetComponent<PlayerMovement>();
        if (mv != null)
        {
            float dashDist = mv.dashSpeed * mv.dashTime;
            float face = pc.transform.localScale.x >= 0f ? 1f : -1f;
            Line(pc.transform.position,
                 pc.transform.position + new Vector3(face * dashDist, 0f, 0f),
                 RangeDebug.Player, $"冲刺距离 {dashDist:F1}");
        }
    }

    // ==================== 绘制工具（和 SlimeEditor 同款：半透明实心 + 描边） ====================

    private static void Disc(Vector3 center, float radius, Color color, string label)
    {
        if (radius <= 0f) return;

        Handles.color = new Color(color.r, color.g, color.b, 0.07f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        Handles.color = color;
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        if (!string.IsNullOrEmpty(label))
        {
            Handles.color = Color.white;
            Handles.Label(center + Vector3.up * radius, label);
        }
    }

    private static void Line(Vector3 from, Vector3 to, Color color, string label = null)
    {
        Handles.color = color;
        Handles.DrawLine(from, to);

        if (!string.IsNullOrEmpty(label))
        {
            Handles.color = Color.white;
            Handles.Label(to, label);
        }
    }
}
