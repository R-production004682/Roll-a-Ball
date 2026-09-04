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

        /// <summary>
        /// 指定された Scene を読み込む
        /// </summary>
        /// <param name="sceneType">読み込む Scene</param>
        /// <param name="context">エラー発生時に紐付ける Unity Object</param>
        /// <returns>読み込みを開始できた場合は true</returns>
        public static bool LoadScene(SceneType sceneType, Object context = null)
        {
            if (currentLoadOperation != null && !currentLoadOperation.isDone)
            {
                Debug.LogWarning("シーン遷移はすでに実行中です。", context);
                return false;
            }

            if (!SceneNameMap.TryGet(sceneType, out var sceneName))
            {
                Debug.LogError("遷移先が設定されていません。", context);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"シーンが Build Settings に登録されていません: {sceneName}", context);
                return false;
            }

            currentLoadOperation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );

            if (currentLoadOperation == null)
            {
                Debug.LogError($"シーンの読み込みを開始できませんでした: {sceneName}", context);
                return false;
            }

            return true;
        }
    }
}
