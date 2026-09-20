using System;
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
        private Button itemButton;
        private ShopItemDefinition boundItem;

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
        }

        /// <summary>
        /// バインド済みの商品を押下通知する
        /// </summary>
        private void NotifyClicked()
        {
            if (boundItem != null)
            {
                Clicked?.Invoke(boundItem);
            }
        }
    }
}
