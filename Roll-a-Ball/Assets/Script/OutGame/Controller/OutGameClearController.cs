using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// プレイ中のステージのクリア状態を共通ゲームデータへ通知する
    /// </summary>
    public sealed class OutGameClearController : MonoBehaviour
    {
        [SerializeField, Tooltip("ステージ進行データとクリア記録に使う安定 ID")]
        private string stageId = GameDataManager.InitialStageId;
        private bool hasRecordedClear;

        /// <summary>
        /// ステージ ID を検証し、未設定なら初期ステージ ID を使用する
        /// </summary>
        private void Awake()
        {
            stageId = StageSelectionContext.SelectedStageId;
            if (string.IsNullOrWhiteSpace(stageId))
            {
                Debug.LogError("OutGameClearController のステージ ID が未設定です。初期ステージ ID を使用します。", this);
                stageId = GameDataManager.InitialStageId;
            }

        }

        /// <summary>
        /// ステージをクリア済みにし、次のステージを解放する
        /// </summary>
        public void MarkStageCleared()
        {
            if (hasRecordedClear)
            {
                return;
            }

            if (!GameDataManager.TryMarkStageCleared(stageId))
            {
                Debug.LogError($"ステージをクリア済みに保存できませんでした: {stageId}", this);
                return;
            }

            hasRecordedClear = true;
            StageSelectionContext.UnlockNextStage();
        }
    }
}
