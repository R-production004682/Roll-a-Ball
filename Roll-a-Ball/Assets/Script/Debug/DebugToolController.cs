#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Roll_a_Ball.OutGame;

namespace Roll_a_Ball.DebugTools
{
    /// <summary>
    /// F12 で開閉する全Scene共通の開発用デバッグパネルを管理する
    /// </summary>
    internal sealed class DebugToolController : MonoBehaviour
    {
        private const int CanvasSortingOrder = 30000;
        private const int DebugButtonColumnCount = 4;

        private GameObject panelObject;
        private UiInputScope inputScope;
        private Button cheatUserButton;
        private Button copyJsonButton;
        private Button deleteUserDataButton;
        private Button confirmDeleteUserDataButton;
        private Button cancelDeleteUserDataButton;
        private TMP_Text statusLabel;

        /// <summary>
        /// 永続ルートへUIを構築し、Scene切り替え通知を購読する
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildUi();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        /// <summary>
        /// Scene切り替え通知の購読を解除する
        /// </summary>
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// F12入力を監視する
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                TogglePanel();
            }
        }

        /// <summary>
        /// Scene読み込み後にUI入力環境を確認し、開いていれば選択を戻す
        /// </summary>
        /// <param name="scene">読み込まれたScene</param>
        /// <param name="mode">Sceneの読み込みモード</param>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystem();
            if (panelObject != null && panelObject.activeSelf && cheatUserButton != null)
            {
                SelectActiveButton();
            }
        }

        /// <summary>
        /// デバッグパネルの表示状態を切り替える
        /// </summary>
        private void TogglePanel()
        {
            if (panelObject == null)
            {
                return;
            }

            var shouldOpen = !panelObject.activeSelf;
            if (shouldOpen)
            {
                EnsureEventSystem();
            }
            else
            {
                HideDeleteConfirmation();
            }

            panelObject.SetActive(shouldOpen);
            if (shouldOpen)
            {
                SelectActiveButton();
            }
        }

        /// <summary>
        /// チートユーザー作成を実行し、結果をパネルとConsoleへ表示する
        /// </summary>
        private void CreateCheatUser()
        {
            if (inputScope == null || !inputScope.CanReceiveInput || cheatUserButton == null)
            {
                return;
            }

            cheatUserButton.interactable = false;
            var succeeded = DebugCheatUserService.TryCreate(out var message);
            SetStatus(message, succeeded);

            cheatUserButton.interactable = true;
            cheatUserButton.Select();
        }

        /// <summary>
        /// PlayerPrefs に保存されたゲームデータ JSON をクリップボードへコピーする
        /// </summary>
        private void CopySavedJson()
        {
            if (inputScope == null || !inputScope.CanReceiveInput || copyJsonButton == null)
            {
                return;
            }

            copyJsonButton.interactable = false;
            var succeeded = DebugGameDataJsonService.TryCopyToClipboard(out var message);
            SetStatus(message, succeeded);
            copyJsonButton.interactable = true;
            copyJsonButton.Select();
        }

        /// <summary>
        /// ユーザーデータ削除の確認操作を表示する
        /// </summary>
        private void RequestDeleteUserData()
        {
            if (inputScope == null || !inputScope.CanReceiveInput || deleteUserDataButton == null ||
                confirmDeleteUserDataButton == null || cancelDeleteUserDataButton == null)
            {
                return;
            }

            deleteUserDataButton.interactable = false;
            confirmDeleteUserDataButton.gameObject.SetActive(true);
            cancelDeleteUserDataButton.gameObject.SetActive(true);
            SetInfoStatus("ユーザーデータを初期状態へ戻します。実行する場合は「削除を確定」を押してください。");
            confirmDeleteUserDataButton.Select();
        }

        /// <summary>
        /// 確認済みのユーザーデータ削除を実行する
        /// </summary>
        private void ConfirmDeleteUserData()
        {
            if (inputScope == null || !inputScope.CanReceiveInput || confirmDeleteUserDataButton == null ||
                cancelDeleteUserDataButton == null)
            {
                return;
            }

            confirmDeleteUserDataButton.interactable = false;
            cancelDeleteUserDataButton.interactable = false;
            var succeeded = DebugUserDataResetService.TryReset(out var message);
            HideDeleteConfirmation();
            SetStatus(message, succeeded);
            SelectActiveButton();
        }

        /// <summary>
        /// ユーザーデータ削除の確認操作をキャンセルする
        /// </summary>
        private void CancelDeleteUserData()
        {
            if (inputScope == null || !inputScope.CanReceiveInput || cancelDeleteUserDataButton == null)
            {
                return;
            }

            HideDeleteConfirmation();
            SetInfoStatus("ユーザーデータ削除をキャンセルしました。データは変更されていません。");
            SelectActiveButton();
        }

        /// <summary>
        /// デバッグメニューのステータス表示を更新する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="succeeded">成功表示にする場合は true</param>
        private void SetStatus(string message, bool succeeded)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
            statusLabel.color = succeeded
                ? new Color(0.55f, 1f, 0.7f)
                : new Color(1f, 0.55f, 0.55f);
        }

        /// <summary>
        /// デバッグメニューの情報表示を更新する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        private void SetInfoStatus(string message)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
            statusLabel.color = new Color(0.68f, 0.76f, 0.86f);
        }

        /// <summary>
        /// ユーザーデータ削除の確認操作を非表示に戻す
        /// </summary>
        private void HideDeleteConfirmation()
        {
            if (deleteUserDataButton != null)
            {
                deleteUserDataButton.interactable = true;
            }

            if (confirmDeleteUserDataButton != null)
            {
                confirmDeleteUserDataButton.interactable = true;
                confirmDeleteUserDataButton.gameObject.SetActive(false);
            }

            if (cancelDeleteUserDataButton != null)
            {
                cancelDeleteUserDataButton.interactable = true;
                cancelDeleteUserDataButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 確認操作中かどうかに応じてデバッグボタンを選択する
        /// </summary>
        private void SelectActiveButton()
        {
            if (confirmDeleteUserDataButton != null && confirmDeleteUserDataButton.gameObject.activeSelf)
            {
                confirmDeleteUserDataButton.Select();
                return;
            }

            if (cheatUserButton != null)
            {
                cheatUserButton.Select();
            }
        }

        /// <summary>
        /// 開発用オーバーレイのCanvasとデバッグボタンを構築する
        /// </summary>
        private void BuildUi()
        {
            var canvasObject = new GameObject(
                "DebugCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panelObject = new GameObject(
                "DebugPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(UiInputScope));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            SetFullScreenRect(panelRect);

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.04f, 0.07f, 0.12f, 0.98f);
            inputScope = panelObject.GetComponent<UiInputScope>();

            CreateText(
                "Title",
                panelObject.transform,
                "DEBUG TOOLS  [F12で閉じる]",
                28f,
                Color.white,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -48f),
                new Vector2(620f, 56f));
            CreateText(
                "Description",
                panelObject.transform,
                "ローカルユーザー用のデバッグツール",
                20f,
                new Color(0.78f, 0.84f, 0.92f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -112f),
                new Vector2(620f, 56f));

            var buttonGrid = CreateButtonGrid(panelObject.transform);
            cheatUserButton = CreateButton(
                "CreateCheatUserButton",
                buttonGrid,
                "チートユーザーを作成",
                Vector2.zero,
                new Vector2(360f, 72f));
            cheatUserButton.onClick.AddListener(CreateCheatUser);

            copyJsonButton = CreateButton(
                "CopySavedJsonButton",
                buttonGrid,
                "保存 JSON をコピー",
                Vector2.zero,
                new Vector2(360f, 72f));
            copyJsonButton.onClick.AddListener(CopySavedJson);

            deleteUserDataButton = CreateButton(
                "DeleteUserDataButton",
                buttonGrid,
                "ユーザーデータを削除",
                Vector2.zero,
                new Vector2(360f, 72f));
            deleteUserDataButton.onClick.AddListener(RequestDeleteUserData);

            confirmDeleteUserDataButton = CreateButton(
                "ConfirmDeleteUserDataButton",
                buttonGrid,
                "削除を確定",
                Vector2.zero,
                new Vector2(360f, 72f));
            confirmDeleteUserDataButton.onClick.AddListener(ConfirmDeleteUserData);

            cancelDeleteUserDataButton = CreateButton(
                "CancelDeleteUserDataButton",
                buttonGrid,
                "削除をキャンセル",
                Vector2.zero,
                new Vector2(360f, 72f));
            cancelDeleteUserDataButton.onClick.AddListener(CancelDeleteUserData);
            HideDeleteConfirmation();

            statusLabel = CreateText(
                "Status",
                panelObject.transform,
                "準備完了",
                18f,
                new Color(0.68f, 0.76f, 0.86f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 96f),
                new Vector2(620f, 80f));

            panelObject.SetActive(false);
        }

        /// <summary>
        /// デバッグボタンを4列で並べるGridLayoutGroupを生成する
        /// </summary>
        /// <param name="parent">グリッドの親Transform</param>
        /// <returns>生成したグリッドのTransform</returns>
        private static Transform CreateButtonGrid(Transform parent)
        {
            var gridObject = new GameObject(
                "DebugButtonGrid",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            gridObject.transform.SetParent(parent, false);

            var rectTransform = gridObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -10f);
            rectTransform.sizeDelta = new Vector2(1600f, 0f);

            var gridLayout = gridObject.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(360f, 72f);
            gridLayout.spacing = new Vector2(24f, 20f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = DebugButtonColumnCount;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            var contentSizeFitter = gridObject.GetComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return gridObject.transform;
        }

        /// <summary>
        /// 親Canvas全体へ広がるRectTransformを設定する
        /// </summary>
        /// <param name="rectTransform">設定対象</param>
        private static void SetFullScreenRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 指定範囲へTextMeshProテキストを生成する
        /// </summary>
        /// <param name="objectName">生成するGameObject名</param>
        /// <param name="parent">親Transform</param>
        /// <param name="text">表示文字列</param>
        /// <param name="fontSize">文字サイズ</param>
        /// <param name="color">文字色</param>
        /// <param name="anchorMin">アンカー最小値</param>
        /// <param name="anchorMax">アンカー最大値</param>
        /// <param name="position">親からの位置</param>
        /// <param name="size">表示サイズ</param>
        /// <returns>生成したTextMeshProテキスト</returns>
        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// 指定位置へTextMeshProラベル付きButtonを生成する
        /// </summary>
        /// <param name="objectName">生成するGameObject名</param>
        /// <param name="parent">親Transform</param>
        /// <param name="labelText">ボタン表示文字列</param>
        /// <param name="position">親からの位置</param>
        /// <param name="size">表示サイズ</param>
        /// <returns>生成したButton</returns>
        private static Button CreateButton(
            string objectName,
            Transform parent,
            string labelText,
            Vector2 position,
            Vector2 size)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.42f, 0.78f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            CreateText(
                "Label",
                buttonObject.transform,
                labelText,
                22f,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            return button;
        }

        /// <summary>
        /// EventSystemがないSceneへ開発用の入力基盤を追加する
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject(
                "DebugEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<EventSystem>().sendNavigationEvents = true;
        }
    }
}
#endif
