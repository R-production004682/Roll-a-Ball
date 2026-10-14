using System;
using TMPro;
using UnityEngine;

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
        [SerializeField] private UnityEngine.UI.Image[] stageDots;
        [SerializeField] private TMP_Text resultTime;
        [SerializeField] private TMP_Text resultCoins;
        [SerializeField] private TMP_Text resultWallet;
        private int startingCurrency;
        private double elapsedSeconds;
        private GameManager gameManager;

        /// <summary>
        /// コイン取得前の所持金を記録し、現在のステージを表示する
        /// </summary>
        private void Start()
        {
            gameManager = GameManager.instance;
            startingCurrency = GameDataManager.Currency - (gameManager != null ? gameManager.CollectedCoins : 0);
            var selectedId = StageSelectionContext.SelectedStageId;
            var definition = Array.Find(stages, stage => stage.StageId == selectedId);
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
        /// プレイ中の時間だけを計測し、クリア時に表示を確定する
        /// </summary>
        private void Update()
        {
            if (gameManager == null)
            {
                return;
            }

            if (OutGameStateController.IsPlaying && !gameManager.isStageCompleted)
            {
                elapsedSeconds += Time.deltaTime;
            }

            if (gameManager.isStageCompleted)
            {
                var duration = TimeSpan.FromSeconds(elapsedSeconds);
                resultTime.text = $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
                resultCoins.text = $"+{gameManager.CollectedCoins:N0}";
                resultWallet.text = $"{startingCurrency:N0} → {GameDataManager.Currency:N0}";
                enabled = false;
            }
        }
    }
}
