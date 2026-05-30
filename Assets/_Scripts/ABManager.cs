using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// 资源管理器 —— 手动实现 Addressables 的核心机制
/// 1. Manifest 依赖自动解析（加载主包前先加载其依赖包）
/// 2. 引用计数（同一 bundle 多处引用时只加载一次，全部释放后才 Unload）
/// 3. 异步加载回调 + 协程
/// </summary>
public class ABManager : MonoBehaviour
{
    public static ABManager Instance;

    private AssetBundle manifestBundle;
    private AssetBundleManifest manifest;

    // bundle 名 → 加载信息（引用计数 + AssetBundle 实例）
    private Dictionary<string, BundleRef> loadedBundles = new();

    private class BundleRef
    {
        public AssetBundle Bundle;
        public int RefCount;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// 初始化（同步）：加载 Manifest 包。简单可靠，但会造成一帧卡顿。
    /// </summary>
    public void Init()
    {
        string manifestPath = Path.Combine(Application.streamingAssetsPath, "StreamingAssets");
        manifestBundle = AssetBundle.LoadFromFile(manifestPath);
        if (manifestBundle == null)
        {
            Debug.LogError($"[ABManager] Manifest 包加载失败: {manifestPath}");
            return;
        }
        manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        if (manifest == null)
            Debug.LogError("[ABManager] 读取 AssetBundleManifest 失败！");
        else
            Debug.Log("[ABManager] 初始化完成，Manifest 加载成功");
    }

    /// <summary>
    /// 初始化（异步）：不卡顿，适合正式游戏。回调在主线程执行。
    /// </summary>
    public void InitAsync(System.Action onComplete = null)
    {
        StartCoroutine(InitAsyncCoroutine(onComplete));
    }

    private System.Collections.IEnumerator InitAsyncCoroutine(System.Action onComplete)
    {
        string manifestPath = Path.Combine(Application.streamingAssetsPath, "StreamingAssets");
        var req = AssetBundle.LoadFromFileAsync(manifestPath);
        yield return req;

        manifestBundle = req.assetBundle;
        if (manifestBundle == null)
        {
            Debug.LogError($"[ABManager] Manifest 包加载失败: {manifestPath}");
            onComplete?.Invoke();
            yield break;
        }

        var assetReq = manifestBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
        yield return assetReq;
        manifest = assetReq.asset as AssetBundleManifest;

        if (manifest == null)
            Debug.LogError("[ABManager] 读取 AssetBundleManifest 失败！");
        else
            Debug.Log("[ABManager] 异步初始化完成，Manifest 加载成功");

        onComplete?.Invoke();
    }

    /// <summary>
    /// 同步加载一个 AssetBundle。自动解析并加载所有依赖。
    /// </summary>
    public AssetBundle LoadBundle(string bundleName)
    {
        if (manifest == null)
        {
            Debug.LogError("[ABManager] 未初始化，请先调用 Init()");
            return null;
        }

        // 先加载所有依赖
        string[] deps = manifest.GetAllDependencies(bundleName);
        foreach (string dep in deps)
        {
            if (!loadedBundles.ContainsKey(dep))
                LoadBundleInternal(dep);
            loadedBundles[dep].RefCount++;
        }

        if (!loadedBundles.ContainsKey(bundleName))
            LoadBundleInternal(bundleName);

        if (!loadedBundles.ContainsKey(bundleName)) // 加载失败，未加入字典
            return null;

        loadedBundles[bundleName].RefCount++;
        return loadedBundles[bundleName].Bundle;
    }

    /// <summary>
    /// 异步加载单个 bundle。回调在主线程执行。
    /// </summary>
    public void LoadBundleAsync(string bundleName, Action<AssetBundle> onComplete)
    {
        StartCoroutine(LoadBundleAsyncCoroutine(bundleName, onComplete));
    }

    private IEnumerator LoadBundleAsyncCoroutine(string bundleName, Action<AssetBundle> onComplete)
    {
        if (manifest == null)
        {
            Debug.LogError("[ABManager] 未初始化");
            onComplete?.Invoke(null);
            yield break;
        }

        // 异步加载依赖
        string[] deps = manifest.GetAllDependencies(bundleName);
        foreach (string dep in deps)
        {
            if (!loadedBundles.ContainsKey(dep))
            {
                string depPath = Path.Combine(Application.streamingAssetsPath, dep);
                var req = AssetBundle.LoadFromFileAsync(depPath);
                yield return req;
                if (req.assetBundle == null)
                {
                    Debug.LogError($"[ABManager] 依赖包加载失败: {depPath}");
                    onComplete?.Invoke(null);
                    yield break;
                }
                loadedBundles[dep] = new BundleRef { Bundle = req.assetBundle, RefCount = 0 };
            }
            loadedBundles[dep].RefCount++;
        }

        // 异步加载目标 bundle
        if (!loadedBundles.ContainsKey(bundleName))
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);
            var req = AssetBundle.LoadFromFileAsync(path);
            yield return req;
            if (req.assetBundle == null)
            {
                Debug.LogError($"[ABManager] 目标包加载失败: {path}");
                onComplete?.Invoke(null);
                yield break;
            }
            loadedBundles[bundleName] = new BundleRef { Bundle = req.assetBundle, RefCount = 0 };
        }
        loadedBundles[bundleName].RefCount++;

