using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ホストシーンの初期 Screen を一度だけ開く
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenBootstrapper : MonoBehaviour
    {
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private ScreenBase initialScreen;

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

            Debug.Log($"初期 Screen を表示します: {initialScreen.GetType().Name}", this);
            screenManager.Replace(initialScreen.GetType());
        }
    }
}
