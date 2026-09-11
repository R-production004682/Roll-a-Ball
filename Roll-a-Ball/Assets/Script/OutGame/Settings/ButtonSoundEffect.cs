using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Button の決定時に指定した UI 効果音を再生する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSoundEffect : MonoBehaviour
    {
        [SerializeField] private AudioClip clickSound;
        private Button button;

        /// <summary>
        /// Button のクリックイベントへ効果音処理を登録する
        /// </summary>
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Play);

            if (clickSound == null)
            {
                Debug.LogWarning("ButtonSoundEffect の clickSound が未設定です。", this);
            }
        }

        /// <summary>
        /// Button のクリック効果音購読を解除する
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Play);
        }

        /// <summary>
        /// 設定された効果音を UI 用プレイヤーへ渡す
        /// </summary>
        private void Play() => UiSoundPlayer.Play(clickSound);
    }
}
