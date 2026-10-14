using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>同じ窓の開閉と視点の前後移動を、タイトル開始とゲーム終了で共有する。</summary>
    public sealed class ForestHomeTransition : MonoBehaviour
    {
        private const string ResourcePath = "UI/ForestHomeTransition";
        private static ForestHomeTransition instance;
        [SerializeField] private RectTransform cameraRig;
        [SerializeField] private RectTransform leftShutter;
        [SerializeField] private RectTransform rightShutter;
        [SerializeField] private RectTransform[] branches;
        [SerializeField] private CanvasGroup visuals;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private CanvasGroup blackFade;
        [SerializeField] private GameObject forestBackdrop;
        [SerializeField] private TMP_Text exitLabel;
        [SerializeField] private Vector2 openingCenter;
        [SerializeField] private Vector2 openingSize = new Vector2(986, 714);
        [SerializeField, Min(1f)] private float travelDuration = 3.4f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.8f;
        private IDisposable inputLock;
        private Coroutine routine;
        private Tween transitionTween;
        private float[] branchAngles;
        public CanvasGroup PromptGroup => promptGroup;
        public bool IsAnimating => routine != null;
        public static bool MenuEntered { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState() { instance = null; MenuEntered = false; }

        public static ForestHomeTransition GetOrCreate()
        {
            if (instance != null) return instance;
            var prefab = Resources.Load<ForestHomeTransition>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("ForestHomeTransition の Prefab が Resources/UI にありません。");
                return null;
            }
            instance = Instantiate(prefab);
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }

        private void Awake()
        {
            branchAngles = new float[branches.Length];
            for (var i = 0; i < branches.Length; i++) branchAngles[i] = branches[i].localEulerAngles.z;
            visuals.gameObject.SetActive(false);
            blackFade.alpha = 0f;
            blackFade.blocksRaycasts = false;
            exitLabel.gameObject.SetActive(false);
        }

        public void ShowTitle()
        {
            if (IsAnimating) return;
            visuals.gameObject.SetActive(true);
            visuals.alpha = 1f;
            forestBackdrop.SetActive(true);
            titleGroup.alpha = 1f;
            promptGroup.alpha = 1f;
            promptGroup.transform.localScale = Vector3.one;
            blackFade.alpha = 0f;
            blackFade.blocksRaycasts = false;
            exitLabel.gameObject.SetActive(false);
            ApplyView(0f);
        }

        /// <summary>同じシーンで準備済みのステージ選択へ、窓を開いてつなぐ。</summary>
        public bool EnterStageSelect(TitleScreen title)
        {
            if (IsAnimating || UiInputScope.IsBlocked) return false;
            var manager = GameServices.Screens;
            if (manager == null || !manager.HasPreparedStageSelect(title)) return false;
            inputLock = UiInputScope.BlockInput();
            if (!manager.RevealPreparedStageSelect(title))
            {
                FinishRoutine();
                return false;
            }
            MenuEntered = true;
            routine = StartCoroutine(EnterRoutine());
            return true;
        }

        private IEnumerator EnterRoutine()
        {
            transitionTween = promptGroup.transform.DOScale(1.035f, 0.1f).SetEase(Ease.OutCubic).SetUpdate(true).OnKill(() => transitionTween = null);
            yield return transitionTween.WaitForCompletion();
            yield return FadeGroup(titleGroup, 1f, 0f, 0.35f);
            Canvas.ForceUpdateCanvases();
            forestBackdrop.SetActive(false);
            yield return Travel(false);
            visuals.gameObject.SetActive(false);
            FinishRoutine();
        }

        public bool QuitGame()
        {
            return BeginExit(true);
        }

#if UNITY_EDITOR
        /// <summary>終了せずに、開始と同じ窓へ戻る演出だけを確認する。</summary>
        public bool PreviewExit() { return BeginExit(false); }
#endif

        private bool BeginExit(bool quitApplication)
        {
            if (IsAnimating || UiInputScope.IsBlocked) return false;
            inputLock = UiInputScope.BlockInput();
            OutGameStateController.Enter(GameFlowState.Paused);
            visuals.gameObject.SetActive(true);
            visuals.alpha = 1f;
            forestBackdrop.SetActive(false);
            titleGroup.alpha = 0f;
            exitLabel.gameObject.SetActive(false);
            ApplyView(1f);
            routine = StartCoroutine(ExitRoutine(quitApplication));
            return true;
        }

        private IEnumerator ExitRoutine(bool quitApplication)
        {
            yield return Travel(true);
            // タイトルと同じ構図・同じ閉じた窓まで引いてから黒を重ねる。
            yield return new WaitForSecondsRealtime(0.2f);
            blackFade.blocksRaycasts = true;
            yield return FadeGroup(blackFade, 0f, 1f, fadeDuration);
            if (!quitApplication)
            {
                FinishRoutine();
                yield break;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            exitLabel.text = "ゲームを終了しました\nタブを閉じて終了できます";
            exitLabel.gameObject.SetActive(true);
            yield return null;
#endif
            // 最後の暗転フレームを描画してから終了する。
            yield return new WaitForEndOfFrame();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator Travel(bool closing)
        {
            var progress = closing ? 1f : 0f;
            transitionTween = DOTween.To(() => progress, value => { progress = value; ApplyView(value); },
                closing ? 0f : 1f, travelDuration).SetEase(Ease.InOutSine).SetUpdate(true).OnKill(() => transitionTween = null);
            yield return transitionTween.WaitForCompletion();
            ApplyView(closing ? 0f : 1f);
        }

        private void ApplyView(float progress)
        {
            var canvasRect = (RectTransform)transform;
            var zoom = Mathf.Max(canvasRect.rect.width / openingSize.x, canvasRect.rect.height / openingSize.y) * 1.2f;
            var scale = Mathf.Lerp(1f, zoom, progress);
            cameraRig.localScale = Vector3.one * scale;
            cameraRig.anchoredPosition = -openingCenter * (scale - 1f);
            var doorProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.8f));
            leftShutter.localRotation = Quaternion.Euler(0, -100f * doorProgress, 0);
            rightShutter.localRotation = Quaternion.Euler(0, 100f * doorProgress, 0);
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;
            transitionTween = group.DOFade(to, duration).SetEase(Ease.InOutSine).SetUpdate(true).OnKill(() => transitionTween = null);
            yield return transitionTween.WaitForCompletion();
            group.alpha = to;
        }

        private void Update()
        {
            if (!visuals.gameObject.activeSelf) return;
            for (var i = 0; i < branches.Length; i++)
            {
                var angle = Mathf.Sin(Time.unscaledTime * (0.55f + i * 0.09f) + i * 1.7f) * 1.8f;
                branches[i].localRotation = Quaternion.Euler(0, 0, branchAngles[i] + angle);
            }
        }

        private void FinishRoutine()
        {
            routine = null;
            inputLock?.Dispose();
            inputLock = null;
        }

        private void OnDisable()
        {
            transitionTween?.Kill(); transitionTween = null;
            if (routine != null) StopCoroutine(routine);
            FinishRoutine();
        }

        private void OnDestroy()
        {
            FinishRoutine();
            if (instance == this) instance = null;
        }
    }
}
