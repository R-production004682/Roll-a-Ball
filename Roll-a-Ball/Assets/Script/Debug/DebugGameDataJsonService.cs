#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Roll_a_Ball.DebugTools
{
    /// <summary>
    /// PlayerPrefs に保存されたゲームデータ JSON を開発時に扱う
    /// </summary>
    internal static class DebugGameDataJsonService
    {
        private const string SaveDataKey = "RollABall.GameData.SaveData";

        /// <summary>
        /// 保存済みゲームデータ JSON をクリップボードへコピーする
        /// </summary>
        /// <param name="message">画面へ表示する実行結果</param>
        /// <returns>JSON をコピーできた場合は true</returns>
        internal static bool TryCopyToClipboard(out string message)
        {
            message = string.Empty;
            if (!PlayerPrefs.HasKey(SaveDataKey))
            {
                message = "保存済みのゲームデータ JSON がありません。";
                Debug.LogWarning(message);
                return false;
            }

            var json = PlayerPrefs.GetString(SaveDataKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                message = "保存済みのゲームデータ JSON が空です。";
                Debug.LogWarning(message);
                return false;
            }

            GUIUtility.systemCopyBuffer = json;
            message = $"ゲームデータ JSON をコピーしました（{json.Length:N0}文字）";
            Debug.Log(message);
            return true;
        }
    }
}
#endif
