using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ショップに表示する商品の最小表示定義
    /// </summary>
    [Serializable]
    public sealed class ShopItemDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private int displayOrder;
        [SerializeField] private string category;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private int price;
        [SerializeField, Min(1)] private int purchaseLimit = 1;
        [SerializeField] private Sprite icon;

        /// <summary>
        /// 商品を識別する安定 ID を取得する
        /// </summary>
        public string Id => id;

        /// <summary>
        /// 商品の表示順を取得する
        /// </summary>
        public int DisplayOrder => displayOrder;

        /// <summary>
        /// 商品カテゴリを取得する
        /// </summary>
        public string Category => category;

        /// <summary>
        /// 商品名を取得する
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 商品説明を取得する
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 商品価格を取得する
        /// </summary>
        public int Price => price;

        /// <summary>
        /// 商品ごとの購入可能数を取得する
        /// </summary>
        public int PurchaseLimit => Mathf.Max(1, purchaseLimit);

        /// <summary>
        /// 商品アイコンを取得する
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 商品表示に必要な値を初期化する
        /// </summary>
        /// <param name="itemId">商品を識別する安定 ID</param>
        /// <param name="order">商品の表示順</param>
        /// <param name="itemCategory">商品カテゴリ</param>
        /// <param name="name">商品名</param>
        /// <param name="itemDescription">商品説明</param>
        /// <param name="itemPrice">商品価格</param>
        /// <param name="itemIcon">商品アイコン</param>
        public ShopItemDefinition(
            string itemId,
            int order,
            string itemCategory,
            string name,
            string itemDescription,
            int itemPrice,
            Sprite itemIcon)
            : this(itemId, order, itemCategory, name, itemDescription, itemPrice, 1, itemIcon)
        {
        }

        /// <summary>
        /// 商品表示に必要な値と購入可能数を初期化する
        /// </summary>
        /// <param name="itemId">商品を識別する安定 ID</param>
        /// <param name="order">商品の表示順</param>
        /// <param name="itemCategory">商品カテゴリ</param>
        /// <param name="name">商品名</param>
        /// <param name="itemDescription">商品説明</param>
        /// <param name="itemPrice">商品価格</param>
        /// <param name="itemPurchaseLimit">商品ごとの購入可能数</param>
        /// <param name="itemIcon">商品アイコン</param>
        public ShopItemDefinition(
            string itemId,
            int order,
            string itemCategory,
            string name,
            string itemDescription,
            int itemPrice,
            int itemPurchaseLimit,
            Sprite itemIcon)
        {
            id = itemId;
            displayOrder = order;
            category = itemCategory;
            displayName = name;
            description = itemDescription;
            price = itemPrice;
            purchaseLimit = Mathf.Max(1, itemPurchaseLimit);
            icon = itemIcon;
        }
    }
}
