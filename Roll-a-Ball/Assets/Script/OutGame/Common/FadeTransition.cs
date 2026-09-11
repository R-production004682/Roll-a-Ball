using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// アウトゲームの画面とシーンの切り替えを黒いフェードで覆う
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class FadeTransition : MonoBehaviour
    {
        private const int OverlaySortingOrder = 32767;
        private static FadeTransition persistentInstance;

        [SerializeField, Min(0.01f)] private float duration = 0.25f;
        [SerializeField] private Color fadeColor = Color.black;

        private CanvasGroup canvasGroup;
        private Coroutine runningRoutine;

        /// <summary>
        /// 画面またはシーンのフェード中かどうかを取得する
        /// </summary>
        public static bool IsTransitioning { get; private set; }

        /// <summary>
        /// フェード UI を初期化し、最初の遷移が始まるまで非表示かつ入力を通す状態にする
        /// 必須コンポーネントを準備できない場合はログを記録して無効化する
        /// </summary>
        private void Awake()
        {
            if (!EnsureVisuals()) enabled = false;
        }

        /// <summary>
        /// プレイ開始時に共有フェード状態を初期化する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            persistentInstance = null;
            IsTransitioning = false;
        }

        /// <summary>
        /// このオブジェクトが破棄されたときに実行中のフェードを停止する
        /// </summary>
        private void OnDestroy()
        {
            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
                runningRoutine = null;
            }

            if (persistentInstance == this)
            {
                persistentInstance = null;
            }
        }

        /// <summary>
        /// 暗転中に表示を切り替え、フェードイン完了後に入力を戻す
        /// </summary>
        /// <param name="onCovered">完全な暗転中に実行する表示切り替え</param>
        /// <param name="onFinished">フェード全体の終了時に実行する後処理</param>
        /// <returns>開始できた場合は true、実行中または UI 準備失敗時は false</returns>
        public bool Play(Action onCovered = null, Action onFinished = null)
        {
            if (IsTransitioning || !EnsureVisuals())
            {
                return false;
            }

            StartRoutine(FadeRoutine(onCovered), onFinished);
            return true;
        }

        /// <summary>
        /// フェードアウトしてシーンを読み込み、読み込んだシーンをフェードインする
        /// </summary>
        /// <param name="scenePath">読み込むシーンのアセットパス</param>
        /// <param name="loadScene">フェードアウト後に呼び出すシーン読み込みコールバック</param>
        /// <param name="onCompleted">シーンの読み込み完了後に呼び出すコールバック</param>
        /// <param name="context">診断ログに関連付ける Unity オブジェクト</param>
        /// <returns>フェード遷移を開始できた場合は true</returns>
        public bool PlaySceneLoad(string scenePath, Func<AsyncOperation> loadScene, Action<bool> onCompleted, UnityEngine.Object context)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || loadScene == null)
            {
                Debug.LogError("シーンのフェード遷移にはシーンパスとローダーが必要です", context);
                return false;
            }

            if (IsTransitioning || !EnsureVisuals())
            {
                return false;
            }

            if (runningRoutine != null)
            {
                Debug.LogWarning("シーンのフェード遷移はすでに実行中です", context);
                return false;
            }

            StartRoutine(SceneLoadRoutine(scenePath, loadScene, onCompleted, context));
            return true;
        }

        /// <summary>
        /// シーン遷移で共有する永続的なフェード UI を取得する
        /// </summary>
        /// <returns>永続的なフェード UI のインスタンス</returns>
        public static FadeTransition GetPersistent()
        {
            if (persistentInstance != null)
            {
                return persistentInstance;
            }

            var fadeObject = new GameObject(
                "SceneFadeTransition",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(GraphicRaycaster)
            );
            DontDestroyOnLoad(fadeObject);
            persistentInstance = fadeObject.AddComponent<FadeTransition>();
            return persistentInstance;
        }

        /// <summary>
        /// 指定した親の下に画面フェード UI を作成する
        /// </summary>
        /// <param name="parent">フェード UI の親 Transform</param>
        /// <returns>作成したフェード UI</returns>
        public static FadeTransition Create(Transform parent)
        {
            if (parent == null)
            {
                Debug.LogError("画面のフェード遷移には親 Transform が必要です");
                return null;
            }

            var fadeObject = new GameObject(
                "ScreenFadeTransition",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(GraphicRaycaster)
            );
            fadeObject.transform.SetParent(parent, false);
            return fadeObject.AddComponent<FadeTransition>();
        }

        /// <summary>
        /// フェードで使用する Canvas、CanvasGroup、Image、レイキャスターを準備する
        /// </summary>
        /// <returns>フェード UI を使用できる場合は true</returns>
        private bool EnsureVisuals()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            if (canvas == null || canvasGroup == null)
            {
                Debug.LogError("フェード遷移の依存コンポーネントを作成できませんでした", this);
                return false;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            if (image == null)
            {
                Debug.LogError("フェード遷移の Image を作成できませんでした", this);
                return false;
            }

            image.color = fadeColor;
            image.raycastTarget = true;

            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogError("画面のフェード遷移には RectTransform が必要です", this);
                return false;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            return true;
        }

        /// <summary>
        /// 実行中のフェードコルーチンを新しいものに置き換える
        /// </summary>
        /// <param name="routine">実行するフェードコルーチン</param>
        /// <param name="onFinished">フェード全体の終了時に実行する後処理</param>
        private void StartRoutine(IEnumerator routine, Action onFinished = null)
        {
            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
            }

            IsTransitioning = true;
            runningRoutine = StartCoroutine(RunRoutine(routine, onFinished));
        }

        /// <summary>
        /// フェード中のナビゲーション入力を止め、終了時に入力と共有状態を復元する
        /// </summary>
        /// <param name="routine">実行するコルーチン</param>
        /// <param name="onFinished">入力と共有状態を復元した後の通知</param>
        private IEnumerator RunRoutine(IEnumerator routine, Action onFinished)
        {
            var eventSystem = EventSystem.current;
            var navigationEnabled = eventSystem != null && eventSystem.sendNavigationEvents;
            try
            {
                if (eventSystem != null) eventSystem.sendNavigationEvents = false;
                yield return routine;
            }
            finally
            {
                if (eventSystem != null) eventSystem.sendNavigationEvents = navigationEnabled;
                if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
                runningRoutine = null;
                IsTransitioning = false;
                onFinished?.Invoke();
            }
        }

        /// <summary>
        /// フェードアウトとフェードインの一連の処理を実行する
        /// </summary>
        /// <param name="onCovered">画面が完全に覆われている間に呼び出すコールバック</param>
        private IEnumerator FadeRoutine(Action onCovered)
        {
            canvasGroup.blocksRaycasts = true;
            yield return FadeAlpha(0f, 1f);
            try
            {
                onCovered?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError($"暗転中のフェードコールバックに失敗しました\n{exception}", this);
            }
            yield return FadeAlpha(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// フェードアウトとフェードインの間にシーンを読み込む
        /// </summary>
        /// <param name="scenePath">読み込むシーンのアセットパス</param>
        /// <param name="loadScene">シーン読み込みコールバック</param>
        /// <param name="onCompleted">読み込み完了時に呼び出すコールバック</param>
        /// <param name="context">診断ログに関連付ける Unity オブジェクト</param>
        private IEnumerator SceneLoadRoutine(string scenePath, Func<AsyncOperation> loadScene, Action<bool> onCompleted, UnityEngine.Object context)
        {
            canvasGroup.blocksRaycasts = true;
            yield return FadeAlpha(0f, 1f);

            AsyncOperation operation = null;
            var succeeded = false;
            try
            {
                operation = loadScene();
            }
            catch (Exception exception)
            {
                Debug.LogError($"シーンの読み込み開始に失敗しました: {scenePath}\n{exception}", context);
            }

            if (operation == null)
            {
                Debug.LogError($"シーンローダーが null を返しました: {scenePath}", context);
            }
            else
            {
                while (!operation.isDone)
                {
                    yield return null;
                }

                succeeded = true;
            }

            try
            {
                onCompleted?.Invoke(succeeded);
            }
            catch (Exception exception)
            {
                Debug.LogError($"シーンのフェード完了コールバックに失敗しました: {scenePath}\n{exception}", context);
            }
            yield return FadeAlpha(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 設定された時間をかけて CanvasGroup のアルファ値を変更する
        /// </summary>
        /// <param name="from">開始時のアルファ値</param>
        /// <param name="to">終了時のアルファ値</param>
        private IEnumerator FadeAlpha(float from, float to)
        {
            var elapsed = 0f;
            var fadeDuration = Mathf.Max(0.01f, duration);
            canvasGroup.alpha = from;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}
