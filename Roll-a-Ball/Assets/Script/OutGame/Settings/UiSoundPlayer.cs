using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Scene 遷移やポーズをまたいで UI 効果音を最後まで再生する
    /// </summary>
    internal sealed class UiSoundPlayer : MonoBehaviour
    {
        private static UiSoundPlayer instance;
        private AudioSource source;

        /// <summary>
        /// 音量変更の購読を開始する
        /// </summary>
        private void OnEnable()
        {
            GameSettings.Changed += ApplyVolume;
        }

        /// <summary>
        /// 音量変更の購読を解除する
        /// </summary>
        private void OnDisable()
        {
            GameSettings.Changed -= ApplyVolume;
        }

        /// <summary>
        /// 共通 SE 音量を AudioSource へ反映する
        /// </summary>
        private void ApplyVolume()
        {
            if (source != null)
            {
                source.volume = GameSettings.SeVolume;
            }
        }

        /// <summary>
        /// シーンをまたいで共有する UI 効果音プレイヤーを取得する
        /// </summary>
        private static UiSoundPlayer Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var playerObject = new GameObject("UI Sound Player");
                DontDestroyOnLoad(playerObject);
                instance = playerObject.AddComponent<UiSoundPlayer>();
                return instance;
            }
        }

        /// <summary>
        /// UI 効果音用の AudioSource を初期化する
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }

        /// <summary>
        /// 指定された UI 効果音を現在の SE 音量で再生する
        /// </summary>
        /// <param name="clip">再生する効果音</param>
        public static void Play(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            Instance.source.volume = GameSettings.SeVolume;
            Instance.source.PlayOneShot(clip);
        }
    }
}
