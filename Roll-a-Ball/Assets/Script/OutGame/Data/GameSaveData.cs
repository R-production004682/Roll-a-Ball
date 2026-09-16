using System;
using System.Collections.Generic;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// JSON ファイルに保存するゲーム進行データ
    /// </summary>
    [Serializable]
    internal sealed class GameSaveData
    {
        public const int CurrentVersion = 1;
        public const int MaximumClearTimeCount = 3;

        public int version;
        public int currency;
        public List<StageProgressData> stages = new List<StageProgressData>();
        public List<string> purchasedItemIds = new List<string>();
    }

    /// <summary>
    /// 一つのステージの解放・クリア記録
    /// </summary>
    [Serializable]
    internal sealed class StageProgressData
    {
        public string stageId;
        public bool isUnlocked;
        public bool isCleared;
        public List<ClearTimeRecord> clearTimes = new List<ClearTimeRecord>();
    }

    /// <summary>
    /// 一回分のステージクリア時間を秒単位で保持する
    /// </summary>
    [Serializable]
    internal sealed class ClearTimeRecord
    {
        public float seconds;
    }
}
