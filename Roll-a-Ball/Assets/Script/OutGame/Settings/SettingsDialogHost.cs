using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// シーンの root Canvas にある設定ダイアログを Screen から利用可能にする
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsDialogHost : MonoBehaviour
    {
        [SerializeField] private SettingsDialogController settingsDialogController;

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

            settingsDialogController.OpenFromStageSelect();
        }
    }
}
