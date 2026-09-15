using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 旧シーンからステージのクリア状態だけを共通ゲームデータへ通知する
    /// </summary>
    public sealed class OutGameClearController : MonoBehaviour
    {
        [SerializeField, Tooltip("ステージ進行データとクリア記録に使う安定 ID")]
        private string stageId = GameDataManager.InitialStageId;

        /// <summary>
        /// ステージ ID を検証し、未設定なら初期ステージ ID を使用する
        /// </summary>
        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                Debug.LogError("OutGameClearController のステージ ID が未設定です。初期ステージ ID を使用します。", this);
                stageId = GameDataManager.InitialStageId;
            }

        }

        /// <summary>
        /// ステージをクリア済みとして保存する
        /// </summary>
        public void MarkStageCleared()
        {
            if (!GameDataManager.TryMarkStageCleared(stageId))
            {
                Debug.LogError($"ステージをクリア済みに保存できませんでした: {stageId}", this);
            }
        }
    }
}
