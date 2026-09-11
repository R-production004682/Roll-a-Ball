using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGameTestScene 内でゲームクリアを完結させるための状態だけを管理し、UI の表示と遷移を制御
    /// </summary>
    public sealed class OutGameClearController : MonoBehaviour
    {
        private const KeyCode ClearKey = KeyCode.C;

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
                Debug.LogWarning("クリア処理はすでに完了しています。", this);
                return;
            }

            isCleared = true;

            OutGameStateController.Enter(GameFlowState.Cleared);

            if (clearDialog != null)
            {
                clearDialog.SetActive(true);
            }

            if (EventSystem.current != null && titleButton != null)
            {
                EventSystem.current.SetSelectedGameObject(titleButton.gameObject);
            }

            Debug.Log("ゲームクリアを確定し、クリアダイアログを表示しました。", this);
        }

    }
}
