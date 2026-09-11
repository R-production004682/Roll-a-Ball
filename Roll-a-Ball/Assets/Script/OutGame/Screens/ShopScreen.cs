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

        /// <summary>
        /// ショップ画面を表示し、戻る操作を登録する
        /// </summary>
        public override void OnOpen(object arg)
        {
            if (backButton == null)
            {
                Debug.LogError("ShopScreen の戻るボタンが設定されていません。", this);
                return;
            }

            backButton.onClick.AddListener(ReturnToStages);
            backButton.Select();
            Debug.Log("ShopScreen を表示しました。", this);
        }

        /// <summary>
        /// ショップ画面を閉じ、戻る操作の購読を解除する
        /// </summary>
        public override void OnClose()
        {
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
            if (GameServices.Screens != null)
            {
                GameServices.Screens.Replace<StageSelectScreen>();
                return;
            }

            Debug.LogError("ScreenManager がないため StageSelectScreen に戻れません。", this);
        }
    }
}
