using System.Collections.Generic;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ショップに表示する商品マスターを保持する
    /// </summary>
    [CreateAssetMenu(menuName = "Roll-a-Ball/Shop/Shop Item Catalog")]
    public sealed class ShopItemCatalog : ScriptableObject
    {
        [SerializeField] private ShopItemDefinition[] items = new ShopItemDefinition[0];

        /// <summary>
        /// 登録された商品定義を取得する
        /// </summary>
        public IReadOnlyList<ShopItemDefinition> Items => items;
    }
}
