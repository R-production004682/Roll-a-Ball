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

        private readonly List<MonoBehaviour> stoppedBehaviours = new List<MonoBehaviour>();
        private bool isCleared;

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
            StopGameplayInput();

            Time.timeScale = 0f;
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

        /// <summary>
        /// ゲームプレイ中の MonoBehaviour を停止して、クリア画面の UI 操作だけを有効にする
        /// </summary>
        private void StopGameplayInput()
        {
            stoppedBehaviours.Clear();

            // MonoBehaviour のみを止めることで、Camera / Light / AudioListener と
            // クリア画面の UI イベント処理は維持する。
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.enabled || ShouldKeepEnabled(behaviour))
                {
                    continue;
                }

                behaviour.enabled = false;
                stoppedBehaviours.Add(behaviour);
            }
        }

        /// <summary>
        /// クリア画面の UI 操作に必要な MonoBehaviour は停止しない
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        private bool ShouldKeepEnabled(MonoBehaviour behaviour)
        {
            return behaviour == this
                || behaviour is Graphic
                || behaviour is Selectable
                || behaviour is EventSystem
                || behaviour is BaseInputModule
                || behaviour is BaseRaycaster
                || behaviour is PauseMenuController;
        }

        /// <summary>
        /// ゲームクリア画面を閉じるときに、停止していた MonoBehaviour を再開
        /// </summary>
        private void OnDestroy()
        {
            Time.timeScale = 1f;

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
