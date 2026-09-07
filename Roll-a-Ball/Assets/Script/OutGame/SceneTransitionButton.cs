using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>Inspector で指定された Scene へ遷移する Button。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class SceneTransitionButton : MonoBehaviour
    {
        [SerializeField, Tooltip("遷移先の Scene アセット。Build Profiles の Scene List にも登録してください。")]
        private SceneReference destination = new SceneReference();
        private Button button;

#if UNITY_EDITOR
        private void OnValidate() => SynchronizeDestination();
        public void SynchronizeDestination() => destination.SynchronizePath();
#endif

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(LoadDestination);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(LoadDestination);
        }

        public void LoadDestination()
        {
            if (!destination.IsAssigned)
            {
                Debug.LogError("遷移先 Scene が設定されていません。", this);
                return;
            }

            SceneRouter.LoadScene(destination.Path, this);
        }
    }
}
