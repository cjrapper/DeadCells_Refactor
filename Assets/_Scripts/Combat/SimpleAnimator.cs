using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// 简单动画器 —— 两种能力二合一，挂弹丸/特效上：
    /// ① 帧动画：把贴图切片后的 Sprite 拖进 frames 数组按 fps 循环播放（火焰/脉冲）；
    /// ② 自转：spinDegreesPerSecond 非 0 时缓慢旋转（火球打转的"活"感）。
    /// 不需要 Animator 控制器，配置即用。
    /// </summary>
    public class SimpleAnimator : MonoBehaviour
    {
        [Header("帧动画（贴图切片后的 Sprite 多选拖入）")]
        public Sprite[] frames;
        public float fps = 12f;
        public bool loop = true;

        [Header("自转（可选）")]
        [Tooltip("每秒旋转角度；0 = 不转")]
        public float spinDegreesPerSecond = 0f;

        private SpriteRenderer sr;
        private float timer;
        private int index;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // 帧播放（零分配：不 new 对象，只换 sprite 引用）
            if (sr != null && frames != null && frames.Length > 0)
            {
                timer += Time.deltaTime;
                float frameTime = 1f / Mathf.Max(0.01f, fps);
                while (timer >= frameTime)
                {
                    timer -= frameTime;
                    index++;
                    if (index >= frames.Length)
                    {
                        index = loop ? 0 : frames.Length - 1;
                    }
                }
                sr.sprite = frames[index];
            }

            // 自转
            if (spinDegreesPerSecond != 0f)
                transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        }
    }
}
