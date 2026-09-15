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
        private UiInputScope clearInputScope;

        // ゲームクリア状態かどうかを記録するフラグ
        private bool isCleared;

        /// <summary>
        /// クリアダイアログを初期化し、必須の UI 参照を検証する
        /// </summary>
        private void Awake()
        {
            clearInputScope = clearDialog != null ? clearDialog.GetComponent<UiInputScope>() : null;
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
        /// 結果画面の共通 Cancel を Title への戻り操作へ接続する
        /// </summary>
        private void OnEnable()
        {
            if (clearInputScope != null)
            {
                clearInputScope.CancelRequested.AddListener(CancelToTitle);
            }
        }

        /// <summary>
        /// 結果画面の共通 Cancel 購読を解除する
        /// </summary>
        private void OnDisable()
        {
            if (clearInputScope != null)
            {
                clearInputScope.CancelRequested.RemoveListener(CancelToTitle);
            }
        }

        /// <summary>
        /// プレイ中のクリアキー入力を監視する
        /// </summary>
        private void Update()
        {
            if (OutGameStateController.IsPlaying && !UiInputScope.BlocksPlayer && Input.GetKeyDown(ClearKey))
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

        /// <summary>
        /// 結果画面で Cancel されたとき Title ボタンの共通遷移を実行する
        /// </summary>
        private void CancelToTitle()
        {
            if (isCleared && titleButton != null && titleButton.isActiveAndEnabled)
            {
                titleButton.onClick.Invoke();
            }
        }

    }
}