        onComplete?.Invoke(loadedBundles[bundleName].Bundle);
    }

    /// <summary>
    /// 从指定 bundle 同步加载资源
    /// </summary>
    public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (!loadedBundles.TryGetValue(bundleName, out var bundleRef))
        {
            var b = LoadBundle(bundleName);
            if (b == null) return null;
            bundleRef = new BundleRef { Bundle = b, RefCount = 1 };
            loadedBundles[bundleName] = bundleRef;
        }
        if (bundleRef.Bundle == null) return null;
        return bundleRef.Bundle.LoadAsset<T>(assetName);
    }

    /// <summary>
    /// 从指定 bundle 异步加载资源
    /// </summary>
    public void LoadAssetAsync<T>(string bundleName, string assetName, Action<T> onComplete) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetAsyncCoroutine(bundleName, assetName, onComplete));
    }

    private IEnumerator LoadAssetAsyncCoroutine<T>(string bundleName, string assetName, Action<T> onComplete) where T : UnityEngine.Object
    {
        if (!loadedBundles.TryGetValue(bundleName, out var bundleRef))
        {
            yield return LoadBundleAsyncCoroutine(bundleName, b => bundleRef = loadedBundles[bundleName]);
        }

        var assetReq = bundleRef.Bundle.LoadAssetAsync<T>(assetName);
        yield return assetReq;
        onComplete?.Invoke(assetReq.asset as T);
    }

    /// <summary>
    /// 异步实例化一个 Prefab
    /// </summary>
    public void InstantiateAsync(string bundleName, string assetName, Action<GameObject> onComplete)
    {
        StartCoroutine(InstantiateAsyncCoroutine(bundleName, assetName, onComplete));
    }

    private IEnumerator InstantiateAsyncCoroutine(string bundleName, string assetName, Action<GameObject> onComplete)
    {
        if (!loadedBundles.TryGetValue(bundleName, out var bundleRef))
        {
            yield return LoadBundleAsyncCoroutine(bundleName, b => bundleRef = loadedBundles[bundleName]);
        }

        var assetReq = bundleRef.Bundle.LoadAssetAsync<GameObject>(assetName);
        yield return assetReq;

        GameObject prefab = assetReq.asset as GameObject;
        var obj = prefab != null ? Instantiate(prefab) : null;
        onComplete?.Invoke(obj);
    }

    /// <summary>
    /// 释放对 bundle 的引用。引用计数归零时自动 Unload。
    /// unloadAllLoadedObjects: true=同时销毁该 bundle 实例化的所有物体
    /// </summary>
    public void Unload(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (!loadedBundles.TryGetValue(bundleName, out var bundleRef)) return;

        bundleRef.RefCount--;
        if (bundleRef.RefCount <= 0)
        {
            bundleRef.Bundle?.Unload(unloadAllLoadedObjects);
            loadedBundles.Remove(bundleName);
        }
    }

    /// <summary>
    /// 强制卸载所有 bundle（适合场景切换时调用）
    /// </summary>
    public void UnloadAll(bool unloadAllLoadedObjects = true)
    {
        foreach (var kv in loadedBundles)
            kv.Value.Bundle?.Unload(unloadAllLoadedObjects);
        loadedBundles.Clear();
        AssetBundle.UnloadAllAssetBundles(unloadAllLoadedObjects);
    }

    // ==================== 调试 ====================

    public int LoadedBundleCount => loadedBundles.Count;

    public void PrintBundleStatus()
    {
        Debug.Log($"[ABManager] 已加载 {loadedBundles.Count} 个 bundle:");
        foreach (var kv in loadedBundles)
            Debug.Log($"  {kv.Key} — 引用计数: {kv.Value.RefCount}");
    }

    private void LoadBundleInternal(string bundleName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, bundleName);
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Debug.LogError($"[ABManager] Bundle 加载失败: {path}");
            return; // ★ 不把 null 加入字典，避免后续 NRE
        }
        loadedBundles[bundleName] = new BundleRef { Bundle = bundle, RefCount = 0 };
    }

    private void OnDestroy()
    {
        UnloadAll(false);
    }
}
