using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ選択シーンの状態初期化と画面遷移を担当する
    /// </summary>
    public sealed class StageSelectController : MonoBehaviour
    {
        [SerializeField] private ScreenManager screenManager;

        /// <summary>
        /// ステージ選択用の OutGame 状態を初期化する
        /// </summary>
        private void Awake() => OutGameStateController.Enter(GameFlowState.Menu);

        /// <summary>
        /// ショップを別シーンへロードせず、現在の Canvas 上で切り替える
        /// </summary>
        public void OpenShop()
        {
            var manager = screenManager != null ? screenManager : GameServices.Screens;
            if (manager == null)
            {
                Debug.LogError("StageSelectScene に ScreenManager が設定されていません。", this);
                return;
            }

            manager.Replace<ShopScreen>();
        }
    }
}
