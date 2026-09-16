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
        [SerializeField] private string stageId;
        [SerializeField] private int stageNumber;
        [SerializeField] private string displayName;
        [SerializeField] private string difficulty;
        [SerializeField] private Texture previewTexture;
        [SerializeField] private SceneReference playScene = new SceneReference();
        [SerializeField] private string nextStageId;

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
        /// 画面に表示する難易度を取得する
        /// </summary>
        public string Difficulty => difficulty;

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
    }
}
