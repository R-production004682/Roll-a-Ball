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
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text priceValueLabel;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button cancelButton;
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
                ? new Color(0.78f, 0.8f, 0.84f, 1f)
                : Color.white;
            categoryLabel.text = item.Category;
            nameLabel.text = item.DisplayName;
            descriptionLabel.text = item.Description;
            priceValueLabel.text = item.Price.ToString("N0");
            purchaseButton.interactable = GameDataManager.Currency >= item.Price;
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
