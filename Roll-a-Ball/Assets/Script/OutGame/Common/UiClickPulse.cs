using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>クリックと決定キーで共用する、短い淡金色の輪。最大6個を再利用する。</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiClickPulse : MaskableGraphic
    {
        private static RectTransform overlay;
        private static readonly List<UiClickPulse> pool = new List<UiClickPulse>();
        private Tween tween;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPool() { overlay = null; pool.Clear(); }

        public static void Play(RectTransform target)
        {
            if (target == null) return;
            var canvas = target.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var point = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
            if (overlay == null)
            {
                var root = new GameObject("UI Click Feedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                DontDestroyOnLoad(root);
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 32760;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                overlay = (RectTransform)root.transform;
            }
            var pulse = pool.Find(p => p != null && !p.gameObject.activeSelf);
            if (pulse == null)
            {
                if (pool.Count >= 6) return;
                var root = new GameObject("ClickRing", typeof(RectTransform), typeof(CanvasRenderer));
                root.transform.SetParent(overlay, false);
                pulse = root.AddComponent<UiClickPulse>();
                pulse.raycastTarget = false;
                pulse.rectTransform.sizeDelta = Vector2.one * 52f;
                pool.Add(pulse);
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, point, null, out var position);
            pulse.rectTransform.anchoredPosition = position;
            pulse.rectTransform.localScale = Vector3.one * 0.55f;
            pulse.color = new Color(1f, 0.91f, 0.65f, 0.75f);
            pulse.gameObject.SetActive(true);
            pulse.tween = DOTween.Sequence().SetUpdate(true)
                .Append(pulse.rectTransform.DOScale(1.45f, 0.32f).SetEase(Ease.OutCubic))
                .Join(pulse.DOFade(0f, 0.32f).SetEase(Ease.OutQuad))
                .OnComplete(() => { pulse.tween = null; pulse.gameObject.SetActive(false); });
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            const int count = 48;
            var radius = rectTransform.rect.width * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var a = i * Mathf.PI * 2f / count;
                var b = (i + 1) * Mathf.PI * 2f / count;
                var x = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var y = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                var start = vertices.currentVertCount;
                vertices.AddVert(x * radius, color, Vector2.zero);
                vertices.AddVert(x * (radius - 1.5f), color, Vector2.zero);
                vertices.AddVert(y * (radius - 1.5f), color, Vector2.zero);
                vertices.AddVert(y * radius, color, Vector2.zero);
                vertices.AddTriangle(start, start + 1, start + 2);
                vertices.AddTriangle(start, start + 2, start + 3);
            }
        }

        protected override void OnDisable()
        {
            tween?.Kill(); tween = null;
            base.OnDisable();
        }
    }
}
