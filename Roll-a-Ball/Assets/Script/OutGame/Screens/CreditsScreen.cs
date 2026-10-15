using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// タイトルの上に制作スタッフを表示し、閉じると元のタイトルへ操作を戻す
    /// </summary>
    public sealed class CreditsScreen : ScreenBase
    {
        [SerializeField, Tooltip("クレジットの右上に配置した閉じるボタンです。")]
        private UnityEngine.UI.Button closeButton;
        private UiInputScope inputScope;
        private Action onClosed;

        /// <summary>
        /// 閉じるボタンのマウス操作を登録する
        /// </summary>
        public override void OnOpen(object arg)
        {
            inputScope = GetComponent<UiInputScope>();
            onClosed = arg as Action;
            if (closeButton == null)
            {
                Debug.LogError("CreditsScreen の closeButton が未設定です。Prefab を確認してください。", this);
                return;
            }

            closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// 購読を解除し、タイトルの入力再開を通知する
        /// </summary>
        public override void OnClose()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        /// <summary>
        /// 最前面のクレジットを閉じてタイトルを復元する
        /// </summary>
        private void Close()
        {
            if (!inputScope.CanReceiveInput)
            {
                return;
            }

            GameServices.Screens.Pop();
        }
    }
}
