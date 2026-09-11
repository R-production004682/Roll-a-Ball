using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 共有設定ダイアログで、呼び出し元の画面と選択状態を維持する
    /// </summary>
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

        /// <summary>
        /// 必須参照を検証し、設定 UI と試聴音を初期化する
        /// 参照が不足する場合はログを記録してコンポーネントを無効化する
        /// </summary>
        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            dialog.SetActive(false);
            // 通常音は AudioListener.pause で停止し、試聴音だけ例外にする。
            previewSource.ignoreListenerPause = true;
            previewSource.playOnAwake = false;
            bgmSlider.onValueChanged.AddListener(GameSettings.SetBgmVolume);
            seSlider.onValueChanged.AddListener(GameSettings.SetSeVolume);
            RefreshValues();
        }

        /// <summary>
        /// 設定値の変更通知を購読する
        /// </summary>
        private void OnEnable() => GameSettings.Changed += RefreshValues;

        /// <summary>
        /// 設定値の変更通知を解除し、試聴音と表示を後始末する
        /// </summary>
        private void OnDisable()
        {
            GameSettings.Changed -= RefreshValues;
            if (previewSource != null) previewSource.Stop();
            if (IsOpen) Close();
        }

        /// <summary>
        /// スライダーの購読を解除して破棄時の多重登録を防ぐ
        /// </summary>
        private void OnDestroy()
        {
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(GameSettings.SetBgmVolume);
            if (seSlider != null) seSlider.onValueChanged.RemoveListener(GameSettings.SetSeVolume);
        }

        /// <summary>
        /// 表示中の Escape 入力で設定ダイアログを閉じる
        /// </summary>
        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        /// <summary>
        /// ステージ選択画面から設定を開く
        /// </summary>
        public void OpenFromStageSelect() => Open(false);

        /// <summary>
        /// ポーズ画面から設定を開き、ポーズ状態を維持する
        /// </summary>
        public void OpenFromPause() => Open(true);

        /// <summary>
        /// 呼び出し元を記録し、設定 UI とフォーカスを表示する
        /// </summary>
        /// <param name="openedFromPause">ポーズ画面から開いた場合は true</param>
        private void Open(bool openedFromPause)
        {
            if (IsOpen)
            {
                Debug.LogWarning("設定ダイアログはすでに開いています。", this);
                return;
            }
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
            Debug.Log(fromPause ? "ポーズ中の設定ダイアログを開きました。" : "設定ダイアログを開きました。", this);
        }

        /// <summary>
        /// 設定を保存し、呼び出し元の UI と選択状態へ戻る
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;
            GameSettings.Save();
            previewSource.Stop();
            dialog.SetActive(false);
            if (fromPause && pauseMenu != null) pauseMenu.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);

            previousSelection = null;
            Debug.Log("設定ダイアログを閉じ、設定を保存しました。", this);
        }

        /// <summary>
        /// 現在の SE 音量で試聴音を再生する
        /// </summary>
        public void TestSe()
        {
            if (!IsOpen)
            {
                Debug.LogWarning("閉じている設定ダイアログから試聴を開始できません。", this);
                return;
            }

            if (previewClip == null)
            {
                Debug.LogError("試聴用 AudioClip が設定されていません。", this);
                return;
            }

            previewSource.Stop();
            previewSource.clip = previewClip;
            previewSource.volume = GameSettings.SeVolume;
            previewSource.Play();
            Debug.Log("SE の試聴を再生しました。", this);
        }

        /// <summary>
        /// 設定値を既定値へ戻す
        /// </summary>
        public void RestoreDefaults() => GameSettings.RestoreDefaults();

        /// <summary>
        /// GameSettings の値をスライダー、ラベル、試聴音へ反映する
        /// </summary>
        private void RefreshValues()
        {
            if (bgmSlider == null || seSlider == null || bgmValue == null || seValue == null)
            {
                return;
            }

            bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
            seSlider.SetValueWithoutNotify(GameSettings.SeVolume);
            bgmValue.text = Mathf.RoundToInt(GameSettings.BgmVolume * 100f) + "%";
            seValue.text = Mathf.RoundToInt(GameSettings.SeVolume * 100f) + "%";
            previewSource.volume = GameSettings.SeVolume;
        }

        /// <summary>
        /// 設定ダイアログが動作に必要とする Inspector 参照を検証する
        /// </summary>
        /// <returns>必須参照がすべて設定されている場合は true</returns>
        private bool ValidateReferences()
        {
            var isValid = dialog != null && bgmSlider != null && seSlider != null &&
                bgmValue != null && seValue != null && backLabel != null &&
                contextLabel != null && previewSource != null;

            if (!isValid)
            {
                Debug.LogError("SettingsDialogController の UI 参照が不足しています。", this);
            }

            return isValid;
        }

        /// <summary>
        /// アプリがバックグラウンドへ移るとき設定を保存する
        /// </summary>
        private void OnApplicationPause(bool paused) { if (paused) GameSettings.Save(); }

        /// <summary>
        /// アプリ終了時に設定を保存する
        /// </summary>
        private void OnApplicationQuit() => GameSettings.Save();
    }
}
