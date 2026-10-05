using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 指定フォルダーのステージ Prefab 参照をビルドでも利用できるよう保持する
    /// </summary>
    public sealed class StagePrefabCatalog : ScriptableObject
    {
        private static StagePrefabCatalog current;

        [SerializeField, Tooltip("指定フォルダーから Editor が自動収集する一覧です。手動変更は不要です。")]
        private StageEntry[] stages = Array.Empty<StageEntry>();

        /// <summary>
        /// ステージ Prefab が一件以上存在するかを返す
        /// </summary>
        internal static bool HasStagePrefabs => GetEntries().Count > 0;

        /// <summary>
        /// ステージの安定 ID に対応する Prefab を取得する
        /// </summary>
        /// <param name="stageId">stage-1 などのステージ ID</param>
        /// <returns>対応する Prefab。存在しない場合は null</returns>
        internal static GameObject GetStagePrefab(string stageId)
        {
            foreach (var stage in GetEntries())
            {
                if (stageId == $"stage-{stage.stageNumber}")
                {
                    return stage.prefab;
                }
            }

            return null;
        }

        /// <summary>
        /// Prefab の存在を判定し、一件もない場合だけ初期ステージの配置済みデータを許可する
        /// </summary>
        internal static bool IsStageAvailable(string stageId)
        {
            return HasStagePrefabs ? GetStagePrefab(stageId) != null : stageId == GameDataManager.InitialStageId;
        }

        /// <summary>
        /// 解放済みで、Prefab が存在し、前のステージもクリア済みなら開始を許可する
        /// </summary>
        internal static bool CanPlayStage(string stageId, int stageNumber)
        {
            return IsStageAvailable(stageId) && GameDataManager.IsStageUnlocked(stageId) &&
                (stageNumber == 1 || GameDataManager.IsStageCleared($"stage-{stageNumber - 1}"));
        }

        /// <summary>
        /// 自動収集された Prefab の一覧を取得する
        /// </summary>
        internal static IReadOnlyList<StageEntry> GetEntries()
        {
            if (current == null)
            {
                current = Resources.Load<StagePrefabCatalog>(nameof(StagePrefabCatalog));
            }

            return current != null ? current.stages : Array.Empty<StageEntry>();
        }

        /// <summary>
        /// プレイ開始時に前回のカタログ参照を解除する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            current = null;
        }

        /// <summary>
        /// 命名規約から取得したステージ番号と Prefab 参照
        /// </summary>
        [Serializable]
        internal sealed class StageEntry
        {
            public int stageNumber;
            public GameObject prefab;
        }
    }
}
