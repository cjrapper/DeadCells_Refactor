using UnityEngine;
// using DG.Tweening; // 如果你有DoTween，没有就用协程

using AngryBirds.Core;

namespace AngryBirds.Player
{
    public class GhostEffect : MonoBehaviour
{
    public float fadeTime = 0.5f;
    private SpriteRenderer sr;
    private SamplePool pool;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // 初始化：设置图片和位置
    public void Init(Sprite sprite, Vector3 pos, Quaternion rot, Vector3 scale, SamplePool pool)
    {
        this.pool = pool;
        if (sr == null)
        {
            if (pool != null) pool.Return(gameObject);
            return;
        }
        sr.sprite = sprite;
        transform.position = pos;
        transform.rotation = rot;
        transform.localScale = scale;
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
        
        // 自动销毁逻辑 (协程版)
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        float timer = 0;
        Color startColor = sr.color;
        
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0, timer / fadeTime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        // 回收或销毁 (简单起见先Destroy，以后改对象池)
        if (pool != null)
        {
            pool.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
}
