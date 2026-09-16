using System.Collections.Generic;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ゲームデータの初期化、互換性検証、記録順位の計算を行う
    /// </summary>
    internal static class GameDataAlgorithm
    {
        /// <summary>
        /// 初回起動用の所持金と最初に解放するステージを作成する
        /// </summary>
        /// <param name="initialStageId">初回に解放するステージの安定 ID</param>
        /// <returns>初期値を持つゲームデータ</returns>
        internal static GameSaveData CreateDefault(string initialStageId)
        {
            return new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                currency = 0,
                stages = new List<StageProgressData>
                {
                    new StageProgressData
                    {
                        stageId = initialStageId,
                        isUnlocked = true
                    }
                },
                purchasedItemIds = new List<string>()
            };
        }

        /// <summary>
        /// 読み込んだデータの版と整合性を確認し、利用可能なコピーを作成する
        /// </summary>
        /// <param name="source">保存ファイルから復元したデータ</param>
        /// <param name="initialStageId">初回起動時に解放するステージの安定 ID</param>
        /// <param name="normalized">検証・正規化したデータ</param>
        /// <param name="error">利用できない場合の理由</param>
        /// <returns>現在の版として安全に利用できる場合は true</returns>
        internal static bool TryNormalize(
            GameSaveData source,
            string initialStageId,
            out GameSaveData normalized,
            out string error)
        {
            normalized = null;
            error = string.Empty;

            if (source == null)
            {
                error = "保存データがありません。";
                return false;
            }

            if (source.version != GameSaveData.CurrentVersion)
            {
                error = $"未対応の保存データ版です: {source.version}";
                return false;
            }

            if (source.currency < 0)
            {
                error = "所持金が負の値です。";
                return false;
            }

            var sourceStages = source.stages ?? new List<StageProgressData>();
            var sourceItems = source.purchasedItemIds ?? new List<string>();
            var stageIds = new HashSet<string>();
            var itemIds = new HashSet<string>();
            var stages = new List<StageProgressData>(sourceStages.Count);
            var purchasedItemIds = new List<string>(sourceItems.Count);

            foreach (var sourceStage in sourceStages)
            {
                if (sourceStage == null || string.IsNullOrWhiteSpace(sourceStage.stageId))
                {
                    error = "ステージ記録に空の ID が含まれています。";
                    return false;
                }

                if (!stageIds.Add(sourceStage.stageId))
                {
                    error = $"ステージ記録の ID が重複しています: {sourceStage.stageId}";
                    return false;
                }

                if (!TryNormalizeClearTimes(sourceStage.clearTimes, out var clearTimes))
                {
                    error = $"クリアタイムが不正です: {sourceStage.stageId}";
                    return false;
                }

                stages.Add(new StageProgressData
                {
                    stageId = sourceStage.stageId,
                    isUnlocked = sourceStage.isUnlocked || sourceStage.isCleared ||
                        string.Equals(sourceStage.stageId, initialStageId, System.StringComparison.Ordinal),
                    isCleared = sourceStage.isCleared,
                    clearTimes = clearTimes
                });
            }

            foreach (var itemId in sourceItems)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    error = "購入済み商品に空の ID が含まれています。";
                    return false;
                }

                if (itemIds.Add(itemId))
                {
                    purchasedItemIds.Add(itemId);
                }
            }

            if (!stageIds.Contains(initialStageId))
            {
                stages.Insert(0, new StageProgressData
                {
                    stageId = initialStageId,
                    isUnlocked = true
                });
            }

            normalized = new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                currency = source.currency,
                stages = stages,
                purchasedItemIds = purchasedItemIds
            };
            return true;
        }

        /// <summary>
        /// 有限かつ非負の時間を昇順に並べ、同タイムでは先に保存された記録を上位に保つ
        /// </summary>
        /// <param name="source">保存されていたクリアタイム</param>
        /// <param name="normalized">上位3件に整えた記録</param>
        /// <returns>すべての時間が有効な場合は true</returns>
        internal static bool TryNormalizeClearTimes(
            List<ClearTimeRecord> source,
            out List<ClearTimeRecord> normalized)
        {
            normalized = new List<ClearTimeRecord>();
            if (source == null)
            {
                return true;
            }

            foreach (var record in source)
            {
                if (record == null || float.IsNaN(record.seconds) ||
                    float.IsInfinity(record.seconds) || record.seconds < 0f)
                {
                    return false;
                }

                var insertIndex = 0;
                while (insertIndex < normalized.Count &&
                    normalized[insertIndex].seconds <= record.seconds)
                {
                    insertIndex++;
                }

                normalized.Insert(insertIndex, new ClearTimeRecord { seconds = record.seconds });
                if (normalized.Count > GameSaveData.MaximumClearTimeCount)
                {
                    normalized.RemoveAt(normalized.Count - 1);
                }
            }

            return true;
        }

        /// <summary>
        /// クリアタイムを昇順に挿入し、上位3件に入った場合の順位を返す
        /// </summary>
        /// <param name="clearTimes">更新対象の順位済みタイム一覧</param>
        /// <param name="seconds">追加するタイム</param>
        /// <returns>上位3件に入った場合は1始まりの順位、それ以外は0</returns>
        internal static int InsertClearTime(List<ClearTimeRecord> clearTimes, float seconds)
        {
            var insertIndex = 0;
            while (insertIndex < clearTimes.Count && clearTimes[insertIndex].seconds <= seconds)
            {
                insertIndex++;
            }

            if (insertIndex >= GameSaveData.MaximumClearTimeCount)
            {
                return 0;
            }

            clearTimes.Insert(insertIndex, new ClearTimeRecord { seconds = seconds });
            if (clearTimes.Count > GameSaveData.MaximumClearTimeCount)
            {
                clearTimes.RemoveAt(clearTimes.Count - 1);
            }

            return insertIndex + 1;
        }

        /// <summary>
        /// 保存データを変更用に複製する
        /// </summary>
        /// <param name="source">複製元データ</param>
        /// <returns>独立したリストを持つコピー</returns>
        internal static GameSaveData Clone(GameSaveData source)
        {
            var stages = source.stages.ConvertAll(stage => new StageProgressData
            {
                stageId = stage.stageId,
                isUnlocked = stage.isUnlocked,
                isCleared = stage.isCleared,
                clearTimes = stage.clearTimes.ConvertAll(record =>
                    new ClearTimeRecord { seconds = record.seconds })
            });

            return new GameSaveData
            {
                version = source.version,
                currency = source.currency,
                stages = stages,
                purchasedItemIds = new List<string>(source.purchasedItemIds)
            };
        }
    }
}
