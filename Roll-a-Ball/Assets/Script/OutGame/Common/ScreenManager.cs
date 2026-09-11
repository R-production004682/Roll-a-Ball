using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 主画面と Dialog の生成、寿命、入力対象を一元管理する
    /// </summary>
    public sealed class ScreenManager : MonoBehaviour
    {
        [SerializeField] private Transform screenRoot;
        [SerializeField] private List<ScreenBase> screenPrefabs = new List<ScreenBase>();
        [SerializeField] private ScreenBase initialScreen;
        [SerializeField] private FadeTransition screenTransition;

        private readonly List<ScreenBase> stack = new List<ScreenBase>();
        private bool changing;
        private Transform stagingRoot;
        private ScreenBase pendingScreen;

        /// <summary>
        /// ScreenRoot を補完し、現在のシーンへ Manager を登録する
        /// </summary>
        private void Awake()
        {
            if (screenRoot == null)
            {
                screenRoot = transform;
            }

            if (screenTransition == null)
            {
                screenTransition = GetComponentInChildren<FadeTransition>();
            }

            if (screenTransition == null)
            {
                screenTransition = FadeTransition.Create(transform);
            }

            GameServices.RegisterScreens(this);
            var stagingObject = new GameObject("PendingScreens");
            stagingObject.transform.SetParent(transform, false);
            stagingObject.SetActive(false);
            stagingRoot = stagingObject.transform;
            Debug.Log("ScreenManager を登録しました。", this);
        }

        /// <summary>
        /// Inspector に指定された初期 Screen を一度だけ表示する
        /// </summary>
        private void Start()
        {
            if (initialScreen != null && stack.Count == 0)
            {
                var screen = Create(initialScreen.GetType());
                if (screen != null) CommitReplacement(screen, null);
                return;
            }

            if (initialScreen == null)
            {
                Debug.LogWarning("初期 Screen が未設定です。必要な画面を明示的に Replace してください。", this);
            }
        }

        /// <summary>
        /// Manager 破棄時に表示中の画面を閉じ、サービス登録を解除する
        /// </summary>
        private void OnDestroy()
        {
            if (pendingScreen != null && pendingScreen != initialScreen)
            {
                Destroy(pendingScreen.gameObject);
            }
            CloseAll();
            GameServices.UnregisterScreens(this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor 上で表示ルートと Screen prefab の設定を検証する
        /// </summary>
        private void OnValidate()
        {
            if (screenRoot == null)
            {
                Debug.LogWarning("ScreenManager の screenRoot が未設定です。自身の Transform を使用します。", this);
            }

            var duplicate = screenPrefabs
                .Where(prefab => prefab != null)
                .GroupBy(prefab => prefab.GetType())
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                Debug.LogError($"Screen prefab の型が重複しています: {duplicate.Key.Name}", this);
            }
        }
#endif

        /// <summary>
        /// 暗転中の主画面切り替えを予約し、表示前のインスタンスを返す
        /// </summary>
        /// <typeparam name="T">表示する Screen の型</typeparam>
        /// <param name="arg">Screen に渡す任意の表示引数</param>
        /// <returns>生成された Screen生成できない場合は null</returns>
        public T Replace<T>(object arg = null) where T : ScreenBase
        {
            return (T)Replace(typeof(T), arg);
        }

        /// <summary>
        /// 現在の表示の上に Screen または Dialog を追加する
        /// </summary>
        /// <typeparam name="T">追加する Screen の型</typeparam>
        /// <param name="arg">Screen に渡す任意の表示引数</param>
        /// <returns>生成された Screen生成できない場合は null</returns>
        public T Push<T>(object arg = null) where T : ScreenBase
        {
            return (T)Push(typeof(T), arg);
        }

        /// <summary>
        /// 最上位の表示を閉じ、下の表示へ操作を戻す
        /// </summary>
        public void Pop()
        {
            if (stack.Count == 0)
            {
                Debug.LogWarning("閉じる表示がありません。", this);
                return;
            }

            if (changing || FadeTransition.IsTransitioning)
            {
                Debug.LogWarning("画面遷移中のため Pop を無視しました。", this);
                return;
            }

            changing = true;
            var current = stack[stack.Count - 1];
            try
            {
                stack.RemoveAt(stack.Count - 1);
                CloseAndDestroy(current);
            }
            finally
            {
                changing = false;
            }

            Debug.Log($"画面を閉じました: {current.GetType().Name}", this);
        }

        /// <summary>
        /// Dialog を表示し、完了またはキャンセルの結果を非同期で待機する
        /// </summary>
        /// <typeparam name="TDialog">表示する Dialog の型</typeparam>
        /// <typeparam name="TResult">Dialog が返す結果の型</typeparam>
        /// <param name="arg">Dialog に渡す任意の表示引数</param>
        /// <returns>Dialog の確定結果</returns>
        public async Task<TResult> ShowDialogAsync<TDialog, TResult>(object arg = null)
            where TDialog : DialogBase<TResult>
        {
            var dialog = Push<TDialog>(arg);
            if (dialog == null)
            {
                Debug.LogError($"Dialog を表示できませんでした: {typeof(TDialog).Name}", this);
                return await Task.FromCanceled<TResult>(new System.Threading.CancellationToken(true));
            }

            return await dialog.CompletionTask;
        }

        /// <summary>
        /// 最上位の Dialog を結果確定として閉じる
        /// </summary>
        /// <typeparam name="TResult">Dialog の結果型</typeparam>
        /// <param name="dialog">完了させる Dialog</param>
        /// <param name="result">呼び出し側へ返す結果</param>
        internal void CompleteDialog<TResult>(DialogBase<TResult> dialog, TResult result)
        {
            if (dialog == null)
            {
                Debug.LogError("null の Dialog から完了通知を受け取りました。", this);
                return;
            }

            if (stack.Count == 0 || stack[stack.Count - 1] != dialog)
            {
                Debug.LogError("最上位ではない Dialog から完了通知を受け取りました。", this);
                dialog.Cancel();
                return;
            }

            stack.RemoveAt(stack.Count - 1);
            dialog.SetResult(result);
            CloseAndDestroy(dialog);
            Debug.Log($"Dialog を結果確定で閉じました: {dialog.GetType().Name}", this);
        }

        /// <summary>
        /// 指定した型の Screen を準備し、暗転完了時に主画面を切り替える
        /// </summary>
        /// <param name="type">表示する Screen の型</param>
        /// <param name="arg">Screen に渡す任意の表示引数</param>
        /// <returns>生成された Screen生成できない場合は null</returns>
        public ScreenBase Replace(Type type, object arg = null)
        {
            if (type == null || !typeof(ScreenBase).IsAssignableFrom(type))
            {
                Debug.LogError("ScreenBase ではない型は表示できません。", this);
                return null;
            }

            if (changing || FadeTransition.IsTransitioning)
            {
                Debug.LogWarning($"画面遷移中のため Replace を無視しました: {type.Name}", this);
                return null;
            }

            var screen = Create(type);
            if (screen == null) return null;

            changing = true;
            pendingScreen = screen;
            if (stack.Count == 0)
            {
                CommitReplacement(screen, arg);
                changing = false;
            }
            else if (screenTransition == null || !screenTransition.Play(
                () => CommitReplacement(screen, arg), () => changing = false))
            {
                if (screen != initialScreen) Destroy(screen.gameObject);
                pendingScreen = null;
                changing = false;
                Debug.LogError($"画面フェードを開始できません: {type.Name}", this);
                return null;
            }
            return screen;
        }

        /// <summary>
        /// 完全な暗転中に旧画面を閉じ、準備した Screen の Canvas 全体を表示する
        /// </summary>
        private void CommitReplacement(ScreenBase screen, object arg)
        {
            if (this == null || screen == null) return;
            CloseAll();
            screen.transform.SetParent(screenRoot, false);
            screen.gameObject.SetActive(true);
            stack.Add(screen);
            pendingScreen = null;
            if (!OpenScreen(screen, arg))
            {
                stack.Remove(screen);
                CloseAndDestroy(screen);
                return;
            }
            Debug.Log($"画面を切り替えました: {screen.GetType().Name}", this);
        }

        /// <summary>
        /// 指定した型の Screen または Dialog を表示スタックへ追加する
        /// </summary>
        /// <param name="type">追加する Screen の型</param>
        /// <param name="arg">Screen に渡す任意の表示引数</param>
        /// <returns>生成された Screen生成できない場合は null</returns>
        private ScreenBase Push(Type type, object arg = null)
        {
            if (type == null || !typeof(ScreenBase).IsAssignableFrom(type))
            {
                Debug.LogError("ScreenBase ではない型は追加できません。", this);
                return null;
            }

            if (changing || FadeTransition.IsTransitioning)
            {
                Debug.LogWarning($"画面遷移中のため Push を無視しました: {type.Name}", this);
                return null;
            }

            changing = true;
            ScreenBase screen = null;
            try
            {
                screen = Create(type);
                if (screen != null)
                {
                    screen.transform.SetParent(screenRoot, false);
                    screen.gameObject.SetActive(true);
                    stack.Add(screen);
                    if (!OpenScreen(screen, arg))
                    {
                        stack.Remove(screen);
                        CloseAndDestroy(screen);
                        screen = null;
                    }
                }
            }
            finally
            {
                changing = false;
            }
            Debug.Log(screen != null ? $"画面を追加しました: {type.Name}" : $"画面を追加できませんでした: {type.Name}", this);
            return screen;
        }

        /// <summary>
        /// Scene 所有の初期画面を返すか、登録済み prefab を非表示の待機ルートへ生成する
        /// </summary>
        /// <param name="type">生成する Screen の型</param>
        /// <returns>生成された Screen登録がない場合は null</returns>
        private ScreenBase Create(Type type)
        {
            if (initialScreen != null && initialScreen.GetType() == type && initialScreen.gameObject.scene.IsValid())
            {
                return initialScreen;
            }

            if (screenRoot == null)
            {
                Debug.LogError("screenRoot が未設定のため Screen を生成できません。", this);
                return null;
            }

            var prefab = screenPrefabs.FirstOrDefault(candidate => candidate != null && candidate.GetType() == type);
            if (prefab == null)
            {
                Debug.LogError($"Screen prefab が登録されていません: {type.Name}", this);
                return null;
            }

            try
            {
                return Instantiate(prefab, stagingRoot);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Screen の生成に失敗しました: {type.Name}\n{exception}", this);
                return null;
            }
        }

        /// <summary>
        /// Screen.OnOpen を実行し、例外発生時に原因を記録する
        /// </summary>
        /// <param name="screen">開く Screen</param>
        /// <param name="arg">Screen に渡す表示引数</param>
        /// <returns>正常に開けた場合は true</returns>
        private bool OpenScreen(ScreenBase screen, object arg)
        {
            try
            {
                if (screen is IDialog dialog)
                {
                    dialog.PrepareForOpen(this);
                }

                screen.OnOpen(arg);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Screen の OnOpen に失敗しました: {screen.GetType().Name}\n{exception}", screen);
                return false;
            }
        }

        /// <summary>
        /// 表示スタックを空にし、各 Screen を閉じる
        /// </summary>
        private void CloseAll()
        {
            while (stack.Count > 0)
            {
                var current = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                CloseAndDestroy(current);
            }
        }

        /// <summary>
        /// Screen を閉じ、Prefab は破棄して Scene 所有 Screen は非表示にする
        /// </summary>
        /// <param name="screen">破棄対象の Screen</param>
        private void CloseAndDestroy(ScreenBase screen)
        {
            if (screen == null)
            {
                return;
            }

            try
            {
                screen.OnClose();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Screen の OnClose に失敗しました: {screen.GetType().Name}\n{exception}", screen);
            }

            if (screen is IDialog dialog)
            {
                try
                {
                    dialog.CancelForClose();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Dialog のキャンセル処理に失敗しました: {screen.GetType().Name}\n{exception}", screen);
                }
            }

            screen.gameObject.SetActive(false);
            if (screen == initialScreen && screen.gameObject.scene.IsValid())
            {
                return;
            }

            UnityEngine.Object.Destroy(screen.gameObject);
        }
    }
}
