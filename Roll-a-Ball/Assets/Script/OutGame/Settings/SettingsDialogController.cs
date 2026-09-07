using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>共有設定ダイアログ。呼び出し元の画面と選択状態を維持する。</summary>
    public sealed class SettingsDialogController : MonoBehaviour
    {
        [SerializeField] private GameObject dialog;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider seSlider;
        [SerializeField] private TMP_Text bgmValue;
        [SerializeField] private TMP_Text seValue;
        [SerializeField] private TMP_Text backLabel;
        [SerializeField] private TMP_Text contextLabel;
        [SerializeField] private AudioSource previewSource;
        [SerializeField] private AudioClip previewClip;
        [SerializeField] private GameObject pauseMenu;

        private GameObject previousSelection;
        private bool fromPause;
        public bool IsOpen => dialog != null && dialog.activeSelf;

        private void Awake()
        {
            dialog.SetActive(false);
            // 通常音は AudioListener.pause で停止し、試聴音だけ例外にする。
            previewSource.ignoreListenerPause = true;
            previewSource.playOnAwake = false;
            bgmSlider.onValueChanged.AddListener(GameSettings.SetBgmVolume);
            seSlider.onValueChanged.AddListener(GameSettings.SetSeVolume);
            RefreshValues();
        }

        private void OnEnable() => GameSettings.Changed += RefreshValues;
        private void OnDisable()
        {
            GameSettings.Changed -= RefreshValues;
            if (previewSource != null) previewSource.Stop();
            if (IsOpen) Close();
        }

        private void OnDestroy()
        {
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(GameSettings.SetBgmVolume);
            if (seSlider != null) seSlider.onValueChanged.RemoveListener(GameSettings.SetSeVolume);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        public void OpenFromStageSelect() => Open(false);
        public void OpenFromPause() => Open(true);

        private void Open(bool openedFromPause)
        {
            if (IsOpen) return;
            fromPause = openedFromPause;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (fromPause)
            {
                OutGameStateController.Enter(GameFlowState.Paused);
                if (pauseMenu != null) pauseMenu.SetActive(false);
            }
            backLabel.text = fromPause ? "BACK TO PAUSE" : "BACK TO STAGES";
            contextLabel.text = fromPause ? "GAME PAUSED  /  Test SE to preview your volume" : "MAKE YOURSELF COMFORTABLE";
            RefreshValues();
            dialog.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(bgmSlider.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            GameSettings.Save();
            previewSource.Stop();
            dialog.SetActive(false);
            if (fromPause && pauseMenu != null) pauseMenu.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        }

        public void TestSe()
        {
            if (!IsOpen || previewClip == null) return;
            previewSource.Stop();
            previewSource.clip = previewClip;
            previewSource.volume = GameSettings.SeVolume;
            previewSource.Play();
        }

        public void RestoreDefaults() => GameSettings.RestoreDefaults();

        private void RefreshValues()
        {
            bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
            seSlider.SetValueWithoutNotify(GameSettings.SeVolume);
            bgmValue.text = Mathf.RoundToInt(GameSettings.BgmVolume * 100f) + "%";
            seValue.text = Mathf.RoundToInt(GameSettings.SeVolume * 100f) + "%";
            previewSource.volume = GameSettings.SeVolume;
        }

        private void OnApplicationPause(bool paused) { if (paused) GameSettings.Save(); }
        private void OnApplicationQuit() => GameSettings.Save();
    }
}
