using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// タイトルの入力案内を明滅させ、キーやマウスなどの入力で次のシーンへ進む
    /// </summary>
    public sealed class TitleScreen : ScreenBase
    {
        private const float MinimumBlinkDuration = 0.1f;

        [SerializeField, Tooltip("開始入力後に移動するシーンです。")]
        private SceneReference destination = new SceneReference();
        [SerializeField, Tooltip("Any Press Key の文字と光をまとめて明滅させるグループです。")]
        private CanvasGroup promptGroup;
        [SerializeField, Min(MinimumBlinkDuration), Tooltip("文字が暗くなり、元の明るさに戻るまでの秒数です。")]
        private float blinkDuration = 4f;
        [SerializeField, Range(0f, 1f), Tooltip("明滅中に最も暗くなったときの不透明度です。")]
        private float minimumAlpha = 0.2f;

        private UiInputScope inputScope;
        private Tween blinkTween;
        private bool isOpen;
        private bool transitionRequested;
        private int openedFrame;

#if UNITY_EDITOR
        /// <summary>
        /// Inspector の遷移先パスを Scene アセットの現在位置へ同期する
        /// </summary>
        private void OnValidate()
        {
            SynchronizeDestination();
        }

        /// <summary>
        /// ビルドで使用する遷移先パスを GUID から同期する
        /// </summary>
        public void SynchronizeDestination()
        {
            destination.SynchronizePath();
        }
#endif

        /// <summary>
        /// 入力受付を開始し、案内文字の緩やかな明滅を再生する
        /// </summary>
        public override void OnOpen(object arg)
        {
            if (promptGroup == null || !destination.IsAssigned)
            {
                Debug.LogError("TitleScreen の promptGroup または destination が未設定です。", this);
                return;
            }

            inputScope = GetComponent<UiInputScope>();
            openedFrame = Time.frameCount;
            transitionRequested = false;
            isOpen = true;
            StopBlink();
            blinkTween = promptGroup.DOFade(Mathf.Clamp01(minimumAlpha),
                    Mathf.Max(MinimumBlinkDuration, blinkDuration) * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        /// <summary>
        /// 画面を閉じ、入力受付と明滅を終了する
        /// </summary>
        public override void OnClose()
        {
            isOpen = false;
            StopBlink();
        }

        /// <summary>
        /// 外部からの非表示やシーン破棄でも明滅を停止する
        /// </summary>
        private void OnDisable()
        {
            isOpen = false;
            StopBlink();
        }

        /// <summary>
        /// 最前面のタイトルで新しい開始入力を受け付け、シーン遷移を一度だけ開始する
        /// </summary>
        private void Update()
        {
            if (!isOpen || transitionRequested || Time.frameCount <= openedFrame ||
                !Application.isFocused || !inputScope.CanReceiveInput || !HasStartInput())
            {
                return;
            }

            transitionRequested = SceneRouter.LoadScene(destination.Path, this);
        }

        /// <summary>
        /// キーボードの任意キー、マウスまたはゲームパッドのボタンが新たに押されたかを返す
        /// </summary>
        /// <returns>開始に使用できる入力がある場合は true</returns>
        private static bool HasStartInput()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && HasPressedButton(Mouse.current))
            {
                return true;
            }

            foreach (var gamepad in Gamepad.all)
            {
                if (HasPressedButton(gamepad))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定したデバイスのボタンが新たに押されたかを返す
        /// </summary>
        /// <param name="device">入力を確認するデバイス</param>
        /// <returns>新しく押されたボタンがある場合は true</returns>
        private static bool HasPressedButton(InputDevice device)
        {
            foreach (var control in device.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 再生中の Tween を破棄し、案内文字の不透明度を初期値へ戻す
        /// </summary>
        private void StopBlink()
        {
            blinkTween?.Kill();
            blinkTween = null;
            if (promptGroup != null)
            {
                promptGroup.alpha = 1f;
            }
        }
    }
}
