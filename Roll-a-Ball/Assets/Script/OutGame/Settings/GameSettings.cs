using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>全シーンで共有する音量設定。値は 0～1、保存先は PlayerPrefs。</summary>
    public static class GameSettings
    {
        public const float DefaultBgmVolume = 0.7f;
        public const float DefaultSeVolume = 0.8f;
        private const string BgmKey = "RollABall.Settings.BgmVolume";
        private const string SeKey = "RollABall.Settings.SeVolume";
        private static bool loaded;
        private static float bgmVolume;
        private static float seVolume;

        public static event Action Changed;
        public static float BgmVolume { get { EnsureLoaded(); return bgmVolume; } }
        public static float SeVolume { get { EnsureLoaded(); return seVolume; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            loaded = false;
            Changed = null;
            AudioListener.pause = false;
        }

        private static float Sanitize(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            bgmVolume = Sanitize(PlayerPrefs.GetFloat(BgmKey, DefaultBgmVolume), DefaultBgmVolume);
            seVolume = Sanitize(PlayerPrefs.GetFloat(SeKey, DefaultSeVolume), DefaultSeVolume);
            loaded = true;
        }

        public static void SetBgmVolume(float value)
        {
            EnsureLoaded();
            bgmVolume = Sanitize(value, DefaultBgmVolume);
            Changed?.Invoke();
        }

        public static void SetSeVolume(float value)
        {
            EnsureLoaded();
            seVolume = Sanitize(value, DefaultSeVolume);
            Changed?.Invoke();
        }

        public static void RestoreDefaults()
        {
            loaded = true;
            bgmVolume = DefaultBgmVolume;
            seVolume = DefaultSeVolume;
            Changed?.Invoke();
            Save();
        }

        public static void Save()
        {
            EnsureLoaded();
            PlayerPrefs.SetFloat(BgmKey, bgmVolume);
            PlayerPrefs.SetFloat(SeKey, seVolume);
            PlayerPrefs.Save();
        }
    }
}
