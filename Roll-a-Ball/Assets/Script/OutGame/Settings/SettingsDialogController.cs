using System.Collections;
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
        private const float PercentPerUnitVolume = 100f;

        /// <summary>
        /// 設定を閉じたときに戻る画面を区別する
        /// </summary>
        private enum OpenOrigin
        {
            StageSelect,
            Pause
        }

        [SerializeField, Tooltip("設定画面全体のオブジェクトです。")]
        private GameObject dialog;
        [SerializeField, Tooltip("BGM 音量を変更するスライダーです。")]
        private Slider bgmSlider;
        [SerializeField, Tooltip("効果音の音量を変更するスライダーです。")]
        private Slider seSlider;
        [SerializeField, Tooltip("鳥の声や森の環境音の音量です。")]
        private Slider ambientSlider;
        [SerializeField] private TMP_Text ambientValue;
        [SerializeField, Tooltip("BGM 音量を数字で表示するテキストです。")]
        private TMP_Text bgmValue;
        [SerializeField, Tooltip("効果音の音量を数字で表示するテキストです。")]
        private TMP_Text seValue;
        [SerializeField, Tooltip("呼び出し元に戻る案内を表示するテキストです。")]
        private TMP_Text backLabel;
        [SerializeField, Tooltip("設定画面の説明を表示するテキストです。")]
        private TMP_Text contextLabel;
        [SerializeField, Tooltip("効果音を試しに鳴らす音源です。")]
        private AudioSource previewSource;
        [SerializeField, Tooltip("効果音を試しに再生するときに使う音です。")]
        private AudioClip previewClip;
        [SerializeField, Tooltip("ポーズ画面から設定を開いたときに隠すポーズ画面です。")]
        private GameObject pauseMenu;

        private GameObject previousSelection;
        private UiInputScope dialogInputScope;
        private OpenOrigin openOrigin;
        private bool confirmingDefaults;

        /// <summary>
        /// 設定ダイアログが表示中か
        /// </summary>
        public bool IsOpen => dialog != null && dialog.activeSelf;

        /// <summary>
        /// 必須参照を検証し、設定 UI と試聴音を初期化する
        /// 参照が不足する場合はログを記録してコンポーネントを無効化する
        /// </summary>
        private void Awake()
        {
            dialogInputScope = dialog != null ? dialog.GetComponent<UiInputScope>() : null;
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
            ambientSlider.onValueChanged.AddListener(GameSettings.SetAmbientVolume);
            RefreshValues();
        }

        /// <summary>
        /// 設定値の変更通知を購読する
        /// </summary>
        private void OnEnable()
        {
            GameSettings.Changed += RefreshValues;
        }

        /// <summary>
        /// 設定値の変更通知を解除し、試聴音と表示を後始末する
        /// </summary>
        private void OnDisable()
        {
            GameSettings.Changed -= RefreshValues;
            confirmingDefaults = false;
            if (previewSource != null)
            {
                previewSource.Stop();
            }

            if (IsOpen)
            {
                Close();
            }
        }

        /// <summary>
        /// スライダーの購読を解除して破棄時の多重登録を防ぐ
        /// </summary>
        private void OnDestroy()
        {
            if (ambientSlider != null) ambientSlider.onValueChanged.RemoveListener(GameSettings.SetAmbientVolume);
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(GameSettings.SetBgmVolume);
            }

            if (seSlider != null)
            {
                seSlider.onValueChanged.RemoveListener(GameSettings.SetSeVolume);
            }
        }

        /// <summary>
        /// ステージ選択画面から設定を開く
        /// </summary>
        public void OpenFromStageSelect()
        {
            Open(OpenOrigin.StageSelect);
        }

        /// <summary>
        /// ポーズ画面から設定を開き、ポーズ状態を維持する
        /// </summary>
        public void OpenFromPause()
        {
            Open(OpenOrigin.Pause);
        }

        /// <summary>設定画面から、同じ窓を閉じる終了演出を開始する。</summary>
        public void QuitGame()
        {
            if (!IsOpen || !dialogInputScope.CanReceiveInput) return;
            var home = ForestHomeTransition.GetOrCreate();
            if (home == null) return;
            GameSettings.Save();
            previewSource.Stop();
            DialogMotion.Hide(dialog, () => { dialog.SetActive(false); home.QuitGame(); });
        }

        /// <summary>
        /// 呼び出し元を記録し、設定 UI とフォーカスを表示する
        /// </summary>
        /// <param name="origin">設定画面の呼び出し元</param>
        private void Open(OpenOrigin origin)
        {
            if (!isActiveAndEnabled || UiInputScope.IsBlocked)
            {
                return;
            }

            if (IsOpen)
            {
                Debug.LogWarning("設定ダイアログはすでに開いています。", this);
                return;
            }
            openOrigin = origin;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (openOrigin == OpenOrigin.Pause)
            {
                OutGameStateController.Enter(GameFlowState.Paused);
                if (pauseMenu != null)
                {
                    pauseMenu.SetActive(false);
                }
            }
            backLabel.text = openOrigin == OpenOrigin.Pause ? "ポーズへ戻る" : "ステージセレクトへ戻る";
            contextLabel.text = "心地よい音量に調整できます";
            RefreshValues();
            dialog.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(bgmSlider.gameObject);
            }

        }

        /// <summary>
        /// 設定を保存し、呼び出し元の UI と選択状態へ戻る
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (isActiveAndEnabled && !dialogInputScope.CanReceiveInput)
            {
                return;
            }

            GameSettings.Save();
            previewSource.Stop();
            if (isActiveAndEnabled) DialogMotion.Hide(dialog, FinishClose);
            else FinishClose();
        }

        private void FinishClose()
        {
            dialog.SetActive(false);
            if (openOrigin == OpenOrigin.Pause && pauseMenu != null)
            {
                pauseMenu.SetActive(true);
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            }

            previousSelection = null;
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
        }

        /// <summary>
        /// 共通確認ダイアログの結果を待ち、了承された場合だけ既定値へ戻す
        /// </summary>
        public void RestoreDefaults()
        {
            if (!IsOpen || confirmingDefaults || !dialogInputScope.CanReceiveInput)
            {
                return;
            }

            var screenManager = GameServices.Screens;
            if (screenManager == null)
            {
                Debug.LogError("設定の確認ダイアログに必要な ScreenManager がありません。", this);
                return;
            }

            confirmingDefaults = true;
            StartCoroutine(WaitForDefaultsConfirmation(screenManager));
        }

        /// <summary>
        /// 確認 Task の完了状態を監視し、成功時だけ既定値を適用する
        /// </summary>
        /// <param name="screenManager">確認ダイアログを表示する ScreenManager</param>
        /// <returns>確認結果が確定するまで待機する Coroutine</returns>
        private IEnumerator WaitForDefaultsConfirmation(ScreenManager screenManager)
        {
            var confirmation = screenManager.ShowDialogAsync<ConfirmDialog, bool>();
            while (!confirmation.IsCompleted)
            {
                yield return null;
            }

            if (confirmation.IsCanceled)
            {
                confirmingDefaults = false;
                yield break;
            }

            if (confirmation.IsFaulted)
            {
                Debug.LogError($"設定の確認処理に失敗しました: {confirmation.Exception}", this);
                confirmingDefaults = false;
                yield break;
            }

            if (confirmation.Result && this != null && IsOpen)
            {
                GameSettings.RestoreDefaults();
            }

            confirmingDefaults = false;
        }

        /// <summary>
        /// GameSettings の値をスライダー、ラベル、試聴音へ反映する
        /// </summary>
        private void RefreshValues()
        {
            bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
            seSlider.SetValueWithoutNotify(GameSettings.SeVolume);
            ambientSlider.SetValueWithoutNotify(GameSettings.AmbientVolume);
            ambientValue.text = Mathf.RoundToInt(GameSettings.AmbientVolume * PercentPerUnitVolume) + "%";
            foreach (var toggle in dialog.GetComponentsInChildren<VolumeMuteToggle>(true))
            {
                toggle.Synchronize();
            }
            bgmValue.text = Mathf.RoundToInt(GameSettings.BgmVolume * PercentPerUnitVolume) + "%";
            seValue.text = Mathf.RoundToInt(GameSettings.SeVolume * PercentPerUnitVolume) + "%";
            previewSource.volume = GameSettings.SeVolume;
        }

        /// <summary>
        /// 設定ダイアログが動作に必要とする Inspector 参照を検証する
        /// </summary>
        /// <returns>必須参照がすべて設定されている場合は true</returns>
        private bool ValidateReferences()
        {
            if (ambientSlider == null || ambientValue == null)
            {
                Debug.LogError("SettingsDialog の環境音スライダーと数値を設定してください。", this);
                return false;
            }
            var missingReferences = SettingsDialogAlgorithm.GetMissingReferences(
                dialog,
                dialogInputScope,
                bgmSlider,
                seSlider,
                bgmValue,
                seValue,
                backLabel,
                contextLabel,
                previewSource);

            if (missingReferences.Count > 0)
            {
                var missingReferenceNames = string.Join(", ", missingReferences);
                Debug.LogError($"SettingsDialogController の UI 参照が不足しています: {missingReferenceNames}", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// アプリがバックグラウンドへ移るとき設定を保存する
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                GameSettings.Save();
            }
        }

        /// <summary>
        /// アプリ終了時に設定を保存する
        /// </summary>
        private void OnApplicationQuit()
        {
            GameSettings.Save();
        }
    }
}
