using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// AudioSource に追加し、BGM / SE の共有音量を適用する
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
            Se
        }

        [SerializeField] private Channel channel;
        [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
        private AudioSource source;

        /// <summary>
        /// 同じ GameObject の AudioSource を取得する
        /// </summary>
        private void Awake()
        {
            source = GetComponent<AudioSource>();
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
            source.volume = baseVolume * (channel == Channel.Bgm ? GameSettings.BgmVolume : GameSettings.SeVolume);
        }
    }
}
