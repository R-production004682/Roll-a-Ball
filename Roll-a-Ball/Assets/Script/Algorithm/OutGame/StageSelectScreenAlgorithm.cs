using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// StageSelectScreen の表示整形と Inspector 参照検証を行う
    /// </summary>
    internal static class StageSelectScreenAlgorithm
    {
        /// <summary>
        /// ランキング順位とクリアタイムを画面表示用の文字列へ変換する
        /// </summary>
        /// <param name="rank">1 始まりのランキング順位</param>
        /// <param name="seconds">表示する秒数。負数は未記録を表す</param>
        /// <returns>ランキングに表示する順位付き時間文字列</returns>
        internal static string FormatRankingEntry(int rank, float seconds)
        {
            return $"{rank}. {FormatClearTime(seconds)}";
        }

        /// <summary>
        /// 未設定または不正な StageSelectScreen の Inspector 参照名を取得する
        /// </summary>
        /// <param name="inputScope">画面の入力範囲</param>
        /// <param name="stages">画面に表示するステージ定義</param>
        /// <param name="currencyLabel">所持金表示</param>
        /// <param name="stageNumberLabel">ステージ番号表示</param>
        /// <param name="stageNameLabel">ステージ名表示</param>
        /// <param name="clearTimeLabels">ランキング表示</param>
        /// <param name="stagePreview">ステージのプレビュー画像</param>
        /// <param name="lockedOverlay">ロック状態の表示</param>
        /// <param name="previousButton">前のステージを選ぶボタン</param>
        /// <param name="nextButton">次のステージを選ぶボタン</param>
        /// <param name="stagePreviewButton">プレビューの決定ボタン</param>
        /// <param name="playButton">プレイ開始ボタン</param>
        /// <param name="shopButton">ショップ表示ボタン</param>
        /// <param name="customizeButton">カスタマイズ表示ボタン</param>
        /// <param name="settingsButton">設定表示ボタン</param>
        /// <returns>未設定または不正な参照名一覧</returns>
        internal static IReadOnlyList<string> GetMissingReferences(
            UiInputScope inputScope,
            StageDefinition[] stages,
            TMP_Text currencyLabel,
            TMP_Text stageNumberLabel,
            TMP_Text stageNameLabel,
            TMP_Text[] clearTimeLabels,
            RawImage stagePreview,
            GameObject lockedOverlay,
            Button previousButton,
            Button nextButton,
            Button stagePreviewButton,
            Button playButton,
            Button shopButton,
            Button customizeButton,
            Button settingsButton)
        {
            var missingReferences = new List<string>();
            AddMissingReference(inputScope, nameof(inputScope), missingReferences);
            AddMissingStageDefinitions(stages, missingReferences);
            AddMissingReference(currencyLabel, nameof(currencyLabel), missingReferences);
            AddMissingReference(stageNumberLabel, nameof(stageNumberLabel), missingReferences);
            AddMissingReference(stageNameLabel, nameof(stageNameLabel), missingReferences);
            AddMissingClearTimeLabels(clearTimeLabels, missingReferences);
            AddMissingReference(stagePreview, nameof(stagePreview), missingReferences);
            AddMissingReference(lockedOverlay, nameof(lockedOverlay), missingReferences);
            AddMissingReference(previousButton, nameof(previousButton), missingReferences);
            AddMissingReference(nextButton, nameof(nextButton), missingReferences);
            AddMissingReference(stagePreviewButton, nameof(stagePreviewButton), missingReferences);
            AddMissingReference(playButton, nameof(playButton), missingReferences);
            AddMissingReference(shopButton, nameof(shopButton), missingReferences);
            AddMissingReference(customizeButton, nameof(customizeButton), missingReferences);
            AddMissingReference(settingsButton, nameof(settingsButton), missingReferences);
            return missingReferences;
        }

        /// <summary>
        /// 秒数をランキング表示用の分秒ミリ秒表記へ変換する
        /// </summary>
        /// <param name="seconds">表示する秒数。負数は未記録を表す</param>
        /// <returns>ランキングに表示する時間文字列</returns>
        private static string FormatClearTime(float seconds)
        {
            if (seconds < 0f)
            {
                return StageSelectScreenConstants.UnrecordedClearTimeText;
            }

            var totalCentiseconds = Mathf.FloorToInt(
                seconds * StageSelectScreenConstants.CentisecondsPerSecond);
            var minutes = totalCentiseconds / StageSelectScreenConstants.CentisecondsPerMinute;
            var remainingCentiseconds = totalCentiseconds % StageSelectScreenConstants.CentisecondsPerMinute;
            var centisecondsPerSecond = (int)StageSelectScreenConstants.CentisecondsPerSecond;
            return $"{minutes:00}:{remainingCentiseconds / centisecondsPerSecond:00}:{remainingCentiseconds % centisecondsPerSecond:00}";
        }

        /// <summary>
        /// ステージ定義の配列と各要素を検証する
        /// </summary>
        /// <param name="stages">画面に表示するステージ定義</param>
        /// <param name="missingReferences">未設定または不正な参照を格納する一覧</param>
        private static void AddMissingStageDefinitions(
            StageDefinition[] stages,
            ICollection<string> missingReferences)
        {
            if (stages == null || stages.Length == 0)
            {
                missingReferences.Add(nameof(stages));
                return;
            }

            for (var index = 0; index < stages.Length; index++)
            {
                if (stages[index] == null)
                {
                    missingReferences.Add($"{nameof(stages)}[{index}]");
                }
            }
        }

        /// <summary>
        /// ランキング表示の配列と各要素を検証する
        /// </summary>
        /// <param name="clearTimeLabels">ランキング表示</param>
        /// <param name="missingReferences">未設定または不正な参照を格納する一覧</param>
        private static void AddMissingClearTimeLabels(
            TMP_Text[] clearTimeLabels,
            ICollection<string> missingReferences)
        {
            if (clearTimeLabels == null ||
                clearTimeLabels.Length != GameSaveData.MaximumClearTimeCount)
            {
                missingReferences.Add(nameof(clearTimeLabels));
                return;
            }

            for (var index = 0; index < clearTimeLabels.Length; index++)
            {
                AddMissingReference(
                    clearTimeLabels[index],
                    $"{nameof(clearTimeLabels)}[{index}]",
                    missingReferences);
            }
        }

        /// <summary>
        /// 未設定の参照だけを結果一覧へ追加する
        /// </summary>
        /// <param name="reference">検証する Unity オブジェクト</param>
        /// <param name="referenceName">Inspector 上の参照名</param>
        /// <param name="missingReferences">未設定または不正な参照を格納する一覧</param>
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
