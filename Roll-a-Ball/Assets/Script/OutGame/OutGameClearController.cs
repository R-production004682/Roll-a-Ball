using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGameTestScene 内でゲームクリアを完結させるための状態だけを管理
    /// UI 自体は OutGameTestScene に配置し、ここでは参照して表示／遷移を制御
    /// </summary>
    public sealed class OutGameClearController : MonoBehaviour
    {
        [SerializeField] private Button clearButton;
        [SerializeField] private GameObject clearDialog;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private List<MonoBehaviour> gameplayBehaviours = new List<MonoBehaviour>();

        private readonly List<MonoBehaviour> stoppedBehaviours = new List<MonoBehaviour>();
        private bool isCleared;
        private float timeScaleBeforeClear;
        private bool hasChangedTimeScale;

        private void Awake()
        {
            Time.timeScale = 1f;

            // OutGameTestScene は通常の UI 画面なので、開始時からクリックできる状態にする。
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 既存シーンの Canvas ルートに残っている 0 スケールを補正
            transform.localScale = Vector3.one;

            if (clearDialog != null)
            {
                clearDialog.SetActive(false);
            }

            if (clearButton == null || clearDialog == null || titleButton == null || retryButton == null)
            {
                Debug.LogError("OutGameClearController の UI 参照が設定されていません。", this);
            }
        }

        /// <summary>
        /// ゲームクリアを確定しクリア画面を表示
        /// </summary>
        public void MarkClear()
        {
            if (isCleared)
            {
                return;
            }

            isCleared = true;

            timeScaleBeforeClear = Time.timeScale;
            Time.timeScale = 0f;
            hasChangedTimeScale = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (clearButton != null)
            {
                clearButton.interactable = false;
            }

            if (clearDialog != null)
            {
                clearDialog.SetActive(true);
            }

            if (EventSystem.current != null && titleButton != null)
            {
                EventSystem.current.SetSelectedGameObject(titleButton.gameObject);
            }
        }

        /// <summary>
        /// TitleScene に戻る
        /// </summary>
        public void GoToTitle()
        {
            Time.timeScale = 1f;
            SceneRouter.LoadScene(SceneType.Title, this);
        }

        /// <summary>
        /// OutGameTestScene を読み直してリトライする
        /// </summary>
        public void Retry()
        {
            Time.timeScale = 1f;
            SceneRouter.LoadScene(SceneType.OutGameTest, this);
        }

        /// ゲームクリア画面を閉じるときに、停止していた MonoBehaviour を再開
        /// </summary>
        private void OnDestroy()
        {
            if (hasChangedTimeScale)
            {
                Time.timeScale = timeScaleBeforeClear;
            }

            foreach (var behaviour in stoppedBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }
        }
    }
}
