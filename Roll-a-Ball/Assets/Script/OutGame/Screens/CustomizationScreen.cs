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
        /// <summary>
        /// カスタマイズ画面から戻る Screen を表す
        /// </summary>
        public enum ReturnDestination
        {
            StageSelect,
            Shop
        }

        [SerializeField] private Button backButton;
        [SerializeField] private GameObject stageSelectBackLabel;
        [SerializeField] private GameObject shopBackLabel;
        private UiInputScope inputScope;
        private bool returnQueued;
        private ReturnDestination returnDestination;

        /// <summary>
        /// 入力範囲を取得する
        /// </summary>
        private void Awake()
        {
            inputScope = GetComponent<UiInputScope>();
        }

        /// <summary>
        /// 遷移元に応じた戻る先と操作を設定する
        /// </summary>
        /// <param name="arg">カスタマイズ画面から戻る Screen</param>
        public override void OnOpen(object arg)
        {
            returnQueued = false;
            returnDestination = arg is ReturnDestination destination
                ? destination
                : ReturnDestination.StageSelect;
            if (inputScope == null || backButton == null ||
                stageSelectBackLabel == null || shopBackLabel == null)
            {
                Debug.LogError("CustomizationScreen の戻るボタン、ラベル、または UiInputScope が設定されていません。", this);
                return;
            }

            RefreshBackLabel();
            backButton.onClick.AddListener(ReturnToSource);
            inputScope.CancelRequested.AddListener(ReturnToSource);
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
                backButton.onClick.RemoveListener(ReturnToSource);
            }

            if (inputScope != null)
            {
                inputScope.CancelRequested.RemoveListener(ReturnToSource);
            }
        }

        /// <summary>
        /// 遷移元の Screen へ戻る
        /// </summary>
        private void ReturnToSource()
        {
            if (returnQueued || inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            returnQueued = true;
            StartCoroutine(ReturnToSourceAfterInputFrame());
        }

        /// <summary>
        /// 戻るボタンのラベルを遷移元に合わせて切り替える
        /// </summary>
        private void RefreshBackLabel()
        {
            var returnsToShop = returnDestination == ReturnDestination.Shop;
            stageSelectBackLabel.SetActive(!returnsToShop);
            shopBackLabel.SetActive(returnsToShop);
        }

        /// <summary>
        /// Button の入力フレーム終了後に遷移元の Screen へ戻る
        /// </summary>
        /// <returns>一フレーム待機する Coroutine</returns>
        private IEnumerator ReturnToSourceAfterInputFrame()
        {
            yield return null;
            if (GameServices.Screens != null)
            {
                if (returnDestination == ReturnDestination.Shop)
                {
                    GameServices.Screens.Replace<ShopScreen>();
                    yield break;
                }

                GameServices.Screens.Replace<StageSelectScreen>();
                yield break;
            }

            Debug.LogError("ScreenManager がないため遷移元の Screen に戻れません。", this);
            returnQueued = false;
        }
    }
}
