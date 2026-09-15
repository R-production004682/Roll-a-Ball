using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 全シーンから共有する所持金、ステージ進行、タイム記録、購入状況を管理する
    /// </summary>
    public static class GameDataManager
    {
        /// <summary>
        /// 初回起動時に解放する最初のステージ ID
        /// </summary>
        public const string InitialStageId = "stage-1";

        private static bool initialized;
        private static bool saveAllowed = true;
        private static bool hasLoadFailure;
        private static bool hasLoggedPersistenceFailure;
        private static GameSaveData saveData;

        /// <summary>
        /// ゲームデータが保存されたときに通知する
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// 保存データを読み込めなかったため初期値で稼働しているか
        /// </summary>
        public static bool HasLoadFailure
        {
            get
            {
                EnsureInitialized();
                return hasLoadFailure;
            }
        }

        /// <summary>
        /// 現在の所持金
        /// </summary>
        public static int Currency
        {
            get
            {
                EnsureInitialized();
                return saveData.currency;
            }
        }

        /// <summary>
        /// プレイ開始時にキャッシュと購読を初期化する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            initialized = false;
            saveAllowed = true;
            hasLoadFailure = false;
            hasLoggedPersistenceFailure = false;
            saveData = null;
            Changed = null;
        }

        /// <summary>
        /// シーン読込前に保存データを初期化する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 所持金を加算して保存する
        /// </summary>
        /// <param name="amount">加算する正の金額</param>
        /// <param name="currencyAfterChange">処理後の所持金。失敗時は変更前の値</param>
        /// <returns>加算して保存できた場合は true</returns>
        public static bool TryAddCurrency(int amount, out int currencyAfterChange)
        {
            EnsureInitialized();
            currencyAfterChange = saveData.currency;

            if (amount <= 0 || amount > int.MaxValue - saveData.currency)
            {
                return false;
            }

            var candidate = GameDataAlgorithm.Clone(saveData);
            candidate.currency += amount;
            if (!TryCommit(candidate))
            {
                return false;
            }

            currencyAfterChange = candidate.currency;
            return true;
        }

        /// <summary>
        /// 所持金が足りる場合だけ減算して保存する
        /// </summary>
        /// <param name="amount">減算する正の金額</param>
        /// <param name="currencyAfterChange">処理後の所持金。失敗時は変更前の値</param>
        /// <returns>減算して保存できた場合は true</returns>
        public static bool TrySpendCurrency(int amount, out int currencyAfterChange)
        {
            EnsureInitialized();
            currencyAfterChange = saveData.currency;

            if (amount <= 0 || amount > saveData.currency)
            {
                return false;
            }

            var candidate = GameDataAlgorithm.Clone(saveData);
            candidate.currency -= amount;
            if (!TryCommit(candidate))
            {
                return false;
            }

            currencyAfterChange = candidate.currency;
            return true;
        }

        /// <summary>
        /// ステージが解放済みかを返す
        /// </summary>
        /// <param name="stageId">ステージを識別する安定 ID</param>
        /// <returns>ステージが解放済みの場合は true</returns>
        public static bool IsStageUnlocked(string stageId)
        {
            EnsureInitialized();
            var stage = FindStage(stageId);
            return stage != null && stage.isUnlocked;
        }

        /// <summary>
        /// ステージがクリア済みかを返す
        /// </summary>
        /// <param name="stageId">ステージを識別する安定 ID</param>
        /// <returns>ステージがクリア済みの場合は true</returns>
        public static bool IsStageCleared(string stageId)
        {
            EnsureInitialized();
            var stage = FindStage(stageId);
            return stage != null && stage.isCleared;
        }

        /// <summary>
        /// 指定したステージを解放して保存する
        /// </summary>
        /// <param name="stageId">解放するステージの安定 ID</param>
        /// <returns>解放済み、または新たに保存できた場合は true</returns>
        public static bool UnlockStage(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            var existingStage = FindStage(stageId);
            if (existingStage != null && existingStage.isUnlocked)
            {
                return true;
            }

            var candidate = GameDataAlgorithm.Clone(saveData);
            var candidateStage = FindStage(candidate, stageId);
            if (candidateStage == null)
            {
                candidateStage = CreateStage(stageId);
                candidate.stages.Add(candidateStage);
            }

            candidateStage.isUnlocked = true;
            return TryCommit(candidate);
        }

        /// <summary>
        /// ステージのクリア状態と上位5件のタイムを保存する
        /// </summary>
        /// <param name="stageId">クリアした解放済みステージの安定 ID</param>
        /// <param name="clearTimeSeconds">クリアまでにかかった有限の秒数</param>
        /// <param name="rank">上位5件に入った場合は1始まりの順位、それ以外は0</param>
        /// <returns>クリア状態を保存できた場合は true</returns>
        public static bool RecordStageClear(string stageId, float clearTimeSeconds, out int rank)
        {
            EnsureInitialized();
            rank = 0;

            if (string.IsNullOrWhiteSpace(stageId) || float.IsNaN(clearTimeSeconds) ||
                float.IsInfinity(clearTimeSeconds) || clearTimeSeconds < 0f)
            {
                return false;
            }

            var currentStage = FindStage(stageId);
            if (currentStage == null || !currentStage.isUnlocked)
            {
                return false;
            }

            var candidate = GameDataAlgorithm.Clone(saveData);
            var candidateStage = FindStage(candidate, stageId);
            candidateStage.isCleared = true;
            var candidateRank = GameDataAlgorithm.InsertClearTime(candidateStage.clearTimes, clearTimeSeconds);
            if (candidateRank == 0 && currentStage.isCleared)
            {
                return true;
            }

            if (!TryCommit(candidate))
            {
                return false;
            }

            rank = candidateRank;
            return true;
        }

        /// <summary>
        /// 指定ステージのクリアタイム上位5件を秒単位で返す
        /// </summary>
        /// <param name="stageId">ステージを識別する安定 ID</param>
        /// <returns>昇順のタイム配列。未登録なら空配列</returns>
        public static float[] GetTopClearTimes(string stageId)
        {
            EnsureInitialized();
            var stage = FindStage(stageId);
            if (stage == null)
            {
                return Array.Empty<float>();
            }

            var clearTimes = new float[stage.clearTimes.Count];
            for (var index = 0; index < stage.clearTimes.Count; index++)
            {
                clearTimes[index] = stage.clearTimes[index].seconds;
            }

            return clearTimes;
        }

        /// <summary>
        /// 商品を購入済みとして保存し、価格を所持金から一度だけ差し引く
        /// </summary>
        /// <param name="itemId">商品を識別する安定 ID</param>
        /// <param name="price">購入に必要な0以上の金額</param>
        /// <param name="currencyAfterPurchase">購入後の所持金。失敗時は変更前の値</param>
        /// <returns>購入状態と所持金を保存できた場合は true</returns>
        public static bool TryPurchaseItem(string itemId, int price, out int currencyAfterPurchase)
        {
            EnsureInitialized();
            currencyAfterPurchase = saveData.currency;

            if (string.IsNullOrWhiteSpace(itemId) || price < 0 ||
                price > saveData.currency || IsItemPurchased(itemId))
            {
                return false;
            }

            var candidate = GameDataAlgorithm.Clone(saveData);
            candidate.currency -= price;
            candidate.purchasedItemIds.Add(itemId);
            if (!TryCommit(candidate))
            {
                return false;
            }

            currencyAfterPurchase = candidate.currency;
            return true;
        }

        /// <summary>
        /// 商品を購入済みかを返す
        /// </summary>
        /// <param name="itemId">商品を識別する安定 ID</param>
        /// <returns>購入済みの場合は true</returns>
        public static bool IsItemPurchased(string itemId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(itemId) && saveData.purchasedItemIds.Contains(itemId);
        }

        /// <summary>
        /// 購入済みの商品 ID をコピーして返す
        /// </summary>
        /// <returns>購入済み商品 ID の配列</returns>
        public static string[] GetPurchasedItemIds()
        {
            EnsureInitialized();
            return saveData.purchasedItemIds.ToArray();
        }

        /// <summary>
        /// 現在のゲームデータを JSON ファイルへ保存する
        /// </summary>
        /// <returns>保存に成功した場合は true</returns>
        public static bool Save()
        {
            EnsureInitialized();
            if (!saveAllowed)
            {
                return false;
            }

            return TryPersist(saveData);
        }

        /// <summary>
        /// 初期所持金と最初のステージ解放状態に戻して保存する
        /// </summary>
        /// <returns>初期状態の保存に成功した場合は true</returns>
        public static bool ResetToDefaults()
        {
            EnsureInitialized();
            var defaults = GameDataAlgorithm.CreateDefault(InitialStageId);
            if (!TryPersist(defaults))
            {
                return false;
            }

            saveData = defaults;
            saveAllowed = true;
            hasLoadFailure = false;
            hasLoggedPersistenceFailure = false;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 保存ファイルを読み込み、初回データを作成する
        /// </summary>
        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            var loadStatus = GameDataStorage.Load(out var loadedData, out var loadError);
            if (loadStatus == GameDataLoadStatus.Missing)
            {
                saveData = GameDataAlgorithm.CreateDefault(InitialStageId);
                saveAllowed = true;
                TryPersist(saveData);
                return;
            }

            if (loadStatus == GameDataLoadStatus.Loaded &&
                GameDataAlgorithm.TryNormalize(
                    loadedData,
                    InitialStageId,
                    out var normalizedData,
                    out loadError))
            {
                saveData = normalizedData;
                saveAllowed = true;
                return;
            }

            saveData = GameDataAlgorithm.CreateDefault(InitialStageId);
            saveAllowed = false;
            hasLoadFailure = true;
            Debug.LogError($"保存データを読み込めないため初期値を使用します。既存ファイルは上書きしません: {GameDataStorage.SavePath}{Environment.NewLine}{loadError}");
        }

        /// <summary>
        /// 変更候補を保存してからランタイムの共有データへ反映する
        /// </summary>
        /// <param name="candidate">変更候補</param>
        /// <returns>保存後の反映に成功した場合は true</returns>
        private static bool TryCommit(GameSaveData candidate)
        {
            if (!saveAllowed || !TryPersist(candidate))
            {
                return false;
            }

            saveData = candidate;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 購読者ごとに変更を通知し、一つの通知先の失敗で後続を止めない
        /// </summary>
        private static void NotifyChanged()
        {
            if (Changed == null)
            {
                return;
            }

            foreach (var callback in Changed.GetInvocationList())
            {
                try
                {
                    ((Action)callback)();
                }
                catch (Exception exception)
                {
                    // 保存済みデータの変更を維持し、問題のある購読者を特定する。
                    Debug.LogError($"GameDataManager.Changed の通知先で例外が発生しました。{Environment.NewLine}{exception}");
                }
            }
        }

        /// <summary>
        /// 保存失敗を診断ログに残し、ストレージ層へ書き込みを依頼する
        /// </summary>
        /// <param name="candidate">保存するデータ</param>
        /// <returns>ファイルへの保存に成功した場合は true</returns>
        private static bool TryPersist(GameSaveData candidate)
        {
            if (GameDataStorage.TrySave(candidate, out var error))
            {
                hasLoggedPersistenceFailure = false;
                return true;
            }

            if (!hasLoggedPersistenceFailure)
            {
                Debug.LogError($"ゲームデータを保存できませんでした: {GameDataStorage.SavePath}{Environment.NewLine}{error}");
                hasLoggedPersistenceFailure = true;
            }

            return false;
        }

        /// <summary>
        /// ステージ ID に一致する進行データを検索する
        /// </summary>
        /// <param name="stageId">検索する安定 ID</param>
        /// <returns>一致した進行データ。未登録なら null</returns>
        private static StageProgressData FindStage(string stageId)
        {
            return string.IsNullOrWhiteSpace(stageId) ? null : FindStage(saveData, stageId);
        }

        /// <summary>
        /// 指定データ内からステージ ID に一致する進行データを検索する
        /// </summary>
        /// <param name="data">検索対象のデータ</param>
        /// <param name="stageId">検索する安定 ID</param>
        /// <returns>一致した進行データ。未登録なら null</returns>
        private static StageProgressData FindStage(GameSaveData data, string stageId)
        {
            foreach (var stage in data.stages)
            {
                if (string.Equals(stage.stageId, stageId, StringComparison.Ordinal))
                {
                    return stage;
                }
            }

            return null;
        }

        /// <summary>
        /// 新しいステージ進行データを初期化する
        /// </summary>
        /// <param name="stageId">ステージを識別する安定 ID</param>
        /// <returns>未解放・未クリア状態の進行データ</returns>
        private static StageProgressData CreateStage(string stageId)
        {
            return new StageProgressData
            {
                stageId = stageId,
                clearTimes = new List<ClearTimeRecord>()
            };
        }
    }
}
