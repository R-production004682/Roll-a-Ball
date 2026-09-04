using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ゲーム中のメニュー表示とポーズ操作を管理
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        private const KeyCode MenuKey = KeyCode.M;

        [SerializeField] private GameObject menuDialog;
        [SerializeField] private GameObject menuButton;
        [SerializeField] private GameObject controlsGuide;
        [SerializeField, Header("ゲーム開始時の状態")] private bool startsInPlay;

        /// <summary>
        /// メニュー UI とゲーム開始状態を初期化
        /// </summary>
        private void Awake()
        {
            if (menuDialog != null)
            {
                menuDialog.SetActive(false);
            }

            if (startsInPlay && menuButton != null)
            {
                menuButton.SetActive(false);
            }

            if (startsInPlay)
            {
                OutGameStateController.Enter(GameFlowState.Playing);
            }
            else
            {
                OutGameStateController.Enter(GameFlowState.Menu);
            }

            RefreshControlsGuide(OutGameStateController.Current);
        }

        /// <summary>
        /// ゲーム進行状態の変更通知を購読
        /// </summary>
        private void OnEnable() => OutGameStateController.StateChanged += RefreshControlsGuide;

        /// <summary>
        /// ゲーム進行状態の変更通知を解除
        /// </summary>
        private void OnDisable() => OutGameStateController.StateChanged -= RefreshControlsGuide;

        /// <summary>
        /// プレイ中のメニューキー入力を監視する
        /// </summary>
        private void Update()
        {
            // インプレイ中に、メニューキーが押されたらメニューを開く
            if (OutGameStateController.IsPlaying && Input.GetKeyDown(MenuKey))
            {
                OpenMenu();
            }
        }

        /// <summary>
        /// メニューボタンからダイアログを表示
        /// </summary>
        public void OpenMenu()
        {
            if (menuDialog == null)
            {
                Debug.LogError("PauseMenuController に MenuDialog が設定されていません。", this);
                return;
            }

            menuDialog.SetActive(true);
            OutGameStateController.Enter(GameFlowState.Paused);
        }

        /// <summary>
        /// 既存 Scene の Button イベントからポーズメニューを開く
        /// </summary>
        public void PauseGame()
        {
            OpenMenu();
        }

        /// <summary>
        /// タイトルへ戻る
        /// </summary>
        public void GoToTitle()
        {
            SceneRouter.LoadScene(SceneType.Title, this);
        }

        /// <summary>
        /// ダイアログを閉じてゲームを再開
        /// </summary>
        public void ResumeGame()
        {
            OutGameStateController.Enter(GameFlowState.Playing);

            if (menuDialog != null)
            {
                menuDialog.SetActive(false);
            }
        }

        /// <summary>
        /// ゲーム進行状態に応じて操作ガイドの表示を切り替える
        /// </summary>
        /// <param name="state">反映するゲーム進行状態</param>
        private void RefreshControlsGuide(GameFlowState state)
        {
            if (controlsGuide != null)
            {
                controlsGuide.SetActive(state == GameFlowState.Playing);
            }
        }
    }
}
