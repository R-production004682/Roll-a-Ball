using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ名・旅の進行表示とリザルトの実際のプレイ情報を表示する
    /// </summary>
    public sealed class StageSessionPresentation : MonoBehaviour
    {
        [SerializeField] private StageDefinition[] stages;
        [SerializeField] private TMP_Text stageName;
        [SerializeField] private TMP_Text journeyLabel;
        [SerializeField] private Image[] stageDots;
        [SerializeField] private TMP_Text resultCoins;
        [SerializeField] private TMP_Text resultWallet;
        private int startingCurrency;
        private GameManager gameManager;

        /// <summary>
        /// コイン取得前の所持金を記録し、現在のステージを表示する
        /// </summary>
        private void Start()
        {
            gameManager = GameManager.instance;
            if (gameManager == null || stageName == null || journeyLabel == null ||
                resultCoins == null || resultWallet == null || stages == null ||
                stageDots == null || Array.Exists(stageDots, dot => dot == null))
            {
                Debug.LogError("StageSessionPresentation: GameManager、ステージ定義、ステージ名・進行表示・リザルトの UI 参照を設定してください。プレイ情報の表示更新を無効化します。", this);
                enabled = false;
                return;
            }

            startingCurrency = GameDataManager.Currency - gameManager.CollectedCoins;

            var selectedId = StageSelectionContext.SelectedStageId;
            var definition = Array.Find(stages, stage => stage != null && stage.StageId == selectedId);
            if (definition == null)
            {
                Debug.LogWarning($"StageSessionPresentation: {selectedId} のステージ定義がないため STAGE 01 を表示します。Stages の設定を確認してください。", this);
            }
            var number = definition != null ? definition.StageNumber : 1;

            stageName.text = definition != null ? definition.DisplayName : $"STAGE {number:00}";
            journeyLabel.text = $"STAGE {number:00}";
            for (var index = 0; index < stageDots.Length; index++)
            {
                stageDots[index].color = index + 1 == number
                    ? new Color32(215, 174, 85, 255)
                    : new Color32(246, 241, 223, 160);
            }
        }

        /// <summary>
        /// クリア時に表示を確定する
        /// </summary>
        private void Update()
        {
            if (gameManager == null)
            {
                return;
            }

            if (gameManager.isStageCompleted)
            {
                resultCoins.text = $"+{gameManager.CollectedCoins:N0}";
                resultWallet.text = $"{startingCurrency:N0} → {GameDataManager.Currency:N0}";
                enabled = false;
            }
        }
    }
}
