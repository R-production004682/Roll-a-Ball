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
    }
}
