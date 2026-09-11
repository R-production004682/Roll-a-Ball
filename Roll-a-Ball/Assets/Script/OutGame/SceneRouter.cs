using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGame から別シーンへ移動する責務だけを持つクラス
    /// 各画面から SceneManager を直接呼ばないための薄い窓口として使用
    /// </summary>
    public static class SceneRouter
    {
        // 現在実行中の非同期処理を保持
        private static AsyncOperation currentLoadOperation;
        private static bool transitionRequested;

        /// <summary>
        /// プレイ開始時に Scene 遷移状態を初期化する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            currentLoadOperation = null;
            transitionRequested = false;
        }

        /// <summary>
        /// 指定された Scene を読み込む
        /// </summary>
        /// <param name="scenePath">読み込む Scene の Asset パス</param>
        /// <param name="context">エラー発生時に紐付ける Unity Object</param>
        /// <returns>読み込みを開始できた場合は true</returns>
        public static bool LoadScene(string scenePath, Object context)
        {
            if (transitionRequested || FadeTransition.IsTransitioning || currentLoadOperation != null && !currentLoadOperation.isDone)
            {
                Debug.LogWarning("シーン遷移はすでに実行中です。", context);
                return false;
            }


            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogError("遷移先が設定されていません。", context);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError($"シーンが Build Settings に登録されていません: {scenePath}", context);
                return false;
            }

            var fadeTransition = FadeTransition.GetPersistent();
            transitionRequested = true;
            var started = fadeTransition.PlaySceneLoad(
                scenePath,
                () =>
                {
                    currentLoadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                    return currentLoadOperation;
                },
                _ => transitionRequested = false,
                context
            );

            if (!started)
            {
                transitionRequested = false;
                Debug.LogError($"シーンのフェード遷移を開始できませんでした: {scenePath}", context);
                return false;
            }

            Debug.Log($"シーン遷移を開始しました: {scenePath}", context);
            return true;
        }
    }
}
