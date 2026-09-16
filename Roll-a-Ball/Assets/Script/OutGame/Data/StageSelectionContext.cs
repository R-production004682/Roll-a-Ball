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
        }

        /// <summary>
        /// 選択されたステージの次ステージを解放する
        /// </summary>
        public static void UnlockNextStage()
        {
            if (string.IsNullOrWhiteSpace(nextStageId))
            {
                return;
            }

            if (!GameDataManager.UnlockStage(nextStageId))
            {
                Debug.LogError($"次ステージを解放できませんでした: {nextStageId}");
            }
        }

        /// <summary>
        /// ドメイン再読み込み時に初期ステージの選択状態へ戻す
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            selectedStageId = GameDataManager.InitialStageId;
            nextStageId = "stage-2";
        }
    }
}
