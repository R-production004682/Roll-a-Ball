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

        private void OnEnable() => GameSettings.Changed += ApplyVolume;
        private void OnDisable() => GameSettings.Changed -= ApplyVolume;
        private void ApplyVolume()
        {
            if (source != null) source.volume = GameSettings.SeVolume;
        }

        private static UiSoundPlayer Instance
        {
            get
            {
                if (instance != null) return instance;
                var playerObject = new GameObject("UI Sound Player");
                DontDestroyOnLoad(playerObject);
                instance = playerObject.AddComponent<UiSoundPlayer>();
                return instance;
            }
        }

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

        public static void Play(AudioClip clip)
        {
            if (clip == null) return;
            Instance.source.volume = GameSettings.SeVolume;
            Instance.source.PlayOneShot(clip);
        }
    }
}
