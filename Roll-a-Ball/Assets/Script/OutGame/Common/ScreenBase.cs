using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 同一シーン内で表示される主画面の共通基底
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public abstract class ScreenBase : MonoBehaviour
    {
        /// <summary>
        /// 画面を表示するときに呼び出され、派生クラスで参照設定と購読を初期化する
        /// </summary>
        public virtual void OnOpen(object arg) { }

        /// <summary>
        /// 画面を閉じるときに呼び出され、派生クラスで購読と一時状態を解除する
        /// </summary>
        public virtual void OnClose() { }
    }
}
