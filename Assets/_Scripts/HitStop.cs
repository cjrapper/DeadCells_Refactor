using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    // Singleton instance for global access
    private static HitStop instance;
    // Flag to prevent overlapping hit stops
    private bool isWaiting;
    private float previousTimeScale = 1f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Static method to trigger hit stop from anywhere
    public static void Stop(float duration)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("HitStop");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStop>();
        }
        if (instance.isWaiting) return;
        instance.StartCoroutine(instance.DoHitStop(duration));
    }

    IEnumerator DoHitStop(float duration)
    {
        isWaiting = true;
        previousTimeScale = Time.timeScale;

        // Freeze game time
        Time.timeScale = 0f;

        // Wait for real time (unaffected by timeScale)
        yield return new WaitForSecondsRealtime(duration);

        // Restore to previous timeScale
        Time.timeScale = previousTimeScale;

        isWaiting = false;
    }
}
