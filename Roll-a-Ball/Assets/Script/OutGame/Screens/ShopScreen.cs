using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// StageSelectScreen と入れ替えて表示するショップ画面
    /// </summary>
    public sealed class ShopScreen : ScreenBase
    {
        [SerializeField] private Button backButton;
        private UiInputScope inputScope;

        /// <summary>
        /// 共通入力範囲を取得する
        /// </summary>
        private void Awake()
        {
            inputScope = GetComponent<UiInputScope>();
        }

        /// <summary>
        /// ショップ画面を表示し、戻る操作を登録する
        /// </summary>
        public override void OnOpen(object arg)
        {
            if (backButton == null || inputScope == null)
            {
                Debug.LogError("ShopScreen の戻るボタンまたは UiInputScope が設定されていません。", this);
                return;
            }

            backButton.onClick.AddListener(ReturnToStages);
            inputScope.CancelRequested.AddListener(ReturnToStages);
            backButton.Select();
        }

        /// <summary>
        /// ショップ画面を閉じ、戻る操作の購読を解除する
        /// </summary>
        public override void OnClose()
        {
            if (inputScope != null)
            {
                inputScope.CancelRequested.RemoveListener(ReturnToStages);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ReturnToStages);
            }
        }

        /// <summary>
        /// StageSelectScreen へ同一シーン内で戻る
        /// </summary>
        private void ReturnToStages()
        {
            if (inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            if (GameServices.Screens != null)
            {
                GameServices.Screens.Replace<StageSelectScreen>();
                return;
            }

            Debug.LogError("ScreenManager がないため StageSelectScreen に戻れません。", this);
        }
    }
}
