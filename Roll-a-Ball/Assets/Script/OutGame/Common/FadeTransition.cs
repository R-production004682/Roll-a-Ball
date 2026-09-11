using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Covers OutGame screen and scene changes with a black fade
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
        /// Screen または Scene のフェード中かどうかを取得する
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
        /// Stops the active fade when this object is destroyed
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
        /// Fades out, loads a scene, then fades the new scene in
        /// </summary>
        /// <param name="scenePath">Asset path of the scene to load</param>
        /// <param name="loadScene">Scene loading callback invoked after fade out</param>
        /// <param name="onCompleted">Callback invoked after scene loading finishes</param>
        /// <param name="context">Unity Object attached to diagnostic logs</param>
        /// <returns>True when the fade transition starts</returns>
        public bool PlaySceneLoad(string scenePath, Func<AsyncOperation> loadScene, Action<bool> onCompleted, UnityEngine.Object context)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || loadScene == null)
            {
                Debug.LogError("Scene fade transition requires a scene path and loader", context);
                return false;
            }

            if (IsTransitioning || !EnsureVisuals())
            {
                return false;
            }

            if (runningRoutine != null)
            {
                Debug.LogWarning("Scene fade transition is already running", context);
                return false;
            }

            StartRoutine(SceneLoadRoutine(scenePath, loadScene, onCompleted, context));
            return true;
        }

        /// <summary>
        /// Gets the persistent fade UI shared by scene transitions
        /// </summary>
        /// <returns>The persistent fade UI instance</returns>
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
        /// Creates a screen fade UI below the specified parent
        /// </summary>
        /// <param name="parent">Parent Transform for the fade UI</param>
        /// <returns>The created fade UI</returns>
        public static FadeTransition Create(Transform parent)
        {
            if (parent == null)
            {
                Debug.LogError("Screen fade transition requires a parent Transform");
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
        /// Ensures the Canvas, CanvasGroup, Image, and raycaster used by the fade exist
        /// </summary>
        /// <returns>True when the fade UI can be used</returns>
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
                Debug.LogError("Fade transition dependencies could not be created", this);
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
                Debug.LogError("Fade transition Image could not be created", this);
                return false;
            }

            image.color = fadeColor;
            image.raycastTarget = true;

            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogError("Screen fade transition requires a RectTransform", this);
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
        /// Replaces the active fade coroutine with a new one
        /// </summary>
        /// <param name="routine">Fade coroutine to run</param>
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
        /// <param name="routine">Coroutine to run</param>
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
        /// Runs the fade out and fade in sequence
        /// </summary>
        /// <param name="onCovered">Callback invoked while the screen is fully covered</param>
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
                Debug.LogError($"Covered fade callback failed\n{exception}", this);
            }
            yield return FadeAlpha(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Runs a scene load between fade out and fade in
        /// </summary>
        /// <param name="scenePath">Asset path of the scene to load</param>
        /// <param name="loadScene">Scene loading callback</param>
        /// <param name="onCompleted">Callback invoked when loading finishes</param>
        /// <param name="context">Unity Object attached to diagnostic logs</param>
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
                Debug.LogError($"Scene load failed to start: {scenePath}\n{exception}", context);
            }

            if (operation == null)
            {
                Debug.LogError($"Scene loader returned null: {scenePath}", context);
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
                Debug.LogError($"Scene fade completion callback failed: {scenePath}\n{exception}", context);
            }
            yield return FadeAlpha(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Changes CanvasGroup alpha over the configured duration
        /// </summary>
        /// <param name="from">Starting alpha</param>
        /// <param name="to">Ending alpha</param>
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
