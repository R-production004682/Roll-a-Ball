using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// AudioSource に追加し、BGM / SE / 環境音の共有音量を適用する
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class AudioVolumeBinding : MonoBehaviour
    {
        /// <summary>
        /// 音量を適用する音声種別
        /// </summary>
        public enum Channel
        {
            Bgm,
            Se,
            Es
        }

        [SerializeField, Tooltip("この音源に適用する共通音量を選びます（BGM・効果音・環境音）。")]
        private Channel channel;
        [SerializeField, Range(0f, 1f), Tooltip("この音源固有の音量です。共通音量を掛けて再生します。")]
        private float baseVolume = 1f;
        private AudioSource source;

        /// <summary>
        /// 同じ GameObject の AudioSource を取得する
        /// </summary>
        private void Awake()
        {
            source = GetComponent<AudioSource>();
            // このプロパティは Prefab に保存されないため、実行時に設定する。
            // ポーズ中も環境音を聴きながら音量を調整できるようにする。
            if (channel == Channel.Es)
            {
                source.ignoreListenerPause = true;
            }
        }

        /// <summary>
        /// 音量変更の購読を開始し、現在の音量を反映する
        /// </summary>
        private void OnEnable()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            GameSettings.Changed += ApplyVolume;
            ApplyVolume();
        }

        /// <summary>
        /// 音量変更の購読を解除する
        /// </summary>
        private void OnDisable()
        {
            GameSettings.Changed -= ApplyVolume;
        }

        /// <summary>
        /// 共通設定の音量を AudioSource へ反映する
        /// </summary>
        private void ApplyVolume()
        {
            float volume;
            switch (channel)
            {
                case Channel.Bgm:
                    {
                        volume = GameSettings.BgmVolume;
                        break;
                    }
                case Channel.Es:
                    {
                        volume = GameSettings.AmbientVolume;
                        break;
                    }
                default:
                    {
                        volume = GameSettings.SeVolume;
                        break;
                    }
            }

            source.volume = baseVolume * volume;
        }
    }
}
