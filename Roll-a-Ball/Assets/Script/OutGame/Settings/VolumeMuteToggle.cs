using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 音量スライダーと音の ON/OFF を同期し、再開時に直前の音量へ戻す
    /// </summary>
    public sealed class VolumeMuteToggle : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider volumeSlider;
        [SerializeField] private UnityEngine.UI.Toggle soundToggle;
        [SerializeField] private TMPro.TMP_Text stateLabel;
        private float audibleVolume = 0.7f;

        /// <summary>
        /// 音量とトグルの変更を購読する
        /// </summary>
        private void OnEnable()
        {
            volumeSlider.onValueChanged.AddListener(Refresh);
            soundToggle.onValueChanged.AddListener(SetEnabled);
            Synchronize();
        }

        /// <summary>通知を伴わない設定値の読み込み後にも表示を同期する。</summary>
        public void Synchronize()
        {
            Refresh(volumeSlider.value);
        }

        /// <summary>
        /// 非表示時に購読を解除する
        /// </summary>
        private void OnDisable()
        {
            volumeSlider.onValueChanged.RemoveListener(Refresh);
            soundToggle.onValueChanged.RemoveListener(SetEnabled);
        }

        /// <summary>
        /// 音量を変更し、既存の設定保存処理へ通知する
        /// </summary>
        private void SetEnabled(bool soundEnabled)
        {
            volumeSlider.value = soundEnabled ? audibleVolume : 0f;
        }

        /// <summary>
        /// 音量に合わせてチェックと状態文字を更新する
        /// </summary>
        private void Refresh(float volume)
        {
            var soundEnabled = volume > 0f;
            if (soundEnabled)
            {
                audibleVolume = volume;
            }

            soundToggle.SetIsOnWithoutNotify(soundEnabled);
            stateLabel.text = soundEnabled ? "ON" : "OFF";
        }
    }
}
