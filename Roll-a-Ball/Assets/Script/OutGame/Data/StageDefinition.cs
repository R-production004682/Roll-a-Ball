using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ステージ選択画面が表示と遷移に使用するマスタ定義
    /// </summary>
    [Serializable]
    public sealed class StageDefinition
    {
        [SerializeField, Tooltip("保存データでステージを見分ける値です。他のステージと重ならないようにします。")]
        private string stageId;
        [SerializeField, Tooltip("画面に表示するステージ番号です。")]
        private int stageNumber;
        [SerializeField, Tooltip("画面に表示するステージ名です。")]
        private string displayName;
        [SerializeField, Tooltip("ステージ選択画面に表示する画像です。")]
        private Texture previewTexture;
        [SerializeField, Tooltip("ステージ選択画面から移動するシーンです。")]
        private SceneReference playScene = new SceneReference();
        [SerializeField, Tooltip("クリア時に解放するステージの ID です。最後のステージなど、解放先がなければ空欄にします。")]
        private string nextStageId;

        /// <summary>
        /// ゲームデータに保存する安定 ID を取得する
        /// </summary>
        public string StageId => stageId;

        /// <summary>
        /// 画面に表示するステージ番号を取得する
        /// </summary>
        public int StageNumber => stageNumber;

        /// <summary>
        /// 画面に表示するステージ名を取得する
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// ステージのプレビュー画像を取得する
        /// </summary>
        public Texture PreviewTexture => previewTexture;

        /// <summary>
        /// プレイ用シーンの参照を取得する
        /// </summary>
        public SceneReference PlayScene => playScene;

        /// <summary>
        /// クリア時に解放する次ステージの ID を取得する
        /// </summary>
        public string NextStageId => nextStageId;

        /// <summary>
        /// 検出したステージ番号に既存の表示設定を合わせて定義を作成する
        /// </summary>
        /// <param name="number">Prefab 名から取得したステージ番号</param>
        /// <param name="template">表示名・画像・シーンの設定を引き継ぐ定義</param>
        /// <param name="useDisplayName">同じ番号の既存定義なら表示名を引き継ぐ</param>
        internal static StageDefinition FromPrefab(int number, StageDefinition template, bool useDisplayName)
        {
            return new StageDefinition
            {
                stageId = $"stage-{number}",
                stageNumber = number,
                displayName = useDisplayName ? template.displayName : $"STAGE {number:00}",
                previewTexture = template.previewTexture,
                playScene = template.playScene,
                nextStageId = $"stage-{number + 1}"
            };
        }
    }
}
