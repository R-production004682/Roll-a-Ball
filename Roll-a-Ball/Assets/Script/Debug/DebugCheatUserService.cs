#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Roll_a_Ball.OutGame;
using UnityEngine;

namespace Roll_a_Ball.DebugTools
{
    /// <summary>
    /// 開発用のチートユーザープリセットをゲームデータへ適用する
    /// </summary>
    internal static class DebugCheatUserService
    {
        private const int PresetCurrency = 100000;
        private const string ShopItemCatalogResourcePath = "ShopItemCatalog";
        private const string StageTwoId = "stage-2";
        private const string StageThreeId = "stage-3";
        private const string StageFourId = "stage-4";

        /// <summary>
        /// 所持金、ステージ解放、購入済み商品を開発用プリセットへ置き換える
        /// </summary>
        /// <param name="message">画面へ表示する実行結果</param>
        /// <returns>プリセットを最後まで保存できた場合は true</returns>
        internal static bool TryCreate(out string message)
        {
            message = string.Empty;
            if (!GameDataManager.ResetToDefaults())
            {
                message = "チートユーザー作成に失敗しました。初期データを保存できません。";
                UnityEngine.Debug.LogError(message);
                return false;
            }

            if (!GameDataManager.TryAddCurrency(PresetCurrency, out var currencyAfterChange))
            {
                message = "チートユーザー作成に失敗しました。所持金を保存できません。";
                UnityEngine.Debug.LogError(message);
                return false;
            }

            if (!UnlockStage(StageTwoId) || !UnlockStage(StageThreeId) || !UnlockStage(StageFourId))
            {
                message = "チートユーザー作成に失敗しました。ステージ解放状態を保存できません。";
                UnityEngine.Debug.LogError(message);
                return false;
            }

            var itemCatalog = Resources.Load<ShopItemCatalog>(ShopItemCatalogResourcePath);
            if (itemCatalog == null || itemCatalog.Items == null)
            {
                message = "チートユーザー作成に失敗しました。\n" +
                          "ショップ商品マスターを読み込めないまたは、\n" +
                          "すでに全商品が登録されています。";
                UnityEngine.Debug.LogError(message);
                return false;
            }

            var catalogItemIds = new HashSet<string>();
            var purchasedItemCount = 0;
            for (var index = 0; index < itemCatalog.Items.Count; index++)
            {
                var item = itemCatalog.Items[index];
                if (item == null || string.IsNullOrWhiteSpace(item.Id) || !catalogItemIds.Add(item.Id))
                {
                    message = "チートユーザー作成に失敗しました。ショップ商品マスターの商品 ID が不正です。";
                    UnityEngine.Debug.LogError(message);
                    return false;
                }

                for (var purchaseIndex = 0; purchaseIndex < item.PurchaseLimit; purchaseIndex++)
                {
                    if (!GameDataManager.TryPurchaseItem(
                            item.Id,
                            0,
                            item.PurchaseLimit,
                            out currencyAfterChange))
                    {
                        message = $"チートユーザー作成に失敗しました。商品を取得できません: {item.Id}";
                        UnityEngine.Debug.LogError(message);
                        return false;
                    }

                    purchasedItemCount++;
                }
            }

            message =
                $"チートユーザーを作成しました。 \n" +
                $"所持金: {currencyAfterChange:N0}、解放: stage-1～stage-4、取得商品数: {purchasedItemCount}";
            UnityEngine.Debug.Log(message);
            return true;
        }

        /// <summary>
        /// 指定したステージを解放し、保存に失敗した場合は原因をログへ残す
        /// </summary>
        /// <param name="stageId">解放するステージ ID</param>
        /// <returns>解放状態を保存できた場合は true</returns>
        private static bool UnlockStage(string stageId)
        {
            if (GameDataManager.UnlockStage(stageId))
            {
                return true;
            }

            UnityEngine.Debug.LogError($"チートユーザー作成でステージを解放できませんでした: {stageId}");
            return false;
        }
    }
}
#endif
