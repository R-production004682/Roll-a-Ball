using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// SettingsDialogController の Inspector 参照を検証する処理をまとめる
    /// </summary>
    internal static class SettingsDialogAlgorithm
    {
        /// <summary>
        /// 未設定の Inspector 参照名を取得する
        /// </summary>
        /// <param name="dialog">設定ダイアログの表示オブジェクト</param>
        /// <param name="dialogInputScope">設定ダイアログの入力範囲</param>
        /// <param name="bgmSlider">BGM 音量スライダー</param>
        /// <param name="seSlider">SE 音量スライダー</param>
        /// <param name="bgmValue">BGM 音量表示</param>
        /// <param name="seValue">SE 音量表示</param>
        /// <param name="backLabel">戻るボタンのラベル</param>
        /// <param name="contextLabel">設定画面の説明ラベル</param>
        /// <param name="previewSource">SE 試聴用 AudioSource</param>
        /// <returns>未設定の参照名一覧</returns>
        public static IReadOnlyList<string> GetMissingReferences(
            GameObject dialog,
            UiInputScope dialogInputScope,
            Slider bgmSlider,
            Slider seSlider,
            TMP_Text bgmValue,
            TMP_Text seValue,
            TMP_Text backLabel,
            TMP_Text contextLabel,
            AudioSource previewSource)
        {
            var missingReferences = new List<string>();
            AddMissingReference(dialog, nameof(dialog), missingReferences);
            AddMissingReference(dialogInputScope, nameof(dialogInputScope), missingReferences);
            AddMissingReference(bgmSlider, nameof(bgmSlider), missingReferences);
            AddMissingReference(seSlider, nameof(seSlider), missingReferences);
            AddMissingReference(bgmValue, nameof(bgmValue), missingReferences);
            AddMissingReference(seValue, nameof(seValue), missingReferences);
            AddMissingReference(backLabel, nameof(backLabel), missingReferences);
            AddMissingReference(contextLabel, nameof(contextLabel), missingReferences);
            AddMissingReference(previewSource, nameof(previewSource), missingReferences);
            return missingReferences;
        }

        /// <summary>
        /// 未設定の参照名だけを結果一覧へ追加する
        /// </summary>
        /// <param name="reference">検証する Unity オブジェクト</param>
        /// <param name="referenceName">Inspector 上の参照名</param>
        /// <param name="missingReferences">未設定参照を格納する一覧</param>
        private static void AddMissingReference(
            Object reference,
            string referenceName,
            ICollection<string> missingReferences)
        {
            if (reference == null)
            {
                missingReferences.Add(referenceName);
            }
        }
    }
}
