using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ選択から開くカスタマイズ Screen の入口
    /// </summary>
    public sealed class CustomizationScreen : ScreenBase
    {
        [SerializeField] private Button backButton;
        private UiInputScope inputScope;
        private bool returnQueued;

        /// <summary>
        /// 入力範囲を取得する
        /// </summary>
        private void Awake()
        {
            inputScope = GetComponent<UiInputScope>();
        }

        /// <summary>
        /// 戻る操作を登録する
        /// </summary>
        public override void OnOpen(object arg)
        {
            returnQueued = false;
            if (inputScope == null || backButton == null)
            {
                Debug.LogError("CustomizationScreen の戻るボタンまたは UiInputScope が設定されていません。", this);
                return;
            }

            backButton.onClick.AddListener(ReturnToStages);
            inputScope.CancelRequested.AddListener(ReturnToStages);
            backButton.Select();
        }

        /// <summary>
        /// 戻る操作を解除する
        /// </summary>
        public override void OnClose()
        {
            returnQueued = false;
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ReturnToStages);
            }

            if (inputScope != null)
            {
                inputScope.CancelRequested.RemoveListener(ReturnToStages);
            }
        }

        /// <summary>
        /// ステージ選択 Screen へ戻る
        /// </summary>
        private void ReturnToStages()
        {
            if (returnQueued || inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            returnQueued = true;
            StartCoroutine(ReturnToStagesAfterInputFrame());
        }

        /// <summary>
        /// Button の入力フレーム終了後にステージ選択 Screen へ戻る
        /// </summary>
        /// <returns>一フレーム待機する Coroutine</returns>
        private IEnumerator ReturnToStagesAfterInputFrame()
        {
            yield return null;
            if (GameServices.Screens != null)
            {
                GameServices.Screens.Replace<StageSelectScreen>();
                yield break;
            }

            Debug.LogError("ScreenManager がないため StageSelectScreen に戻れません。", this);
            returnQueued = false;
        }
    }
}
