using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ選択シーンの状態初期化と画面遷移を担当する
    /// </summary>
    public sealed class StageSelectController : MonoBehaviour
    {
        /// <summary>
        /// ステージ選択用の OutGame 状態を初期化する
        /// </summary>
        private void Awake()
        {
            OutGameStateController.Enter(GameFlowState.Menu);
        }

    }
}
