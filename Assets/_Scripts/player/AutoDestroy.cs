using UnityEngine;

namespace DeadCells.Player
{
    public class AutoDestroy : MonoBehaviour
{
    void Start()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // 销毁时间 = 粒子时长 + 最大生命周期
            Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(gameObject, 1f); // 保底 1秒销毁
        }
    }
}}
