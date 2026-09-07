using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>Button の決定時に指定した UI 効果音を再生する。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSoundEffect : MonoBehaviour
    {
        [SerializeField] private AudioClip clickSound;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Play);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Play);
        }

        private void Play() => UiSoundPlayer.Play(clickSound);
    }
}
