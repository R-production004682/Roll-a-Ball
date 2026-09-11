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

        private bool CanAnimate => isActiveAndEnabled && hasFocus && target != null &&
            button != null && button.isActiveAndEnabled && button.IsInteractable();
        private bool IsPressed => pressedPointer.HasValue && hoveringPointers.Contains(pressedPointer.Value);

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            target = animationTarget != null ? animationTarget : transform;
            // Recapture after pooling/re-enabling, so an intentional size change is preserved.
            baseScale = target.localScale;
            hoveringPointers.Clear();
            pressedPointer = null;
            isSelected = EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject;
            wasInteractable = CanAnimate;
            button.onClick.AddListener(PlayClickFeedback);
            if (wasInteractable && isSelected && animateSelection) RefreshVisual();
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayClickFeedback);
            ResetVisual();
            isSelected = false;
        }

        private void OnDestroy()
        {
            StopTween();
        }

        private void Update()
        {
            // Button.enabled/interactable and parent CanvasGroups can change without pointer events.
            bool canAnimate = CanAnimate;
            if (canAnimate == wasInteractable) return;

            wasInteractable = canAnimate;
            pressedPointer = null;
            if (canAnimate) RefreshVisual();
            else RestoreScale();
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused) ResetVisual();
            else if (CanAnimate) RefreshVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isActiveAndEnabled) return;
            hoveringPointers.Add(eventData.pointerId);
            if (CanAnimate) RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isActiveAndEnabled) return;
            hoveringPointers.Remove(eventData.pointerId);
            if (CanAnimate) RefreshVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanAnimate ||
                pressedPointer.HasValue) return;

            pressedPointer = eventData.pointerId;
            hoveringPointers.Add(eventData.pointerId);
            RefreshVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // A second touch must not release the finger currently holding the Button.
            if (eventData.button != PointerEventData.InputButton.Left ||
                pressedPointer != eventData.pointerId) return;

            pressedPointer = null;
            if (CanAnimate) RefreshVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            if (CanAnimate) RefreshVisual();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            if (CanAnimate) RefreshVisual();
        }

        /// <summary>
        /// Visual feedback only. Automatically called by Button.onClick, including Submit.
        /// Does not invoke, delay or replace the Button's action.
        /// </summary>
        public void PlayClickFeedback()
        {
            // An earlier click listener may close a dialog or destroy/disable the Button.
            if (this == null || !CanAnimate) return;

            StopTween();
            Sequence sequence = DOTween.Sequence();
            // Also provides a press phase for keyboard/gamepad Submit and very fast taps.
            sequence.Append(target.DOScale(GetPressedScale(), pressDuration * 0.5f).SetEase(pressEase));
            sequence.Append(target.DOScale(ScaleXY(clickOvershootScale), releaseDuration * 0.55f).SetEase(Ease.OutCubic));
            sequence.Append(target.DOScale(GetRestScale(), releaseDuration).SetEase(releaseEase));
            TrackTween(sequence);
        }

        private void RefreshVisual()
        {
            if (IsPressed) AnimateTo(GetPressedScale(), pressDuration, pressEase);
            else AnimateTo(GetRestScale(), hoverDuration, hoverEase);
        }

        private Vector3 GetPressedScale()
        {
            return Vector3.Scale(baseScale, new Vector3(pressedScale.x, pressedScale.y, 1f));
        }

        private Vector3 GetRestScale()
        {
            return ScaleXY(hoveringPointers.Count > 0 || (animateSelection && isSelected) ? hoverScale : 1f);
        }

        private Vector3 ScaleXY(float multiplier)
        {
            return Vector3.Scale(baseScale, new Vector3(multiplier, multiplier, 1f));
        }

        private void AnimateTo(Vector3 scale, float duration, Ease ease)
        {
            StopTween();
            if (target == null) return;
            TrackTween(target.DOScale(scale, duration).SetEase(ease));
        }

        private void TrackTween(Tween tween)
        {
            scaleTween = tween
                .SetUpdate(useUnscaledTime)
                .SetTarget(this)
                .SetAutoKill(true)
                // DOTween can recycle completed tweens: never retain a reference to one.
                .OnKill(() => scaleTween = null);
        }

        private void StopTween()
        {
            if (scaleTween == null) return;
            scaleTween.Kill();
            scaleTween = null;
        }

        private void RestoreScale()
        {
            StopTween();
            if (target != null) target.localScale = baseScale;
        }

        private void ResetVisual()
        {
            hoveringPointers.Clear();
            pressedPointer = null;
            RestoreScale();
        }

#if UNITY_EDITOR
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
