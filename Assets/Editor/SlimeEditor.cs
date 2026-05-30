using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SlimeEnemy))]
public class SlimeEditor : Editor
{
    private Enemy enemy;
    private void OnEnable()
    {
        enemy = (Enemy)target;
    }
   public override void OnInspectorGUI()
   {
     base.OnInspectorGUI();
     EditorGUILayout.Space();
     EditorGUILayout.LabelField("调试信息",EditorStyles.boldLabel);

     if(Application.isPlaying && enemy.StateMachine != null)
     {
        EditorGUILayout.LabelField($"当前状态：{enemy.StateMachine.CurrentState?.GetType().Name ?? "无"}");
        Repaint();
     }
   }
   private void OnSceneGUI()
   {
    if(enemy == null)return;

    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
    Vector3 center = enemy.transform.position;

    Handles.color = new Color(0,1,0,0.1f);
    Handles.DrawSolidDisc(center,Vector3.forward,enemy.chaseRange);
    Handles.color = Color.green;
    Handles.DrawWireDisc(center,Vector3.forward,enemy.chaseRange);

    Vector3 attackCenter = enemy.attackPos != null ? enemy.attackPos.position : center;
    Handles.color = new Color(1,0,0,0.1f);
    Handles.DrawSolidDisc(attackCenter,Vector3.forward,enemy.attackRange); 
    Handles.color = Color.red;
    Handles.DrawWireDisc(attackCenter,Vector3.forward,enemy.attackRange);

    Handles.Label(center + Vector3.up * (enemy.chaseRange+0.5f),"巡逻范围");
    Handles.Label(attackCenter + Vector3.up * (enemy.attackRange+0.5f),"攻击范围");

    Repaint();
   }

}
