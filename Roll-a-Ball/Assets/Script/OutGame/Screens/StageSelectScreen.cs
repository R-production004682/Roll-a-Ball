using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ選択画面で、ショップへの切り替えは同一シーンの Manager に委譲する
    /// </summary>
    public sealed class StageSelectScreen : ScreenBase
    {
        /// <summary>
        /// ステージ選択画面を表示状態にする
        /// </summary>
        public override void OnOpen(object arg)
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// ステージ選択画面を閉じ、表示対象から外す
        /// </summary>
        public override void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
