using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Title 画面の入力を受け取って、 SceneRouter にシーン遷移を依頼する
    /// UI の Button はこのクラスの OnStartButtonPressed を呼び出す
    /// </summary>
    public sealed class TitleController : MonoBehaviour
    {
        [SerializeField] private SceneRouter sceneRouter;

        public void OnStartButtonPressed()
        {
            if (sceneRouter == null)
            {
                Debug.LogError("TitleController に SceneRouter が設定されていません。", this);
                return;
            }

            sceneRouter.LoadSelectedScene();
        }
    }
}
