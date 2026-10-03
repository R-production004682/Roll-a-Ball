using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 商品情報と説明を表示し、購入またはキャンセルの結果を返すダイアログ
    /// </summary>
    public sealed class ShopPurchaseDialog : DialogBase<bool>
    {
        [SerializeField, Tooltip("購入する商品の画像を表示します。")]
        private Image iconImage;
        [SerializeField, Tooltip("商品のカテゴリを表示します。")]
        private TMP_Text categoryLabel;
        [SerializeField, Tooltip("商品名を表示します。")]
        private TMP_Text nameLabel;
        [SerializeField, Tooltip("商品の説明を表示します。")]
        private TMP_Text descriptionLabel;
        [SerializeField, Tooltip("商品の価格を表示します。")]
        private TMP_Text priceValueLabel;
        [SerializeField, Tooltip("商品を購入するときに押すボタンです。")]
        private Button purchaseButton;
        [SerializeField, Tooltip("購入をやめるときに押すボタンです。")]
        private Button cancelButton;
        private UiInputScope inputScope;
        private bool isReady;

        /// <summary>
        /// 必須のInspector参照を取得して検証する
        /// </summary>
        private void Awake()
        {
            inputScope = GetComponent<UiInputScope>();
            isReady = iconImage != null && categoryLabel != null && nameLabel != null &&
                descriptionLabel != null && priceValueLabel != null && purchaseButton != null &&
                cancelButton != null && inputScope != null;
            if (!isReady)
            {
                Debug.LogError($"ShopPurchaseDialog の参照が不足しています: {name}", this);
            }
        }

        /// <summary>
        /// 商品情報を表示して購入操作を登録する
        /// </summary>
        /// <param name="arg">購入対象の商品</param>
        public override void OnOpen(object arg)
        {
            if (!isReady)
            {
                Debug.LogError($"購入確認ダイアログのUI参照が不足しています: {name}", this);
                Complete(false);
                return;
            }

            if (!(arg is ShopItemDefinition item))
            {
                Debug.LogError($"購入確認ダイアログの商品データが渡されていません: {name}", this);
                Complete(false);
                return;
            }

            if (item.Icon != null)
            {
                iconImage.sprite = item.Icon;
            }

            iconImage.color = item.Icon == null
                ? ShopPresentationConstants.MissingIconColor
                : Color.white;
            categoryLabel.text = item.Category;
            nameLabel.text = item.DisplayName;
            descriptionLabel.text = item.Description;
            var isSoldOut = GameDataManager.GetItemPurchaseCount(item.Id) >= item.PurchaseLimit;
            priceValueLabel.text = isSoldOut ? "売り切れ" : item.Price.ToString("N0");
            purchaseButton.interactable = !isSoldOut && GameDataManager.Currency >= item.Price;
            purchaseButton.onClick.AddListener(Accept);
            cancelButton.onClick.AddListener(Cancel);
            inputScope.CancelRequested.AddListener(Cancel);
            if (purchaseButton.interactable)
            {
                purchaseButton.Select();
                return;
            }

            cancelButton.Select();
        }

        /// <summary>
        /// 購入操作とキャンセル操作の購読を解除する
        /// </summary>
        public override void OnClose()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(Accept);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
            }

            if (inputScope != null)
            {
                inputScope.CancelRequested.RemoveListener(Cancel);
            }
        }

        /// <summary>
        /// 購入を確定する
        /// </summary>
        private void Accept()
        {
            Complete(true);
        }

        /// <summary>
        /// 購入をキャンセルする
        /// </summary>
        private new void Cancel()
        {
            Complete(false);
        }
    }
}
