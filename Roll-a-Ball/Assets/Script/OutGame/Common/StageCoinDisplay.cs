using TMPro;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 保存済み所持金と分けて、このプレイで集めたコインの金額を表示する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class StageCoinDisplay : MonoBehaviour
    {
        private TMP_Text coinLabel;
        private GameManager gameManager;

        /// <summary>
        /// 同じオブジェクトのコイン表示テキストを取得する
        /// </summary>
        private void Awake()
        {
            coinLabel = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 現在のプレイの取得金額を表示し、取得金額の変更を購読する
        /// </summary>
        private void OnEnable()
        {
            gameManager = GameManager.instance;
            if (gameManager == null)
            {
                coinLabel.text = "COIN  0";
                Debug.LogError("StageCoinDisplay: GameManager が設定されたゲームシーンに配置してください。", this);
                return;
            }

            gameManager.CollectedCoinsChanged += Refresh;
            Refresh();
        }

        /// <summary>
        /// 非表示やシーン破棄時に取得金額の変更の購読を解除する
        /// </summary>
        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.CollectedCoinsChanged -= Refresh;
            }

            gameManager = null;
        }

        /// <summary>
        /// このプレイで集めた金額を桁区切り付きで表示する
        /// </summary>
        private void Refresh()
        {
            coinLabel.text = $"COIN  {gameManager.CollectedCoins:N0}";
        }
    }
}
