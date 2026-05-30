using UnityEngine;

/// <summary>
/// ABLoader 演示：使用 ABManager 从 AssetBundle 加载并实例化敌人。
/// 启动时自动执行，完成后输出加载摘要。
/// </summary>
public class ABLoader : MonoBehaviour
{
    private string bundleName = "enemies";
    private string assetName = "assets/abresources/enemy.prefab";

    private void Start()
    {
        if (ABManager.Instance == null)
        {
            Debug.LogError("[ABLoader] ABManager.Instance 为 null！请确保场景中有 ABManager 组件");
            return;
        }

        // 禁用场景里残留的 DummyEnemy_BT，避免两个敌人同时存在
        GameObject dummy = GameObject.Find("DummyEnemy_BT");
        if (dummy != null)
        {
            dummy.SetActive(false);
            Debug.Log("[ABLoader] 已禁用 DummyEnemy_BT（仅保留 AB 版敌人）");
        }

        // ★ 异步初始化，不再卡帧
        ABManager.Instance.InitAsync(() =>
        {
            ABManager.Instance.InstantiateAsync(bundleName, assetName, OnEnemyLoaded);
        });
    }

    private void OnEnemyLoaded(GameObject enemyObj)
    {
        if (enemyObj == null)
        {
            Debug.LogError($"[ABLoader] 实例化失败: {bundleName}/{assetName}");
            return;
        }

        // 自动绑定 Player 引用
        var enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null && enemyScript.player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemyScript.player = player.transform;
                Debug.Log("[ABLoader] 已自动为敌人绑定 Player 引用");
            }
            else
            {
                Debug.LogWarning("[ABLoader] 未找到 Player，敌人可能无法正常行为");
            }
        }

        ABManager.Instance.PrintBundleStatus();
    }
}
