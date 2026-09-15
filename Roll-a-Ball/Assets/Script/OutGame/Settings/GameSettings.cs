using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 全シーンで共有する音量を管理し、値は 0～1、保存先は PlayerPrefs
    /// </summary>
    public static class GameSettings
    {
        public const float DefaultBgmVolume = 0.7f;
        public const float DefaultSeVolume = 0.8f;
        private const string BgmKey = "RollABall.Settings.BgmVolume";
        private const string SeKey = "RollABall.Settings.SeVolume";
        private static bool loaded;
        private static float bgmVolume;
        private static float seVolume;

        /// <summary>
        /// 設定値が変更されたときに通知する
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// 現在の BGM 音量を返す
        /// </summary>
        public static float BgmVolume
        {
            get
            {
                EnsureLoaded();
                return bgmVolume;
            }
        }

        /// <summary>
        /// 現在の SE 音量を返す
        /// </summary>
        public static float SeVolume
        {
            get
            {
                EnsureLoaded();
                return seVolume;
            }
        }

        /// <summary>
        /// プレイ開始時に設定のキャッシュと購読を初期化する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            loaded = false;
            Changed = null;
            AudioListener.pause = false;
        }

        /// <summary>
        /// 音量を有限な 0 から 1 の範囲へ正規化する
        /// </summary>
        /// <param name="value">検証する音量</param>
        /// <param name="fallback">有限値でない場合に使用する音量</param>
        /// <returns>利用可能な音量</returns>
        private static float Sanitize(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
        }

        /// <summary>
        /// PlayerPrefs から音量設定を一度だけ読み込む
        /// </summary>
        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            bgmVolume = Sanitize(PlayerPrefs.GetFloat(BgmKey, DefaultBgmVolume), DefaultBgmVolume);
            seVolume = Sanitize(PlayerPrefs.GetFloat(SeKey, DefaultSeVolume), DefaultSeVolume);
            loaded = true;
        }

        /// <summary>
        /// BGM 音量を更新して購読者へ通知する
        /// </summary>
        /// <param name="value">設定する音量</param>
        public static void SetBgmVolume(float value)
        {
            EnsureLoaded();
            var sanitized = Sanitize(value, DefaultBgmVolume);
            if (Mathf.Approximately(bgmVolume, sanitized))
            {
                return;
            }

            bgmVolume = sanitized;
            Changed?.Invoke();
        }

        /// <summary>
        /// SE 音量を更新して購読者へ通知する
        /// </summary>
        /// <param name="value">設定する音量</param>
        public static void SetSeVolume(float value)
        {
            EnsureLoaded();
            var sanitized = Sanitize(value, DefaultSeVolume);
            if (Mathf.Approximately(seVolume, sanitized))
            {
                return;
            }

            seVolume = sanitized;
            Changed?.Invoke();
        }

        /// <summary>
        /// 音量を初期値へ戻して保存する
        /// </summary>
        public static void RestoreDefaults()
        {
            loaded = true;
            bgmVolume = DefaultBgmVolume;
            seVolume = DefaultSeVolume;
            Changed?.Invoke();
            Save();
        }

        /// <summary>
        /// 現在の音量を PlayerPrefs へ保存する
        /// </summary>
        public static void Save()
        {
            EnsureLoaded();
            PlayerPrefs.SetFloat(BgmKey, bgmVolume);
            PlayerPrefs.SetFloat(SeKey, seVolume);
            PlayerPrefs.Save();
        }
    }
}
