using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ショップ一覧の一商品分を表示する
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceValueLabel;
        [SerializeField] private TMP_Text stockLabel;
        [SerializeField] private TMP_Text soldOutLabel;
        private Button itemButton;
        private ShopItemDefinition boundItem;
        private UnityEngine.UI.Graphic[] cardGraphics = Array.Empty<UnityEngine.UI.Graphic>();
        private readonly Dictionary<UnityEngine.UI.Graphic, Color> originalGraphicColors =
            new Dictionary<UnityEngine.UI.Graphic, Color>();
        private bool isSoldOut;

        /// <summary>
        /// 商品カードが押下されたときに通知する
        /// </summary>
        public event Action<ShopItemDefinition> Clicked;

        /// <summary>
        /// 商品カードの Button を取得する
        /// </summary>
        private void Awake()
        {
            itemButton = GetComponent<Button>();
            CacheOriginalGraphicColors();
        }

        /// <summary>
        /// 商品カードの押下通知を登録する
        /// </summary>
        private void OnEnable()
        {
            if (itemButton == null)
            {
                itemButton = GetComponent<Button>();
            }

            if (itemButton != null)
            {
                itemButton.onClick.AddListener(NotifyClicked);
            }
        }

        /// <summary>
        /// 商品カードの押下通知を解除する
        /// </summary>
        private void OnDisable()
        {
            if (itemButton != null)
            {
                itemButton.onClick.RemoveListener(NotifyClicked);
            }
        }

        /// <summary>
        /// 商品定義をカードへ反映する
        /// </summary>
        /// <param name="item">表示する商品</param>
        public void Bind(ShopItemDefinition item)
        {
            if (item == null)
            {
                Debug.LogError("ShopItemView に null の商品が渡されました。", this);
                return;
            }

            boundItem = item;
            if (iconImage != null)
            {
                if (item.Icon != null)
                {
                    iconImage.sprite = item.Icon;
                }

                iconImage.color = item.Icon == null
                    ? new Color(0.78f, 0.8f, 0.84f, 1f)
                    : Color.white;
                originalGraphicColors[iconImage] = iconImage.color;
            }

            if (categoryLabel != null)
            {
                categoryLabel.text = item.Category;
            }

            if (nameLabel != null)
            {
                nameLabel.text = item.DisplayName;
            }

            if (priceValueLabel != null)
            {
                priceValueLabel.text = item.Price.ToString("N0");
            }

            var purchaseLimit = item.PurchaseLimit;
            var purchasedCount = GameDataManager.GetItemPurchaseCount(item.Id);
            var remainingCount = Mathf.Max(0, purchaseLimit - purchasedCount);
            if (stockLabel != null)
            {
                stockLabel.text = $"在庫 {remainingCount} / {purchaseLimit}";
            }

            SetSoldOutState(remainingCount == 0);
        }

        /// <summary>
        /// バインド済みの商品を押下通知する
        /// </summary>
        private void NotifyClicked()
        {
            if (boundItem != null && !isSoldOut && itemButton != null && itemButton.interactable)
            {
                Clicked?.Invoke(boundItem);
            }
        }

        /// <summary>
        /// カード内のGraphicごとの初期色を保存する
        /// </summary>
        private void CacheOriginalGraphicColors()
        {
            cardGraphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            originalGraphicColors.Clear();
            for (var index = 0; index < cardGraphics.Length; index++)
            {
                var graphic = cardGraphics[index];
                if (graphic != null)
                {
                    originalGraphicColors[graphic] = graphic.color;
                }
            }
        }

        /// <summary>
        /// 購入済み状態に応じてカードの操作可否、表示文字、色を更新する
        /// </summary>
        /// <param name="soldOut">売り切れとして表示する場合は true</param>
        private void SetSoldOutState(bool soldOut)
        {
            isSoldOut = soldOut;
            if (itemButton != null)
            {
                itemButton.interactable = !soldOut;
            }

            if (soldOutLabel != null)
            {
                soldOutLabel.text = "売り切れ";
                soldOutLabel.gameObject.SetActive(soldOut);
            }

            for (var index = 0; index < cardGraphics.Length; index++)
            {
                var graphic = cardGraphics[index];
                if (graphic != null && originalGraphicColors.TryGetValue(graphic, out var originalColor))
                {
                    graphic.color = soldOut ? ToGrayscale(originalColor) : originalColor;
                }
            }
        }

        /// <summary>
        /// 色の明度を保ったグレースケール色を作る
        /// </summary>
        /// <param name="color">変換元の色</param>
        /// <returns>グレースケールへ変換した色</returns>
        private static Color ToGrayscale(Color color)
        {
            var luminance = color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
            return new Color(luminance, luminance, luminance, color.a);
        }
    }
}
