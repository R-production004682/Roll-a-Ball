using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>AudioSource に追加し、BGM / SE の共有音量を適用する。</summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class AudioVolumeBinding : MonoBehaviour
    {
        public enum Channel { Bgm, Se }

        [SerializeField] private Channel channel;
        [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
        private AudioSource source;

        private void Awake() => source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            if (source == null) source = GetComponent<AudioSource>();
            GameSettings.Changed += ApplyVolume;
            ApplyVolume();
        }

        private void OnDisable() => GameSettings.Changed -= ApplyVolume;

        private void ApplyVolume()
        {
            source.volume = baseVolume * (channel == Channel.Bgm ? GameSettings.BgmVolume : GameSettings.SeVolume);
        }
    }
}
