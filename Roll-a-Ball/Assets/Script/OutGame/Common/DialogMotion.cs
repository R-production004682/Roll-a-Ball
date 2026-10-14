using System;
using DG.Tweening;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ダイアログ共通の短い浮上・フェード。ポーズ中も動作し、演出中の二重入力を防ぐ
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        private CanvasGroup group;
        private Vector2 position;
        private Vector3 scale;
        private Sequence sequence;
        private IDisposable inputLock;

        /// <summary>
        /// ダイアログの透明度とパネルの初期位置を取得する
        /// </summary>
        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (panel == null)
            {
                Debug.LogError("DialogMotion: 演出対象の Panel を設定してください。ダイアログ演出を無効化します。", this);
                enabled = false;
                return;
            }
            position = panel.anchoredPosition;
            scale = panel.localScale;
        }

        /// <summary>
        /// 入力を一時停止してダイアログの表示演出を開始する
        /// </summary>
        private void OnEnable()
        {
            if (panel == null)
            {
                return;
            }
            inputLock = UiInputScope.BlockInput();
            group.alpha = 0f;
            panel.localScale = scale * 0.97f;
            panel.anchoredPosition = position + Vector2.down * 16f;
            sequence = DOTween.Sequence().SetUpdate(true)
                .Append(group.DOFade(1f, 0.24f).SetEase(Ease.OutCubic))
                .Join(panel.DOScale(scale, 0.28f).SetEase(Ease.OutCubic))
                .Join(panel.DOAnchorPos(position, 0.28f).SetEase(Ease.OutCubic))
                .OnComplete(Finish);
        }

        /// <summary>
        /// 閉じる演出の完了後に入力ロックを解除して呼び出し元へ通知する
        /// </summary>
        public static void Hide(GameObject dialog, Action onClosed)
        {
            var motion = dialog != null ? dialog.GetComponent<DialogMotion>() : null;
            if (motion == null || !motion.isActiveAndEnabled)
            {
                onClosed?.Invoke();
                return;
            }
            motion.sequence?.Kill();
            motion.Finish();
            motion.inputLock = UiInputScope.BlockInput();
            motion.sequence = DOTween.Sequence().SetUpdate(true)
                .Append(motion.group.DOFade(0f, 0.16f).SetEase(Ease.InQuad))
                .Join(motion.panel.DOScale(motion.scale * 0.985f, 0.16f).SetEase(Ease.InQuad))
                .Join(motion.panel.DOAnchorPos(motion.position + Vector2.down * 8f, 0.16f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    motion.Finish();
                    onClosed?.Invoke();
                });
        }

        /// <summary>
        /// 演出の参照と入力ロックを解放する
        /// </summary>
        private void Finish()
        {
            sequence = null;
            inputLock?.Dispose();
            inputLock = null;
        }

        /// <summary>
        /// 演出を停止しパネルの表示状態を初期値へ戻す
        /// </summary>
        private void OnDisable()
        {
            sequence?.Kill();
            Finish();
            if (group != null)
            {
                group.alpha = 1f;
            }
            if (panel == null)
            {
                return;
            }
            panel.localScale = scale;
            panel.anchoredPosition = position;
        }
    }
}
