using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// StageSelectScreen と入れ替えて表示するショップ画面
    /// </summary>
    public sealed class ShopScreen : ScreenBase
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button customizeButton;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text currencyValueLabel;
        [SerializeField] private TMP_Text pageIndicatorLabel;
        [SerializeField] private RectTransform itemGrid;
        [SerializeField] private ShopItemView itemViewPrefab;
        [SerializeField] private ShopItemCatalog itemCatalog;
        [SerializeField, Min(1)] private int columnCount = 5;
        [SerializeField, Min(1)] private int itemsPerPage = 10;
        [SerializeField] private string pageIndicatorFormat = "{0} / {1}";
        private UiInputScope inputScope;
        private bool returnQueued;
        private bool purchaseQueued;
        private readonly List<ShopItemView> itemViews = new List<ShopItemView>();
        private readonly List<ShopItemDefinition> orderedItems = new List<ShopItemDefinition>();
        private int currentPage;
        private int pageCount = 1;

        /// <summary>
        /// 共通入力範囲を取得する
        /// </summary>
        private void Awake()
        {
            inputScope = GetComponent<UiInputScope>();
        }

        /// <summary>
        /// ショップ画面を表示し、戻る操作を登録する
        /// </summary>
        public override void OnOpen(object arg)
        {
            returnQueued = false;
            purchaseQueued = false;
            if (backButton == null || customizeButton == null || inputScope == null)
            {
                Debug.LogError("ShopScreen の下部ボタンまたは UiInputScope が設定されていません。", this);
                return;
            }

            if (previousPageButton == null || nextPageButton == null ||
                currencyValueLabel == null || pageIndicatorLabel == null ||
                itemGrid == null || itemViewPrefab == null || itemCatalog == null)
            {
                Debug.LogError("ShopScreen のヘッダー、ページボタン、商品表示の参照が設定されていません。", this);
                return;
            }

            backButton.onClick.AddListener(ReturnToStages);
            customizeButton.onClick.AddListener(OpenCustomization);
            previousPageButton.onClick.AddListener(ShowPreviousPage);
            nextPageButton.onClick.AddListener(ShowNextPage);
            inputScope.CancelRequested.AddListener(ReturnToStages);
            GameDataManager.Changed += RefreshCurrency;
            RefreshItems();
            RefreshCurrency();
            backButton.Select();
        }

        /// <summary>
        /// ショップ画面を閉じ、戻る操作の購読を解除する
        /// </summary>
        public override void OnClose()
        {
            returnQueued = false;
            purchaseQueued = false;
            StopAllCoroutines();
            if (inputScope != null)
            {
                inputScope.CancelRequested.RemoveListener(ReturnToStages);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ReturnToStages);
            }

            if (customizeButton != null)
            {
                customizeButton.onClick.RemoveListener(OpenCustomization);
            }

            if (previousPageButton != null)
            {
                previousPageButton.onClick.RemoveListener(ShowPreviousPage);
            }

            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveListener(ShowNextPage);
            }

            GameDataManager.Changed -= RefreshCurrency;

            ClearItemViews();
        }

        /// <summary>
        /// 商品カタログの商品を表示順に生成する
        /// </summary>
        private void RefreshItems()
        {
            var gridLayout = itemGrid.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                Debug.LogError("商品グリッドに GridLayoutGroup が設定されていません。", itemGrid);
                return;
            }

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = Mathf.Max(1, columnCount);

            var items = itemCatalog.Items;
            if (items == null)
            {
                Debug.LogError("ShopItemCatalog の商品配列が設定されていません。", itemCatalog);
                return;
            }

            orderedItems.Clear();
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null)
                {
                    Debug.LogWarning($"ShopItemCatalog の {index} 番目の商品が null です。", itemCatalog);
                    continue;
                }

                orderedItems.Add(item);
            }

            orderedItems.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
            pageCount = Mathf.Max(1, Mathf.CeilToInt(orderedItems.Count / (float)Mathf.Max(1, itemsPerPage)));
            currentPage = 0;
            RefreshPage();
        }

        /// <summary>
        /// 現在のページの商品カードを生成する
        /// </summary>
        private void RefreshPage()
        {
            ClearItemViews();
            var pageSize = Mathf.Max(1, itemsPerPage);
            var firstItemIndex = currentPage * pageSize;
            var lastItemIndex = Mathf.Min(firstItemIndex + pageSize, orderedItems.Count);
            for (var index = firstItemIndex; index < lastItemIndex; index++)
            {
                var item = orderedItems[index];
                var itemView = Instantiate(itemViewPrefab, itemGrid);
                itemView.Bind(item);
                itemView.Clicked += HandleItemClicked;
                itemViews.Add(itemView);
            }

            RefreshPageControls();
        }

        /// <summary>
        /// ページ番号とページ移動ボタンを更新する
        /// </summary>
        private void RefreshPageControls()
        {
            if (pageIndicatorLabel != null)
            {
                pageIndicatorLabel.text = string.Format(pageIndicatorFormat, currentPage + 1, pageCount);
            }

            if (previousPageButton != null)
            {
                previousPageButton.interactable = currentPage > 0;
            }

            if (nextPageButton != null)
            {
                nextPageButton.interactable = currentPage < pageCount - 1;
            }
        }

        /// <summary>
        /// 所持金表示を更新する
        /// </summary>
        private void RefreshCurrency()
        {
            if (currencyValueLabel != null)
            {
                currencyValueLabel.text = GameDataManager.Currency.ToString("N0");
            }
        }

        /// <summary>
        /// 前のショップページへ移動する
        /// </summary>
        private void ShowPreviousPage()
        {
            if (currentPage <= 0)
            {
                return;
            }

            currentPage--;
            RefreshPage();
        }

        /// <summary>
        /// 次のショップページへ移動する
        /// </summary>
        private void ShowNextPage()
        {
            if (currentPage >= pageCount - 1)
            {
                return;
            }

            currentPage++;
            RefreshPage();
        }

        /// <summary>
        /// カスタマイズ画面へ同一シーン内で移動する
        /// </summary>
        private void OpenCustomization()
        {
            if (inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            if (GameServices.Screens != null)
            {
                GameServices.Screens.Replace<CustomizationScreen>(CustomizationScreen.ReturnDestination.Shop);
                return;
            }

            Debug.LogError("ScreenManager がないため CustomizationScreen に移動できません。", this);
        }

        /// <summary>
        /// 生成済みの商品カードを削除する
        /// </summary>
        private void ClearItemViews()
        {
            for (var index = itemViews.Count - 1; index >= 0; index--)
            {
                if (itemViews[index] != null)
                {
                    itemViews[index].Clicked -= HandleItemClicked;
                    Destroy(itemViews[index].gameObject);
                }
            }

            itemViews.Clear();
        }

        /// <summary>
        /// 商品カードから購入確認を開始する
        /// </summary>
        /// <param name="item">押下された商品</param>
        private void HandleItemClicked(ShopItemDefinition item)
        {
            if (item == null || purchaseQueued || inputScope == null || !inputScope.CanReceiveInput ||
                GameDataManager.GetItemPurchaseCount(item.Id) >= item.PurchaseLimit)
            {
                return;
            }

            purchaseQueued = true;
            StartCoroutine(PurchaseItem(item));
        }

        /// <summary>
        /// 購入確認と購入保存を順番に実行する
        /// </summary>
        /// <param name="item">購入対象の商品</param>
        /// <returns>購入処理が完了するまで待機する Coroutine</returns>
        private IEnumerator PurchaseItem(ShopItemDefinition item)
        {
            var screenManager = GameServices.Screens;
            if (screenManager == null)
            {
                Debug.LogError("ScreenManager がないため購入確認を表示できません。", this);
                purchaseQueued = false;
                yield break;
            }

            var confirmation = screenManager.ShowDialogAsync<ShopPurchaseDialog, bool>(item);
            while (!confirmation.IsCompleted)
            {
                yield return null;
            }

            if (confirmation.IsCanceled || confirmation.IsFaulted || !confirmation.Result)
            {
                if (confirmation.IsFaulted)
                {
                    Debug.LogError($"購入確認ダイアログでエラーが発生しました: {confirmation.Exception}", this);
                }

                purchaseQueued = false;
                yield break;
            }

            if (GameDataManager.Currency < item.Price)
            {
                purchaseQueued = false;
                yield break;
            }

            if (GameDataManager.GetItemPurchaseCount(item.Id) >= item.PurchaseLimit)
            {
                RefreshPage();
                purchaseQueued = false;
                yield break;
            }

            int currencyAfterPurchase;
            if (!GameDataManager.TryPurchaseItem(
                    item.Id,
                    item.Price,
                    item.PurchaseLimit,
                    out currencyAfterPurchase))
            {
                Debug.LogWarning($"商品を購入できませんでした: {item.Id}", this);
                purchaseQueued = false;
                yield break;
            }

            RefreshPage();
            purchaseQueued = false;
        }

        /// <summary>
        /// StageSelectScreen へ同一シーン内で戻る
        /// </summary>
        private void ReturnToStages()
        {
            if (returnQueued || inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            returnQueued = true;
            StartCoroutine(ReturnToStagesAfterInputFrame());
        }

        /// <summary>
        /// Button の入力フレーム終了後に Screen を入れ替え、破棄中の UI と生成中の UI を分離する
        /// </summary>
        /// <returns>一フレーム待機する Coroutine</returns>
        private IEnumerator ReturnToStagesAfterInputFrame()
        {
            yield return null;
            if (GameServices.Screens != null)
            {
                GameServices.Screens.Replace<StageSelectScreen>();
                yield break;
            }

            Debug.LogError("ScreenManager がないため StageSelectScreen に戻れません。", this);
            returnQueued = false;
        }
    }
}
