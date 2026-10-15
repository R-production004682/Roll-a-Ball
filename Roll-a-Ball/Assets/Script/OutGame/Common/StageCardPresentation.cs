using TMPro;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージカードの未解放・選択中・クリア済みを文字とアイコンで表示する
    /// </summary>
    public sealed class StageCardPresentation : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private UnityEngine.UI.Image statusIcon;
        [SerializeField] private UnityEngine.UI.Image[] stageDots;
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Sprite checkSprite;
        [SerializeField] private Sprite leafSprite;

        /// <summary>
        /// 選択されたステージの状態と旅全体の現在位置を反映する
        /// </summary>
        public void Refresh(string stageId, int number, bool unlocked)
        {
            var cleared = GameDataManager.IsStageCleared(stageId);
            if (!unlocked)
            {
                statusLabel.text = "未解放";
                statusIcon.sprite = lockSprite;
            }
            else if (cleared)
            {
                statusLabel.text = "クリア済み";
                statusIcon.sprite = checkSprite;
            }
            else
            {
                statusLabel.text = "選択中";
                statusIcon.sprite = leafSprite;
            }

            for (var index = 0; index < stageDots.Length; index++)
            {
                stageDots[index].color = index + 1 == number
                    ? new Color32(69, 102, 70, 255)
                    : new Color32(201, 189, 158, 255);
            }
        }
    }
}
