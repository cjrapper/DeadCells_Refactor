using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class SamplePool : MonoBehaviour
{
    public GameObject prefab;
    public int prewarmCount = 10;
    public int maxPoolSize = 30;
    private Queue<GameObject> pool = new Queue<GameObject>();//对象池
    private int createdCount;

    void Awake()
    {
        if (prefab == null) return;
        int count = prewarmCount;
        if (maxPoolSize > 0) count = Mathf.Min(prewarmCount, maxPoolSize);
        for (int i = 0; i < count; i++)
        {
            var obj = CreateInstance();
            pool.Enqueue(obj);
        }
    }

    GameObject CreateInstance()
    {
        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        createdCount++;
        return obj;
    }

    public GameObject Get()
    {
        if(pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        if (prefab == null) return null;
        if (maxPoolSize > 0 && createdCount >= maxPoolSize) return null;
        var newObj = CreateInstance();
        newObj.SetActive(true);
        return newObj;
    }

    public GameObject Return(GameObject obj)
    {
        if (obj == null) return null;
        if (maxPoolSize > 0 && pool.Count >= maxPoolSize)
        {
            createdCount = Mathf.Max(0, createdCount - 1);
            Destroy(obj);
            return null;
        }
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
        return obj;
    }
}
