using System;
using System.Linq;
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
        [SerializeField, Tooltip("ステージの表示名・画像・遷移先を設定します。Prefab がある場合、一覧は自動収集され、同じステージ番号の表示設定を使用します。")]
        private StageDefinition[] stages = Array.Empty<StageDefinition>();
        [SerializeField, Tooltip("所持コイン数を表示するテキストです。")]
        private TMP_Text currencyLabel;
        [SerializeField, Tooltip("選択中のステージ番号を表示するテキストです。")]
        private TMP_Text stageNumberLabel;
        [SerializeField, Tooltip("選択中のステージ名を表示するテキストです。")]
        private TMP_Text stageNameLabel;
        [SerializeField, Tooltip("選択中のステージのクリア時間を表示するテキストです。3件分を登録します。")]
        private TMP_Text[] clearTimeLabels = new TMP_Text[GameSaveData.MaximumClearTimeCount];
        [SerializeField, Tooltip("選択中のステージ画像を表示します。")]
        private RawImage stagePreview;
        [SerializeField, Tooltip("未解放のステージを選んだときに表示する案内です。")]
        private GameObject lockedOverlay;
        [SerializeField, Tooltip("前のステージを選ぶボタンです。")]
        private Button previousButton;
        [SerializeField, Tooltip("次のステージを選ぶボタンです。")]
        private Button nextButton;
        [SerializeField, Tooltip("選択中のステージを画像から開始するボタンです。")]
        private Button stagePreviewButton;
        [SerializeField, Tooltip("選択中のステージを開始するボタンです。")]
        private Button playButton;
        [SerializeField, Tooltip("ショップ画面を開くボタンです。")]
        private Button shopButton;
        [SerializeField, Tooltip("カスタマイズ画面を開くボタンです。")]
        private Button customizeButton;
        [SerializeField, Tooltip("設定画面を開くボタンです。")]
        private Button settingsButton;
        [SerializeField, Tooltip("選択中のステージを強調する表示です。")]
        private RectTransform stageCard;
        [SerializeField, Min(StageSelectScreenConstants.MinimumSelectionPulseDuration),
            Tooltip("ステージ選択時にカードが拡大して元に戻るまでの秒数です。大きくするとゆっくり動きます。")]
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
        /// シーン破棄時にもゲームデータ通知の購読を解除する
        /// </summary>
        private void OnDestroy()
        {
            GameDataManager.Changed -= Refresh;
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

            RefreshStageDefinitions();
            StageSelectionContext.UnlockAvailableStages(stages);
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
            if (stages != null && selectedIndex >= 0 && selectedIndex < stages.Length && stages[selectedIndex] != null)
            {
                StageSelectionContext.RememberStageSelectId(stages[selectedIndex].StageId);
            }

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
            if (stage == null || !StagePrefabCatalog.CanPlayStage(stage.StageId, stage.StageNumber))
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
            ReplaceScreen<CustomizationScreen>(CustomizationScreen.ReturnDestination.StageSelect);
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
        /// <param name="arg">表示する Screen に渡す引数</param>
        private void ReplaceScreen<T>(object arg = null) where T : ScreenBase
        {
            if (inputScope != null && inputScope.CanReceiveInput && GameServices.Screens != null)
            {
                GameServices.Screens.Replace<T>(arg);
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

            var unlocked = StagePrefabCatalog.CanPlayStage(stage.StageId, stage.StageNumber);
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
            var stageId = StageSelectionContext.ConsumeStageSelectId();
            for (var index = 0; index < stages.Length; index++)
            {
                if (stages[index] != null && stages[index].StageId == stageId)
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>
        /// Prefab がある場合は実在するステージを番号順に表示し、未登録の番号にも定義を補う
        /// </summary>
        private void RefreshStageDefinitions()
        {
            var entries = StagePrefabCatalog.GetEntries();
            if (entries.Count == 0)
            {
                return;
            }

            var configuredStages = stages;
            stages = entries.Select(entry =>
            {
                var template = configuredStages.FirstOrDefault(stage => stage.StageNumber == entry.stageNumber);
                return StageDefinition.FromPrefab(entry.stageNumber, template ?? configuredStages[0], template != null);
            }).ToArray();
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
