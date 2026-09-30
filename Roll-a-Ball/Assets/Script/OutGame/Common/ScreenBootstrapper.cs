using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ホストシーンの初期 Screen を一度だけ開く
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenBootstrapper : MonoBehaviour
    {
        [SerializeField, Tooltip("最初の画面を表示する管理役です。未設定なら同じオブジェクトから探します。")]
        private ScreenManager screenManager;
        [SerializeField, Tooltip("ゲーム開始時に表示する画面のプレハブです。")]
        private ScreenBase initialScreen;

        /// <summary>
        /// Inspector の設定を確認し、初期 Screen を表示する
        /// </summary>
        private void Start()
        {
            if (screenManager == null)
            {
                screenManager = GetComponent<ScreenManager>();
            }

            if (screenManager == null || initialScreen == null)
            {
                Debug.LogError("ScreenBootstrapper の ScreenManager または initialScreen が未設定です。", this);
                return;
            }

            screenManager.Replace(initialScreen.GetType());
        }
    }
}
