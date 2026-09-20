#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Globalization;
using Roll_a_Ball.OutGame;
using UnityEngine;

namespace Roll_a_Ball.DebugTools
{
    /// <summary>
    /// 開発用のコイン付与をゲームデータへ反映する
    /// </summary>
    internal static class DebugCurrencyGrantService
    {
        /// <summary>
        /// 入力された正の整数分だけ所持金を加算して保存する
        /// </summary>
        /// <param name="amountText">画面で入力された付与額</param>
        /// <param name="message">画面へ表示する実行結果</param>
        /// <returns>所持金を加算して保存できた場合は true</returns>
        internal static bool TryGrant(string amountText, out string message)
        {
            if (string.IsNullOrWhiteSpace(amountText))
            {
                message = "コイン付与に失敗しました。付与額を入力してください。";
                Debug.LogWarning(message);
                return false;
            }

            if (!int.TryParse(
                    amountText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var amount) || amount <= 0)
            {
                message = $"コイン付与に失敗しました。付与額は1以上の整数にしてください: {amountText}";
                Debug.LogWarning(message);
                return false;
            }

            var currencyBefore = GameDataManager.Currency;
            if (amount > int.MaxValue - currencyBefore)
            {
                message = $"コイン付与に失敗しました。所持金の上限を超えるため付与できません: {amount:N0}";
                Debug.LogWarning(message);
                return false;
            }

            if (!GameDataManager.TryAddCurrency(amount, out var currencyAfter))
            {
                message =
                    $"コイン付与に失敗しました。保存できませんでした。入力額: {amount:N0}、" +
                    $"保存先: {GameDataStorage.SavePath}";
                Debug.LogError(message);
                return false;
            }

            message =
                $"コインを付与しました。付与前: {currencyBefore:N0}、" +
                $"付与額: {amount:N0}、付与後: {currencyAfter:N0}";
            Debug.Log(message);
            return true;
        }
    }
}
#endif
