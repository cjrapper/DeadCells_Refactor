using UnityEditor; 
using UnityEngine;

public class MyTools 
{
    // 魔法标签：直接在 Unity 顶部导航栏生成一个下拉菜单！
    [MenuItem("Tools/生成史莱姆")] 
    public static void SpawnSlimeCheat() 
    {
        // 1. 编辑器模式下的终极白嫖：不用去解压 AB 包！直接从工程目录强行读取 Prefab！
        // （注意把下面路径换成你史莱姆 Prefab 在项目里的真实路径，比如 Assets/Prefabs/Slime.prefab）
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ABResources/Enemy.prefab");

        // 2. 扔进场景里！
        if (slimePrefab != null) {
            GameObject.Instantiate(slimePrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("史莱姆已空投至原点！");
        } else {
            Debug.LogError("找不到预制体，路径写错了吧！");
        }
    }
}