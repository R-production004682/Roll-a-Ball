using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージの選択、進行状況の表示、アウトゲーム内画面への遷移を担当する
    /// </summary>
    public sealed class StageSelectScreen : ScreenBase
    {
        [SerializeField] private StageDefinition[] stages = Array.Empty<StageDefinition>();
        [SerializeField] private TMP_Text currencyLabel;
        [SerializeField] private TMP_Text stageNumberLabel;
        [SerializeField] private TMP_Text stageNameLabel;
        [SerializeField] private TMP_Text[] clearTimeLabels = new TMP_Text[GameSaveData.MaximumClearTimeCount];
        [SerializeField] private RawImage stagePreview;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button stagePreviewButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button customizeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private RectTransform stageCard;
        [SerializeField, Min(StageSelectScreenConstants.MinimumSelectionPulseDuration)]
        private float selectionPulseDuration = StageSelectScreenConstants.DefaultSelectionPulseDuration;

        private UiInputScope inputScope;
        private int selectedIndex;
        private float selectionPulseElapsed = StageSelectScreenConstants.InactiveSelectionPulseElapsed;

        /// <summary>
        /// 入力対象を取得する
        /// </summary>
        private void Awake()
        {
            inputScope = GetComponent<UiInputScope>();
        }

        /// <summary>
        /// ステージ選択を開き、ゲームデータと操作を購読する
        /// </summary>
        public override void OnOpen(object arg)
        {
            if (!ValidateReferences())
            {
                return;
            }

            selectedIndex = FindSelectedIndex();
            previousButton.onClick.AddListener(SelectPrevious);
            nextButton.onClick.AddListener(SelectNext);
            stagePreviewButton.onClick.AddListener(StartSelectedStage);
            playButton.onClick.AddListener(StartSelectedStage);
            shopButton.onClick.AddListener(OpenShop);
            customizeButton.onClick.AddListener(OpenCustomize);
            settingsButton.onClick.AddListener(OpenSettings);
            GameDataManager.Changed += Refresh;
            Refresh();
            playButton.Select();
        }

        /// <summary>
        /// ステージ選択を閉じ、購読した UI 操作とデータ通知を解除する
        /// </summary>
        public override void OnClose()
        {
            GameDataManager.Changed -= Refresh;
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(SelectPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(SelectNext);
            }

            if (stagePreviewButton != null)
            {
                stagePreviewButton.onClick.RemoveListener(StartSelectedStage);
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(StartSelectedStage);
            }

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OpenShop);
            }

            if (customizeButton != null)
            {
                customizeButton.onClick.RemoveListener(OpenCustomize);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
            }
        }

        /// <summary>
        /// キーボードとマウスホイールによるステージ送り、および選択演出を更新する
        /// </summary>
        private void Update()
        {
            UpdateSelectionPulse();
            if (inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SelectPrevious();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SelectNext();
            }

            var scroll = Input.mouseScrollDelta.y;
            if (scroll > StageSelectScreenConstants.ScrollInputThreshold)
            {
                SelectNext();
            }
            else if (scroll < -StageSelectScreenConstants.ScrollInputThreshold)
            {
                SelectPrevious();
            }
        }

        /// <summary>
        /// 一つ前のステージを選択する
        /// </summary>
        private void SelectPrevious()
        {
            if (!CanChangeSelection())
            {
                return;
            }

            selectedIndex = (selectedIndex - 1 + stages.Length) % stages.Length;
            Refresh();
        }

        /// <summary>
        /// 一つ後のステージを選択する
        /// </summary>
        private void SelectNext()
        {
            if (!CanChangeSelection())
            {
                return;
            }

            selectedIndex = (selectedIndex + 1) % stages.Length;
            Refresh();
        }

        /// <summary>
        /// 選択中で解放済みのステージをゲームシーンへ遷移する
        /// </summary>
        private void StartSelectedStage()
        {
            if (inputScope == null || !inputScope.CanReceiveInput || stages.Length == 0)
            {
                return;
            }

            var stage = stages[selectedIndex];
            if (stage == null || !GameDataManager.IsStageUnlocked(stage.StageId))
            {
                return;
            }

            if (!stage.PlayScene.IsAssigned)
            {
                Debug.LogError($"プレイ用 Scene が未設定です: {stage.StageId}", this);
                return;
            }

            StageSelectionContext.Select(stage.StageId, stage.NextStageId);
            if (!SceneRouter.LoadScene(stage.PlayScene.Path, this))
            {
                Debug.LogError($"ステージ開始に失敗しました: {stage.PlayScene.Path}", this);
            }
        }

        /// <summary>
        /// ショップ Screen へ入れ替える
        /// </summary>
        private void OpenShop()
        {
            ReplaceScreen<ShopScreen>();
        }

        /// <summary>
        /// カスタマイズ Screen へ入れ替える
        /// </summary>
        private void OpenCustomize()
        {
            ReplaceScreen<CustomizationScreen>();
        }

        /// <summary>
        /// 設定ダイアログを表示する
        /// </summary>
        private void OpenSettings()
        {
            if (inputScope == null || !inputScope.CanReceiveInput)
            {
                return;
            }

            if (GameServices.Settings == null)
            {
                Debug.LogError("SettingsDialogHost がないため設定ダイアログを開けません。", this);
                return;
            }

            GameServices.Settings.OpenFromStageSelect();
        }

        /// <summary>
        /// 指定した Screen へ遷移する
        /// </summary>
        /// <typeparam name="T">表示する Screen 型</typeparam>
        private void ReplaceScreen<T>() where T : ScreenBase
        {
            if (inputScope != null && inputScope.CanReceiveInput && GameServices.Screens != null)
            {
                GameServices.Screens.Replace<T>();
            }
        }

        /// <summary>
        /// 選択中のステージ情報と進行データを UI へ反映する
        /// </summary>
        private void Refresh()
        {
            if (stages == null || stages.Length == 0 || selectedIndex < 0 || selectedIndex >= stages.Length)
            {
                return;
            }

            var stage = stages[selectedIndex];
            if (stage == null)
            {
                return;
            }

            var unlocked = GameDataManager.IsStageUnlocked(stage.StageId);
            currencyLabel.text = $"COIN  {GameDataManager.Currency:N0}";
            stageNumberLabel.text = $"STAGE {stage.StageNumber:00}";
            stageNameLabel.text = stage.DisplayName;
            stagePreview.texture = stage.PreviewTexture;
            lockedOverlay.SetActive(!unlocked);
            stagePreviewButton.interactable = unlocked;
            playButton.interactable = unlocked;
            previousButton.interactable = stages.Length > 1;
            nextButton.interactable = stages.Length > 1;

            var clearTimes = GameDataManager.GetTopClearTimes(stage.StageId);
            for (var index = 0; index < clearTimeLabels.Length; index++)
            {
                var clearTimeSeconds = index < clearTimes.Length
                    ? clearTimes[index]
                    : StageSelectScreenConstants.UnrecordedClearTimeSeconds;
                clearTimeLabels[index].text = StageSelectScreenAlgorithm.FormatRankingEntry(
                    index + StageSelectScreenConstants.FirstRank,
                    clearTimeSeconds);
            }

            selectionPulseElapsed = StageSelectScreenConstants.SelectionPulseStartElapsed;
        }

        /// <summary>
        /// 選択変更時のステージカード拡縮演出を更新する
        /// </summary>
        private void UpdateSelectionPulse()
        {
            if (stageCard == null ||
                selectionPulseElapsed < StageSelectScreenConstants.SelectionPulseStartElapsed)
            {
                return;
            }

            selectionPulseElapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(selectionPulseElapsed / selectionPulseDuration);
            var scale = 1f + Mathf.Sin(progress * Mathf.PI) *
                StageSelectScreenConstants.SelectionPulseScaleAmplitude;
            stageCard.localScale = Vector3.one * scale;
            if (progress >= 1f)
            {
                selectionPulseElapsed = StageSelectScreenConstants.InactiveSelectionPulseElapsed;
                stageCard.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 現在のセッションで選んだステージの配列位置を取得する
        /// </summary>
        /// <returns>一致するステージがなければ先頭の位置</returns>
        private int FindSelectedIndex()
        {
            for (var index = 0; index < stages.Length; index++)
            {
                if (stages[index] != null && stages[index].StageId == StageSelectionContext.SelectedStageId)
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>
        /// 現在の入力状態で選択変更を受け付けられるかを返す
        /// </summary>
        /// <returns>選択を変更できる場合は true</returns>
        private bool CanChangeSelection()
        {
            return inputScope != null && inputScope.CanReceiveInput && stages != null && stages.Length > 1;
        }

        /// <summary>
        /// 画面が動作に必要な参照とステージ定義を検証する
        /// </summary>
        /// <returns>参照が揃っている場合は true</returns>
        private bool ValidateReferences()
        {
            var missingReferences = StageSelectScreenAlgorithm.GetMissingReferences(
                inputScope,
                stages,
                currencyLabel,
                stageNumberLabel,
                stageNameLabel,
                clearTimeLabels,
                stagePreview,
                lockedOverlay,
                previousButton,
                nextButton,
                stagePreviewButton,
                playButton,
                shopButton,
                customizeButton,
                settingsButton);
            if (missingReferences.Count > 0)
            {
                Debug.LogError(
                    $"StageSelectScreen の Inspector 参照またはステージ定義が不足しています: {string.Join(", ", missingReferences)}",
                    this);
                return false;
            }

            return true;
        }
    }
}
