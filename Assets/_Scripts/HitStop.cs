using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    //单例模式，便于调用
    private static HitStop instance;
    //防止多帧冲突
    private bool isWaiting;

    void Awake()
    {
        instance = this;
    }
    //
    public static void Stop(float duration)
    {
        if (instance.isWaiting) return;
        instance.StartCoroutine(instance.DoHitStop(duration));
    }
    IEnumerator DoHitStop(float duration)
    {
        isWaiting = true;
        //暂停时间
        Time.timeScale = 0f;
        //
        yield return new WaitForSecondsRealtime(duration);
        //恢复时间
        Time.timeScale = 1f;
        isWaiting = false;
    }
}