using UnityEngine;
using UnityEditor;

using DeadCells.Enemy;

[CustomEditor(typeof(SlimeEnemy))]
public class SlimeEditor : Editor
{
    private DeadCells.Enemy.Enemy enemy;

    private void OnEnable()
    {
        enemy = (DeadCells.Enemy.Enemy)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("调试信息", EditorStyles.boldLabel);

        if (Application.isPlaying && enemy.StateMachine != null)
        {
            EditorGUILayout.LabelField($"当前状态：{enemy.StateMachine.CurrentState?.GetType().Name ?? "无"}");
            Repaint();
        }
    }

    private void OnSceneGUI()
    {
        // 范围绘制统一交给 Assets/Editor/RangeDebugOverlay.cs：
        //   按住 ` 键就能看到【所有】敌人 + 玩家的范围，不用先选中物体。
        //   这里如果继续画，选中史莱姆时会出现两套圈（叠在一起还不好分辨）。
        //   本文件只保留 Inspector 里的"当前状态"显示。
    }
}
