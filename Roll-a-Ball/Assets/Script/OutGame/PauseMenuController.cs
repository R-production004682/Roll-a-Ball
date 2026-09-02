using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGameTestScene のメニューとポーズ状態を管理
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuDialog;

        private bool isPaused;

        private void Awake()
        {
            Time.timeScale = 1f;
            isPaused = false;

            if (menuDialog != null)
            {
                menuDialog.SetActive(false);
            }
        }

        /// <summary>
        /// メニューボタンからダイアログを表示
        /// </summary>
        public void OpenMenu()
        {
            if (menuDialog != null)
            {
                menuDialog.SetActive(true);
            }
        }

        /// <summary>
        /// タイトルへ戻る
        /// </summary>
        public void GoToTitle()
        {
            isPaused = false;
            Time.timeScale = 1f;
            SceneRouter.LoadScene(SceneType.Title, this);
        }

        /// <summary>
        /// ダイアログを閉じてゲームを再開
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (menuDialog != null)
            {
                menuDialog.SetActive(false);
            }
        }

        /// <summary>
        /// ゲームをポーズ
        /// </summary>
        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (isPaused)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
