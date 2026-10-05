using System.Collections.Generic;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ選択からゲームシーンへ渡す一時的な選択状態を保持する
    /// </summary>
    public static class StageSelectionContext
    {
        private static string selectedStageId = GameDataManager.InitialStageId;
        private static string nextStageId = "stage-2";
        private static string stageSelectId = GameDataManager.InitialStageId;
        private static string newlyUnlockedStageId;

        /// <summary>
        /// 現在プレイするステージの安定 ID を取得する
        /// </summary>
        public static string SelectedStageId => selectedStageId;

        /// <summary>
        /// ステージ選択で決定したステージと、クリア時に解放する次ステージを設定する
        /// </summary>
        /// <param name="stageId">プレイするステージの安定 ID</param>
        /// <param name="followingStageId">クリア時に解放する次ステージの安定 ID</param>
        public static void Select(string stageId, string followingStageId)
        {
            selectedStageId = string.IsNullOrWhiteSpace(stageId) ? GameDataManager.InitialStageId : stageId;
            nextStageId = followingStageId;
            stageSelectId = selectedStageId;
            newlyUnlockedStageId = null;
        }

        /// <summary>
        /// 選択ステージがクリア済みで次の Prefab が存在する場合だけ解放する
        /// </summary>
        public static void UnlockNextStage()
        {
            if (string.IsNullOrWhiteSpace(nextStageId) || !GameDataManager.IsStageCleared(selectedStageId) ||
                !StagePrefabCatalog.IsStageAvailable(nextStageId))
            {
                return;
            }

            UnlockStageForReturn(nextStageId);
        }

        /// <summary>
        /// クリア後に Prefab が追加されたステージも、直前のクリア記録から解放する
        /// </summary>
        internal static void UnlockAvailableStages(IEnumerable<StageDefinition> stages)
        {
            foreach (var stage in stages)
            {
                if (stage.StageNumber > 1 && StagePrefabCatalog.IsStageAvailable(stage.StageId) &&
                    GameDataManager.IsStageCleared($"stage-{stage.StageNumber - 1}"))
                {
                    UnlockStageForReturn(stage.StageId);
                }
            }
        }

        /// <summary>
        /// 新規解放に成功したステージだけを次回のステージ選択表示先にする
        /// </summary>
        private static void UnlockStageForReturn(string stageId)
        {
            if (GameDataManager.IsStageUnlocked(stageId))
            {
                return;
            }

            if (GameDataManager.UnlockStage(stageId))
            {
                newlyUnlockedStageId = stageId;
            }
            else
            {
                Debug.LogError($"次ステージを解放できませんでした: {stageId}");
            }
        }

        /// <summary>
        /// 新規解放の表示予約を一度だけ消費し、以降は最後に表示した選択へ戻す
        /// </summary>
        internal static string ConsumeStageSelectId()
        {
            if (!string.IsNullOrWhiteSpace(newlyUnlockedStageId) &&
                StagePrefabCatalog.IsStageAvailable(newlyUnlockedStageId) &&
                GameDataManager.IsStageUnlocked(newlyUnlockedStageId))
            {
                stageSelectId = newlyUnlockedStageId;
            }

            newlyUnlockedStageId = null;
            return stageSelectId;
        }

        /// <summary>
        /// 別のアウトゲーム画面から戻るため、表示中の選択を保持する
        /// </summary>
        internal static void RememberStageSelectId(string stageId)
        {
            stageSelectId = stageId;
        }

        /// <summary>
        /// ドメイン再読み込み時に初期ステージの選択状態へ戻す
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            selectedStageId = GameDataManager.InitialStageId;
            nextStageId = "stage-2";
            stageSelectId = GameDataManager.InitialStageId;
            newlyUnlockedStageId = null;
        }
    }
}
