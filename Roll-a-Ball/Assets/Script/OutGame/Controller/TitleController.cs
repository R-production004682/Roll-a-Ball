using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Title 画面のゲーム進行状態を初期化する
    /// </summary>
    public sealed class TitleController : MonoBehaviour
    {
        /// <summary>
        /// タイトル画面用のゲーム進行状態を設定する
        /// </summary>
        private void Awake()
        {
            OutGameStateController.Enter(GameFlowState.Menu);
        }

    }
}
