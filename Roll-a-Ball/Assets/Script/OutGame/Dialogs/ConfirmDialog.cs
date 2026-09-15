using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// メッセージを表示し、決定で true、戻る操作で false を返す共通確認ダイアログ
    /// </summary>
    public sealed class ConfirmDialog : DialogBase<bool>
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button okButton;
        [SerializeField] private Button cancelButton;

        /// <summary>
        /// 文字列の表示引数とボタン購読を設定し、必須参照不足では例外で表示を中止する
        /// </summary>
        public override void OnOpen(object arg)
        {
            if (messageText == null || okButton == null || cancelButton == null)
            {
                throw new InvalidOperationException($"ConfirmDialog の参照が不足しています: {name}");
            }

            messageText.text = arg as string ?? "Are you sure?";
            okButton.onClick.AddListener(Accept);
            cancelButton.onClick.AddListener(Decline);
            GetComponent<UiInputScope>().CancelRequested.AddListener(Decline);
        }

        /// <summary>
        /// ボタンと Cancel の購読を解除し、途中で表示に失敗した場合も後始末する
        /// </summary>
        public override void OnClose()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(Accept);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Decline);
            }

            GetComponent<UiInputScope>().CancelRequested.RemoveListener(Decline);
        }

        /// <summary>
        /// 最前面でのみ決定を所有 Manager へ渡す
        /// </summary>
        private void Accept() => Complete(true);

        /// <summary>
        /// 最前面でのみ通常のキャンセル結果を所有 Manager へ渡す
        /// </summary>
        private void Decline() => Complete(false);
    }
}
