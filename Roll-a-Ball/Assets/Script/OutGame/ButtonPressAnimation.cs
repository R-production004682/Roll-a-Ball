using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Reusable uGUI Button feedback. Attach to a Button or its prefab.
    /// Only owns scale animation; Button retains responsibility for click actions and color transitions.
    /// </summary>
    [AddComponentMenu("UI/Effects/Button Press Animation")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressAnimation : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField, Tooltip("Optional visual child. If omitted, animates this Button. Use a visual child to keep the hit area stationary.")]
        private Transform animationTarget;

        [Header("Scale (relative to the original size)")]
        [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1.025f;
        [SerializeField] private Vector2 pressedScale = new Vector2(0.96f, 0.9f);
        [SerializeField, Range(1f, 1.15f)] private float clickOvershootScale = 1.055f;
        [SerializeField] private bool animateSelection = true;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.075f;
        [SerializeField, Min(0.01f)] private float releaseDuration = 0.16f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Easing")]
        [SerializeField] private Ease hoverEase = Ease.OutQuad;
        [SerializeField] private Ease pressEase = Ease.OutCubic;
        [SerializeField] private Ease releaseEase = Ease.OutCubic;

        private readonly HashSet<int> hoveringPointers = new HashSet<int>();
        private Button button;
        private Transform target;
        private Tween scaleTween;
        private Vector3 baseScale;
        private int? pressedPointer;
        private bool isSelected;
        private bool wasInteractable;
        private bool hasFocus = true;

        /// <summary>
        /// 現在の Button が演出を再生できる状態か
        /// </summary>
        private bool CanAnimate => isActiveAndEnabled && hasFocus && target != null &&
            button != null && button.isActiveAndEnabled && button.IsInteractable();

        /// <summary>
        /// ポインターで押下中か
        /// </summary>
        private bool IsPressed => pressedPointer.HasValue && hoveringPointers.Contains(pressedPointer.Value);

        /// <summary>
        /// 同じ GameObject の Button を取得する
        /// </summary>
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        /// <summary>
        /// 演出対象と Button 操作を初期化する
        /// </summary>
        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            target = animationTarget != null ? animationTarget : transform;
            // Recapture after pooling/re-enabling, so an intentional size change is preserved.
            baseScale = target.localScale;
            hoveringPointers.Clear();
            pressedPointer = null;
            isSelected = EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject;
            wasInteractable = CanAnimate;
            button.onClick.AddListener(PlayClickFeedback);
            if (wasInteractable && isSelected && animateSelection)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// Button 操作の購読を解除して表示を元に戻す
        /// </summary>
        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickFeedback);
            }

            ResetVisual();
            isSelected = false;
        }

        /// <summary>
        /// 破棄時に再生中の Tween を停止する
        /// </summary>
        private void OnDestroy()
        {
            StopTween();
        }

        /// <summary>
        /// Button の有効状態の変更を監視して演出を更新する
        /// </summary>
        private void Update()
        {
            // Button.enabled/interactable and parent CanvasGroups can change without pointer events.
            var canAnimate = CanAnimate;
            if (canAnimate == wasInteractable)
            {
                return;
            }

            wasInteractable = canAnimate;
            pressedPointer = null;
            if (canAnimate)
            {
                RefreshVisual();
            }
            else
            {
                RestoreScale();
            }
        }

        /// <summary>
        /// アプリケーションのフォーカスに応じて演出を更新する
        /// </summary>
        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused)
            {
                ResetVisual();
            }
            else if (CanAnimate)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// ポインターが Button に入ったときの演出を更新する
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            hoveringPointers.Add(eventData.pointerId);
            if (CanAnimate)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// ポインターが Button から出たときの演出を更新する
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            hoveringPointers.Remove(eventData.pointerId);
            if (CanAnimate)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// 左クリックまたはタップ開始時の押下状態を記録する
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanAnimate ||
                pressedPointer.HasValue)
            {
                return;
            }

            pressedPointer = eventData.pointerId;
            hoveringPointers.Add(eventData.pointerId);
            RefreshVisual();
        }

        /// <summary>
        /// 押下中の左クリックまたはタップが離れたときの状態を更新する
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            // A second touch must not release the finger currently holding the Button.
            if (eventData.button != PointerEventData.InputButton.Left ||
                pressedPointer != eventData.pointerId)
            {
                return;
            }

            pressedPointer = null;
            if (CanAnimate)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// Button が選択されたときの演出を更新する
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            if (CanAnimate)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// Button の選択が外れたときの演出を更新する
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            if (CanAnimate)
            {
                RefreshVisual();
            }
        }

        /// <summary>
        /// Visual feedback only. Automatically called by Button.onClick, including Submit.
        /// Does not invoke, delay or replace the Button's action.
        /// </summary>
        public void PlayClickFeedback()
        {
            // An earlier click listener may close a dialog or destroy/disable the Button.
            if (this == null || !CanAnimate)
            {
                return;
            }

            StopTween();
            var sequence = DOTween.Sequence();
            // Also provides a press phase for keyboard/gamepad Submit and very fast taps.
            sequence.Append(target.DOScale(GetPressedScale(), pressDuration * 0.5f).SetEase(pressEase));
            sequence.Append(target.DOScale(ScaleXY(clickOvershootScale), releaseDuration * 0.55f).SetEase(Ease.OutCubic));
            sequence.Append(target.DOScale(GetRestScale(), releaseDuration).SetEase(releaseEase));
            TrackTween(sequence);
        }

        /// <summary>
        /// ポインター・選択・押下状態に応じた目標スケールへ遷移する
        /// </summary>
        private void RefreshVisual()
        {
            if (IsPressed)
            {
                AnimateTo(GetPressedScale(), pressDuration, pressEase);
            }
            else
            {
                AnimateTo(GetRestScale(), hoverDuration, hoverEase);
            }
        }

        /// <summary>
        /// 押下時のスケールを返す
        /// </summary>
        /// <returns>基準スケールへ押下倍率を適用した値</returns>
        private Vector3 GetPressedScale()
        {
            return Vector3.Scale(baseScale, new Vector3(pressedScale.x, pressedScale.y, 1f));
        }

        /// <summary>
        /// ポインターと選択状態に応じた通常時のスケールを返す
        /// </summary>
        /// <returns>現在の通常スケール</returns>
        private Vector3 GetRestScale()
        {
            return ScaleXY(hoveringPointers.Count > 0 || (animateSelection && isSelected) ? hoverScale : 1f);
        }

        /// <summary>
        /// X と Y に同じ倍率を適用したスケールを返す
        /// </summary>
        /// <param name="multiplier">適用する倍率</param>
        /// <returns>倍率を適用したスケール</returns>
        private Vector3 ScaleXY(float multiplier)
        {
            return Vector3.Scale(baseScale, new Vector3(multiplier, multiplier, 1f));
        }

        /// <summary>
        /// 指定スケールへ Tween で遷移する
        /// </summary>
        /// <param name="scale">遷移先のスケール</param>
        /// <param name="duration">遷移時間</param>
        /// <param name="ease">遷移イージング</param>
        private void AnimateTo(Vector3 scale, float duration, Ease ease)
        {
            StopTween();
            if (target == null)
            {
                return;
            }

            TrackTween(target.DOScale(scale, duration).SetEase(ease));
        }

        /// <summary>
        /// Tween を管理対象へ登録する
        /// </summary>
        /// <param name="tween">登録する Tween</param>
        private void TrackTween(Tween tween)
        {
            scaleTween = tween
                .SetUpdate(useUnscaledTime)
                .SetTarget(this)
                .SetAutoKill(true)
                // DOTween can recycle completed tweens: never retain a reference to one.
                .OnKill(() => scaleTween = null);
        }

        /// <summary>
        /// 実行中の Tween を停止する
        /// </summary>
        private void StopTween()
        {
            if (scaleTween == null)
            {
                return;
            }

            scaleTween.Kill();
            scaleTween = null;
        }

        /// <summary>
        /// 演出対象のスケールを基準値へ戻す
        /// </summary>
        private void RestoreScale()
        {
            StopTween();
            if (target != null)
            {
                target.localScale = baseScale;
            }
        }

        /// <summary>
        /// ポインターと押下状態を破棄して表示を初期化する
        /// </summary>
        private void ResetVisual()
        {
            hoveringPointers.Clear();
            pressedPointer = null;
            RestoreScale();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector で設定できる演出パラメーターを有効範囲へ制限する
        /// </summary>
        private void OnValidate()
        {
            hoverScale = Mathf.Clamp(hoverScale, 1f, 1.1f);
            pressedScale.x = Mathf.Clamp(pressedScale.x, 0.01f, 1f);
            pressedScale.y = Mathf.Clamp(pressedScale.y, 0.01f, 1f);
            clickOvershootScale = Mathf.Clamp(clickOvershootScale, hoverScale, 1.15f);
            hoverDuration = Mathf.Max(0.01f, hoverDuration);
            pressDuration = Mathf.Max(0.01f, pressDuration);
            releaseDuration = Mathf.Max(0.01f, releaseDuration);
        }
#endif
    }
}
