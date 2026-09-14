using UnityEngine;

namespace DeadCells.Core
{
    /// <summary>
    /// 相机震动 —— 无依赖版本，挂在 Main Camera 上即可。
    ///
    /// 为什么不用 Cinemachine 的 Impulse：
    ///   关卡场景里没有 Cinemachine（没有 Brain / VirtualCamera / ImpulseListener），
    ///   所以 PlayerHealth 上的 CinemachineImpulseSource 调了也【完全没有效果】——
    ///   "接了震屏但打起来没感觉"就是这种静默失效。
    ///   这个脚本只有一个 Transform 操作，装上去立刻能用。
    ///
    /// 强度约定（trauma 0~1）：
    ///   0.15 轻（被打）  0.3 中（命中敌人）  0.6 重（Boss 落地 / 玩家死亡）
    /// </summary>
    [DefaultExecutionOrder(100)]   // 放在其它相机逻辑之后跑
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Tooltip("最大位移（世界单位）")]
        public float maxOffset = 0.35f;

        [Tooltip("每秒衰减多少 trauma。越大收得越快")]
        public float damping = 1.8f;

        [Tooltip("抖动频率")]
        public float frequency = 22f;

        private float trauma;
        private float seed;
        private Vector3 lastOffset;   // 上一帧加过的偏移，每帧先减掉再重算

        private void Awake()
        {
            Instance = this;
            seed = Random.value * 100f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>叠加一次震动（会累加，连续命中会越来越强，但被 clamp 到 1）</summary>
        public static void Shake(float amount)
        {
            if (Instance == null || amount <= 0f) return;
            Instance.trauma = Mathf.Clamp01(Instance.trauma + amount);
        }

        private void LateUpdate()
        {
            // 先把上一帧加的偏移减掉：这样即使有别的脚本在控制相机位置（跟随/边界钳制），
            // 也不会和震动互相打架。每帧都以"相机自己的位置"为基准重新叠加。
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;

            if (trauma <= 0f) return;

            trauma = Mathf.Max(0f, trauma - damping * Time.unscaledDeltaTime);

            // 平方衰减：小震动收得快，大震动有冲击感（比线性自然）
            float k = trauma * trauma;
            float t = Time.unscaledTime * frequency + seed;

            // Perlin 噪声而不是纯随机：避免高频"抽搐"，抖起来更像手持镜头
            float x = (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * maxOffset * k;
            float y = (Mathf.PerlinNoise(0f, t) * 2f - 1f) * maxOffset * k;

            lastOffset = new Vector3(x, y, 0f);
            transform.localPosition += lastOffset;
        }
    }
}
