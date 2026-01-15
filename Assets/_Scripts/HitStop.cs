using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    // Singleton instance for global access
    private static HitStop instance;
    // Flag to prevent overlapping hit stops
    private bool isWaiting;

    void Awake()
    {
        instance = this;
    }

    // Static method to trigger hit stop from anywhere
    public static void Stop(float duration)
    {
        if (instance.isWaiting) return;
        instance.StartCoroutine(instance.DoHitStop(duration));
    }

    IEnumerator DoHitStop(float duration)
    {
        isWaiting = true;
        
        // Freeze game time
        Time.timeScale = 0f;
        
        // Wait for real time (unaffected by timeScale)
        yield return new WaitForSecondsRealtime(duration);
        
        // Restore game time
        Time.timeScale = 1f;
        
        isWaiting = false;
    }
}
