using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Title 画面を初期化し、ゲーム開始操作を受け取る
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

        /// <summary>
        /// MainScene を読み込みゲームを開始する
        /// </summary>
        public void StartGame()
        {
            SceneRouter.LoadScene(SceneType.Main, this);
        }
    }
}
