using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Inspector で指定された Scene へ遷移する Button
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class SceneTransitionButton : MonoBehaviour
    {
        [SerializeField, Tooltip("遷移先の Scene アセット。Build Profiles の Scene List にも登録してください。")]
        private SceneReference destination = new SceneReference();
        private Button button;

#if UNITY_EDITOR
        /// <summary>
        /// Inspector で SceneReference のパスを最新化する
        /// </summary>
        private void OnValidate() => SynchronizeDestination();

        /// <summary>
        /// SceneReference の保存パスを同期する
        /// </summary>
        public void SynchronizeDestination() => destination.SynchronizePath();
#endif

        /// <summary>
        /// Button のクリックイベントへ遷移処理を一度だけ登録する
        /// </summary>
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(LoadDestination);
        }

        /// <summary>
        /// Button のクリックイベントから遷移処理を解除する
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(LoadDestination);
        }

        /// <summary>
        /// Inspector で指定された Scene への読み込みを開始する
        /// </summary>
        public void LoadDestination()
        {
            if (!destination.IsAssigned)
            {
                Debug.LogError("遷移先 Scene が設定されていません。", this);
                return;
            }

            if (!SceneRouter.LoadScene(destination.Path, this))
            {
                Debug.LogError($"シーン遷移を開始できませんでした: {destination.Path}", this);
            }
        }
    }
}
