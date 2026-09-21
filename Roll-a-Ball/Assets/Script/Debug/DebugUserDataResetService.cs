#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Roll_a_Ball.OutGame;
using UnityEngine;

namespace Roll_a_Ball.DebugTools
{
    /// <summary>
    /// 開発用のユーザーデータ初期化を実行する
    /// </summary>
    internal static class DebugUserDataResetService
    {
        /// <summary>
        /// GameDataManagerの保存データを初期状態へ戻す
        /// </summary>
        /// <param name="message">画面へ表示する実行結果</param>
        /// <returns>初期状態の保存に成功した場合は true</returns>
        internal static bool TryReset(out string message)
        {
            if (GameDataManager.ResetToDefaults())
            {
                message = "ユーザーデータを削除し、初期状態へ戻しました。";
                Debug.Log(message);
                return true;
            }

            message = $"ユーザーデータ削除に失敗しました。保存先: {GameDataStorage.SavePath}";
            Debug.LogError(message);
            return false;
        }
    }
}
#endif
