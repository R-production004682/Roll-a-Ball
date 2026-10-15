using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// タイトルの入力案内を明滅させ、入力で同じシーンのステージ選択を表示する
    /// </summary>
    public sealed class TitleScreen : ScreenBase
    {
        private const float MinimumBlinkDuration = 0.1f;

        [SerializeField, Tooltip("開始案内の文字と光をまとめて明滅させるグループです。")]
        private CanvasGroup promptGroup;
        [SerializeField] private AudioClip confirmSound;
        [SerializeField, Min(MinimumBlinkDuration), Tooltip("文字が暗くなり、元の明るさに戻るまでの秒数です。")]
        private float blinkDuration = 4f;
        [SerializeField, Range(0f, 1f), Tooltip("明滅中に最も暗くなったときの不透明度です。")]
        private float minimumAlpha = 0.2f;
        [SerializeField, Tooltip("タイトルからクレジットを開くボタンです。")]
        private Button creditsButton;

        private UiInputScope inputScope;
        private Tween blinkTween;
        private bool isOpen;
        private bool transitionRequested;
        private int openedFrame;

        /// <summary>
        /// 入力受付を開始し、案内文字の緩やかな明滅を再生する
        /// </summary>
        public override void OnOpen(object arg)
        {
            if (promptGroup == null || creditsButton == null)
            {
                Debug.LogError("TitleScreen の開始案内・クレジットボタンの参照が不足しています。", this);
                return;
            }

            inputScope = GetComponent<UiInputScope>();
            var home = ForestHomeTransition.GetOrCreate();
            if (home != null)
            {
                home.ShowTitle();
                promptGroup = home.PromptGroup;
            }
            openedFrame = Time.frameCount;
            transitionRequested = false;
            isOpen = true;
            creditsButton.onClick.AddListener(OpenCredits);
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
            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(OpenCredits);
            }

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
            if (!isOpen || !inputScope.CanReceiveInput || transitionRequested)
            {
                return;
            }

            if (Time.frameCount <= openedFrame || !Application.isFocused)
            {
                return;
            }

            if (UsesFooterControl() || !HasStartInput())
            {
                return;
            }

            var home = ForestHomeTransition.GetOrCreate();
            if (home != null && !home.IsAnimating)
            {
                StopBlink();
                UiClickPulse.Play(promptGroup.transform as RectTransform);
                UiSoundPlayer.Play(confirmSound);
            }
            transitionRequested = home != null && home.EnterStageSelect(this);
        }

        /// <summary>
        /// 開始入力と競合させずにクレジット画面をタイトルへ重ねる
        /// </summary>
        private void OpenCredits()
        {
            if (!isOpen || transitionRequested || !inputScope.CanReceiveInput)
            {
                return;
            }

            var manager = GameServices.Screens;
            if (manager == null)
            {
                Debug.LogError("TitleScreen: クレジット表示用の ScreenManager がありません。", this);
                return;
            }

            manager.Push<CreditsScreen>((System.Action)ResumeTitleInput);
        }

        /// <summary>
        /// タイトルの表示と選択を維持したまま設定ダイアログを開く
        /// </summary>
        private void OpenSettings()
        {
            if (!isOpen || transitionRequested || !inputScope.CanReceiveInput)
            {
                return;
            }

            var settings = GameServices.Settings;
            if (settings == null)
            {
                Debug.LogError("TitleScreen: 設定表示用の SettingsDialogHost がありません。", this);
                return;
            }

            settings.OpenFromTitle(ResumeTitleInput);
        }

        /// <summary>
        /// 閉じる操作と同じフレームの開始入力を抑止する
        /// </summary>
        private void ResumeTitleInput()
        {
            openedFrame = Time.frameCount;
        }

        /// <summary>
        /// 設定とクレジットのクリックをタイトルの開始入力から除外する
        /// </summary>
        private bool UsesFooterControl()
        {
            return Mouse.current != null && HasPressedButton(Mouse.current) &&
                (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)creditsButton.transform, Mouse.current.position.ReadValue()));
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
