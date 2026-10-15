using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// タイトルとステージ選択で音量設定・終了の入口をまとめる
    /// </summary>
    public sealed class SettingsMenuDialog : DialogBase<bool>
    {
        [SerializeField] private UnityEngine.UI.Button audioButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        private UiInputScope inputScope;
        private Action onClosed;

        /// <summary>
        /// 親メニューの各ボタンと閉じる通知を登録する
        /// </summary>
        public override void OnOpen(object arg)
        {
            inputScope = GetComponent<UiInputScope>();
            onClosed = arg as Action;
            if (audioButton == null || quitButton == null || closeButton == null)
            {
                Debug.LogError("SettingsMenuDialog のボタン参照が不足しています。Prefab を確認してください。", this);
                Complete(false);
                return;
            }

            audioButton.onClick.AddListener(OpenAudio);
            quitButton.onClick.AddListener(QuitGame);
            closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// ボタンの購読を解除し、呼び出し元へ終了を通知する
        /// </summary>
        public override void OnClose()
        {
            if (audioButton != null)
            {
                audioButton.onClick.RemoveListener(OpenAudio);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        /// <summary>
        /// 親メニューの上に共通の音量設定を表示する
        /// </summary>
        private void OpenAudio()
        {
            if (!inputScope.CanReceiveInput)
            {
                return;
            }

            var settings = GameServices.Settings;
            if (settings == null)
            {
                Debug.LogError("音量設定を表示する SettingsDialogHost がありません。", this);
                return;
            }

            settings.OpenAudioFromMenu();
        }

        /// <summary>
        /// 親メニューを閉じて既存の窓演出からゲームを終了する
        /// </summary>
        private void QuitGame()
        {
            if (!inputScope.CanReceiveInput)
            {
                return;
            }

            var home = ForestHomeTransition.GetOrCreate();
            var manager = GameServices.Screens;
            if (home == null || manager == null)
            {
                return;
            }

            GameSettings.Save();
            DialogMotion.Hide(gameObject, () =>
            {
                manager.Pop();
                home.QuitGame();
            });
        }

        /// <summary>
        /// × ボタンから親メニューを閉じる
        /// </summary>
        private void Close()
        {
            if (inputScope.CanReceiveInput)
            {
                Complete(false);
            }
        }
    }
}
