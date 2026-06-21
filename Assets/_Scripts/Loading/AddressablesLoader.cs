using UnityEngine;

using AngryBirds.Core;
// 直接使用 AngryBirds.Enemy.Enemy 全限定名，避免命名空间冲突

// 安装 com.unity.addressables 包后自动启用 Addressables 加载
#if HAS_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace AngryBirds.Loading
{
    /// <summary>
    /// Addressables 加载器 —— 替代手写的 ABManager + ABLoader。
    /// 安装 Addressables 包后自动启用。
    /// 安装步骤：Window > Package Manager > Add package by name > com.unity.addressables
    /// </summary>
    public class AddressablesLoader : MonoBehaviour
    {
#if HAS_ADDRESSABLES
        [Header("Addressable Asset References")]
        [SerializeField] private AssetReference enemyPrefabRef;
        [SerializeField] private AssetReference fireballPrefabRef;

        private void Start()
        {
            if (enemyPrefabRef != null && !string.IsNullOrEmpty(enemyPrefabRef.AssetGUID))
            {
                StartCoroutine(LoadEnemyAsync());
            }
            else
            {
                Debug.LogWarning("[AddressablesLoader] enemyPrefabRef 未赋值，跳过敌人加载");
            }

            if (fireballPrefabRef != null && !string.IsNullOrEmpty(fireballPrefabRef.AssetGUID))
            {
                StartCoroutine(LoadFireballAsync());
            }
        }

        private System.Collections.IEnumerator LoadEnemyAsync()
        {
            var handle = Addressables.InstantiateAsync(enemyPrefabRef);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                OnEnemyLoaded(handle.Result);
                Debug.Log($"[AddressablesLoader] 敌人实例化成功: {handle.Result.name}");
            }
            else
            {
                Debug.LogError($"[AddressablesLoader] 敌人加载失败: {handle.OperationException}");
            }
        }

        private System.Collections.IEnumerator LoadFireballAsync()
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(fireballPrefabRef);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                ProjectileCache.FireballPrefab = handle.Result;
                Debug.Log("[AddressablesLoader] Fireball Prefab 加载成功");
            }
            else
            {
                Debug.LogError($"[AddressablesLoader] Fireball 加载失败: {handle.OperationException}");
            }
        }
#else
        private void Start()
        {
            Debug.Log("[AddressablesLoader] Addressables 包未安装。请在 Package Manager 中安装 com.unity.addressables");
        }
#endif

        private void OnEnemyLoaded(GameObject enemyObj)
        {
            var enemyScript = enemyObj.GetComponent<AngryBirds.Enemy.Enemy>();
            if (enemyScript != null && enemyScript.player == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    enemyScript.player = player.transform;
                    Debug.Log("[AddressablesLoader] 已自动为敌人绑定 Player 引用");
                }
                else
                {
                    Debug.LogWarning("[AddressablesLoader] 未找到 Player");
                }
            }

            GameObject dummy = GameObject.Find("DummyEnemy_BT");
            if (dummy != null)
            {
                dummy.SetActive(false);
                Debug.Log("[AddressablesLoader] 已禁用 DummyEnemy_BT");
            }
        }
    }

    /// <summary>
    /// 全局预制体缓存 —— Addressables 加载的 Prefab 暂存于此。
    /// </summary>
    public static class ProjectileCache
    {
        public static GameObject FireballPrefab { get; set; }
    }
}
