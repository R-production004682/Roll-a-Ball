using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGame から別シーンへ移動する責務だけを持つクラス
    /// Button から SceneManager を直接呼ばないための薄い窓口として使用
    /// </summary>
    public sealed class SceneRouter : MonoBehaviour
    {
        [SerializeField, Header("遷移させたい Scene を指定する")] private SceneType targetScene;

        /// <summary>
        /// 選択された Scene を読み込む
        /// </summary>
        public void LoadSelectedScene()
        {
            LoadScene(targetScene, this);
        }

        /// <summary>
        /// 指定された Scene を読み込む
        /// </summary>
        /// <param name="sceneType">読み込む Scene</param>
        /// <param name="context">エラー発生時に紐付ける Unity Object</param>
        /// <returns>読み込みを開始できた場合は true</returns>
        public static bool LoadScene(SceneType sceneType, Object context = null)
        {
            var sceneName = SceneNameMap.Get(sceneType);

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("遷移先が設定されていません。", context);
                return false;
            }

            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );

            return true;
        }
    }
}
