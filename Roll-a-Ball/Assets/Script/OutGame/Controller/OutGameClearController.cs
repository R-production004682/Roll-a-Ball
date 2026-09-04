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
        private const KeyCode ClearKey = KeyCode.C;

        [SerializeField] private Button clearButton;
        [SerializeField] private GameObject clearDialog;
        [SerializeField] private Button titleButton;

        // ゲームクリア状態かどうかを記録するフラグ
        private bool isCleared;

        /// <summary>
        /// クリアダイアログを初期化し、必須の UI 参照を検証する
        /// </summary>
        private void Awake()
        {
            if (clearDialog != null)
            {
                clearDialog.SetActive(false);
            }

            if (clearDialog == null || titleButton == null)
            {
                Debug.LogError("OutGameClearController の UI 参照が設定されていません。", this);
            }
        }

        /// <summary>
        /// ゲーム開始状態に応じてデバッグ用クリアボタンの表示を切り替える
        /// </summary>
        private void Start()
        {
            if (clearButton != null)
            {
                clearButton.gameObject.SetActive(!OutGameStateController.IsPlaying);
            }
        }

        /// <summary>
        /// プレイ中のクリアキー入力を監視する
        /// </summary>
        private void Update()
        {
            if (OutGameStateController.IsPlaying && Input.GetKeyDown(ClearKey))
            {
                MarkClear();
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

            OutGameStateController.Enter(GameFlowState.Cleared);

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
            SceneRouter.LoadScene(SceneType.Title, this);
        }

        /// <summary>
        /// MainScene を読み直してリトライする
        /// </summary>
        public void Retry()
        {
            SceneRouter.LoadScene(SceneType.Main, this);
        }
    }
}
