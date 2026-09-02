using System.Collections;
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
            var sceneName = SceneNameMap.Get(targetScene);

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("遷移先が設定されていません。", this);
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Scene を非同期で読み込む
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );
        }
    }
}
