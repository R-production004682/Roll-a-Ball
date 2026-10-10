using TMPro;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 共通の所持コイン数を表示し、取得や購入による変更を反映する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CurrencyDisplay : MonoBehaviour
    {
        private TMP_Text currencyLabel;

        /// <summary>
        /// 同じオブジェクトのコイン表示テキストを取得する
        /// </summary>
        private void Awake()
        {
            currencyLabel = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 表示開始時の所持コイン数を反映し、データ変更を購読する
        /// </summary>
        private void OnEnable()
        {
            GameDataManager.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// 非表示やシーン破棄時にデータ変更の購読を解除する
        /// </summary>
        private void OnDisable()
        {
            GameDataManager.Changed -= Refresh;
        }

        /// <summary>
        /// 現在の所持コイン数を桁区切り付きで表示する
        /// </summary>
        private void Refresh()
        {
            currencyLabel.text = $"COIN  {GameDataManager.Currency:N0}";
        }
    }
}
