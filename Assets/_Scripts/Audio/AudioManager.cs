using UnityEngine;

using DeadCells.Data;

namespace DeadCells.Core
{
    /// <summary>
    /// 音频管理器 —— 音效播放 + 背景音乐，全局一份。
    ///
    /// 用法（任何地方一行调用，不用持有引用）：
    ///     AudioManager.Play(SfxId.Jump);
    ///     AudioManager.PlayAt(SfxId.Hit, transform.position);   // 带位置（有距离衰减）
    ///
    /// 挂在 GameManager 上（它已经是 DontDestroyOnLoad，所以音乐跨场景不断）。
    /// 所有 clip 拖在 SoundLibrary 资产里，组件上只需要拖那一个资产。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Tooltip("音效表资产：所有 clip 都在那一个资产里拖")]
        public SoundLibrary library;

        [Tooltip("同时最多播几个音效（声部池大小）。太小会互相打断，太大多余")]
        [Range(4, 32)] public int sfxVoices = 12;

        [Range(0f, 1f)] public float masterVolume = 1f;

        [Tooltip("进游戏自动播 SoundLibrary 里的 BGM")]
        public bool playBgmOnStart = true;

        [Tooltip("有位置的音效用多少 3D 衰减（0 = 全 2D）")]
        [Range(0f, 1f)] public float spatialBlend = 0.6f;

        private AudioSource[] voices;
        private int nextVoice;
        private AudioSource bgmSource;
        private static bool warnedMissing;
        private static bool warnedNoLibrary;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);      // 只销毁组件，别把 GameManager 整个删了
                return;
            }
            Instance = this;

            // 声部池：一次建好复用。每个声部一个子物体，
            // 这样 PlayAt 移动的是子物体，不会把 GameManager 本体的位置改掉（踩过这种坑）
            voices = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < voices.Length; i++)
            {
                GameObject go = new GameObject($"Voice{i}");
                go.transform.SetParent(transform, false);
                AudioSource src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                voices[i] = src;
            }

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (playBgmOnStart) PlayBgm();
        }

        // ==================== 静态入口 ====================

        /// <summary>播 2D 音效（UI、跳跃等不需要方位的）</summary>
        public static void Play(SfxId id)
        {
            if (!EnsureInstance()) return;
            Instance.PlayInternal(id, Vector3.zero, false);
        }

        /// <summary>在世界坐标播音效（有距离衰减）</summary>
        public static void PlayAt(SfxId id, Vector3 worldPos)
        {
            if (!EnsureInstance()) return;
            Instance.PlayInternal(id, worldPos, true);
        }

        public static void PlayBgm()
        {
            if (!EnsureInstance()) return;
            Instance.PlayBgmInternal();
        }

        public static void StopBgm()
        {
            if (Instance == null || Instance.bgmSource == null) return;
            Instance.bgmSource.Stop();
        }

        /// <summary>实例不存在时只警告一次，避免每帧刷屏</summary>
        private static bool EnsureInstance()
        {
            if (Instance != null) return true;
            if (!warnedMissing)
            {
                warnedMissing = true;
                Debug.LogWarning("[AudioManager] 场景里没有 AudioManager → 音效不会播放。" +
                                 "请把它挂到 GameManager 物体上");
            }
            return false;
        }

        // ==================== 实现 ====================

        private void PlayInternal(SfxId id, Vector3 pos, bool spatial)
        {
            if (library == null)
            {
                if (!warnedNoLibrary)
                {
                    warnedNoLibrary = true;
                    Debug.LogWarning("[AudioManager] 没有拖 SoundLibrary 资产 → 音效不会播放");
                }
                return;
            }

            SoundLibrary.Entry entry = library.Find(id);
            if (entry == null) return;      // 这个音效还没配素材：静默跳过，不报错

            AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;

            AudioSource src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;

            src.Stop();
            src.clip = clip;
            src.volume = Mathf.Clamp01(entry.volume * masterVolume);
            src.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);

            if (spatial)
            {
                src.transform.position = pos;
                src.spatialBlend = spatialBlend;
            }
            else
            {
                src.spatialBlend = 0f;
            }

            src.Play();
        }

        private void PlayBgmInternal()
        {
            if (library == null || library.bgm == null || bgmSource == null) return;
            if (bgmSource.isPlaying && bgmSource.clip == library.bgm) return;   // 已经在播同一首

            bgmSource.clip = library.bgm;
            bgmSource.volume = Mathf.Clamp01(library.bgmVolume * masterVolume);
            bgmSource.Play();
        }
    }
}
