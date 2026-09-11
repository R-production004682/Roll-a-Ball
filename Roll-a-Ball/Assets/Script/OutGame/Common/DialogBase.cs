using System.Threading.Tasks;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ScreenManager が Dialog の完了待機と終了処理を接続するための契約
    /// </summary>
    internal interface IDialog
    {
        /// <summary>
        /// Dialog の完了待機を所有 Manager に紐付ける
        /// </summary>
        void PrepareForOpen(ScreenManager screenManager);

        /// <summary>
        /// 画面切り替えやシーン破棄で Dialog をキャンセルする
        /// </summary>
        void CancelForClose();
    }

    /// <summary>
    /// 結果を返す重ね表示の共通基底で、完了処理は所有 Manager に委譲する
    /// </summary>
    public abstract class DialogBase<TResult> : ScreenBase, IDialog
    {
        private TaskCompletionSource<TResult> completionSource;
        private ScreenManager owner;
        private bool completed;

        /// <summary>
        /// Dialog の完了待機と所有 Manager を初期化する
        /// </summary>
        /// <param name="screenManager">この Dialog を所有する Manager</param>
        internal void Prepare(ScreenManager screenManager)
        {
            owner = screenManager;
            completed = false;
            completionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// 所有 Manager から Dialog の完了待機を準備する
        /// </summary>
        /// <param name="screenManager">この Dialog を所有する Manager</param>
        void IDialog.PrepareForOpen(ScreenManager screenManager) => Prepare(screenManager);

        /// <summary>
        /// Dialog の完了結果を待機する Task を取得する
        /// </summary>
        /// <returns>確定結果またはキャンセル状態の Task</returns>
        internal Task<TResult> CompletionTask => completionSource != null
            ? completionSource.Task
            : Task.FromCanceled<TResult>(new System.Threading.CancellationToken(true));

        /// <summary>
        /// Dialog の結果を確定し、所有 Manager にクローズを依頼する
        /// 完了済みまたは所有 Manager 不在の場合はログを記録して処理を中断する
        /// </summary>
        /// <param name="result">呼び出し側へ返す結果</param>
        protected void Complete(TResult result)
        {
            if (completed)
            {
                Debug.LogWarning($"完了済みの Dialog を再度完了しようとしました: {name}", this);
                return;
            }

            if (owner == null)
            {
                Debug.LogError($"所有 Manager がない Dialog は完了できません: {name}", this);
                return;
            }

            completed = true;
            owner.CompleteDialog(this, result);
        }

        /// <summary>
        /// Dialog の待機をキャンセルとして終了する
        /// すでに完了している場合は重複処理を行わない
        /// </summary>
        internal void Cancel()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            completionSource?.TrySetCanceled();
        }

        /// <summary>
        /// 完了待機へ確定結果を書き込む
        /// 完了待機が存在しない場合は処理を終了する
        /// </summary>
        /// <param name="result">確定した結果</param>
        internal void SetResult(TResult result)
        {
            completionSource?.TrySetResult(result);
        }

        /// <summary>
        /// Dialog 破棄前に所有 Manager への参照を解除する
        /// </summary>
        internal void ClearOwner()
        {
            owner = null;
        }

        /// <summary>
        /// 所有 Manager 以外からのクローズをキャンセルとして扱う
        /// </summary>
        void IDialog.CancelForClose() => Cancel();
    }
}
