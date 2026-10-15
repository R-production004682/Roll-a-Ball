using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// シーンの root Canvas にある設定ダイアログを Screen から利用可能にする
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsDialogHost : MonoBehaviour
    {
        [SerializeField, Tooltip("このシーンで使う設定画面を指定します。")]
        private SettingsDialogController settingsDialogController;

        /// <summary>
        /// 設定ダイアログの参照を検証して現在のシーンのサービス窓口へ登録する
        /// </summary>
        private void Awake()
        {
            if (settingsDialogController == null)
            {
                Debug.LogError("SettingsDialogHost に SettingsDialogController が設定されていません。", this);
                enabled = false;
                return;
            }

            GameServices.RegisterSettings(this);
        }

        /// <summary>
        /// static 初期化後にも設定ダイアログ Host をサービス窓口へ登録する
        /// </summary>
        private void Start()
        {
            if (isActiveAndEnabled && settingsDialogController != null)
            {
                GameServices.RegisterSettings(this);
            }
        }

        /// <summary>
        /// 破棄時に現在のシーンのサービス窓口から解除する
        /// </summary>
        private void OnDestroy()
        {
            GameServices.UnregisterSettings(this);
        }

        /// <summary>
        /// ステージ選択用の文脈で設定ダイアログを表示する
        /// </summary>
        public void OpenFromStageSelect()
        {
            if (!isActiveAndEnabled || settingsDialogController == null)
            {
                return;
            }

            OpenMenu(null);
        }

        /// <summary>
        /// タイトル用の文脈で設定を開き、閉じたフレームを呼び出し元へ通知する
        /// </summary>
        /// <param name="onClosed">タイトルの開始入力を再開する処理</param>
        public void OpenFromTitle(System.Action onClosed)
        {
            if (!isActiveAndEnabled || settingsDialogController == null)
            {
                return;
            }

            OpenMenu(onClosed);
        }

        /// <summary>
        /// 親メニューから共通の音量設定を表示する
        /// </summary>
        public void OpenAudioFromMenu()
        {
            if (isActiveAndEnabled && settingsDialogController != null)
            {
                settingsDialogController.OpenFromStageSelect();
            }
        }

        /// <summary>
        /// 登録済みの設定メニューを表示する
        /// </summary>
        /// <param name="onClosed">親メニューを閉じたときの任意の通知</param>
        private void OpenMenu(System.Action onClosed)
        {
            var manager = GameServices.Screens;
            if (manager == null)
            {
                Debug.LogError("設定メニューを表示する ScreenManager がありません。", this);
                return;
            }

            manager.Push<SettingsMenuDialog>(onClosed);
        }
    }
}
