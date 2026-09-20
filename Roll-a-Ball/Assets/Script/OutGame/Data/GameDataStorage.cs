using System;
using System.IO;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// バージョン付きゲームデータを PlayerPrefs へ読み書きする
    /// </summary>
    internal static class GameDataStorage
    {
        private const string SaveDataKey = "RollABall.GameData.SaveData";
        private const string LegacyCurrencyKey = "HasCoin";
        private const string LegacySaveFileName = "roll-a-ball-save.json";

        /// <summary>
        /// ゲームデータを保存する PlayerPrefs のキー
        /// </summary>
        internal static string SavePath => $"PlayerPrefs:{SaveDataKey}";

        /// <summary>
        /// PlayerPrefs に新しい保存データが存在するか
        /// </summary>
        internal static bool HasPlayerPrefsData => PlayerPrefs.HasKey(SaveDataKey);

        /// <summary>
        /// 保存ファイルを読み込み、未作成と読み込み失敗を区別して返す
        /// </summary>
        /// <param name="saveData">読み込んだデータ。未作成または失敗時は null</param>
        /// <param name="error">読み込み失敗時の理由</param>
        /// <returns>保存ファイルがない場合は Missing、正常読込時は Loaded、それ以外は Failed</returns>
        internal static GameDataLoadStatus Load(out GameSaveData saveData, out string error)
        {
            saveData = null;
            error = string.Empty;

            if (PlayerPrefs.HasKey(SaveDataKey))
            {
                return LoadFromPlayerPrefs(out saveData, out error);
            }

            var legacySavePath = GetLegacySavePath();
            if (File.Exists(legacySavePath))
            {
                return LoadFromLegacyFile(legacySavePath, out saveData, out error);
            }

            if (PlayerPrefs.HasKey(LegacyCurrencyKey))
            {
                saveData = new GameSaveData
                {
                    version = GameSaveData.CurrentVersion,
                    currency = Mathf.Max(0, PlayerPrefs.GetInt(LegacyCurrencyKey, 0))
                };
                saveData.stages.Add(new StageProgressData
                {
                    stageId = "stage-1",
                    isUnlocked = true
                });
                return GameDataLoadStatus.Loaded;
            }

            return GameDataLoadStatus.Missing;
        }

        /// <summary>
        /// PlayerPrefs の JSON を読み込み、未作成と読み込み失敗を区別して返す
        /// </summary>
        /// <param name="saveData">読み込んだデータ。失敗時は null</param>
        /// <param name="error">読み込み失敗時の理由</param>
        /// <returns>正常読込時は Loaded、それ以外は Failed</returns>
        private static GameDataLoadStatus LoadFromPlayerPrefs(out GameSaveData saveData, out string error)
        {
            saveData = null;
            error = string.Empty;

            var json = PlayerPrefs.GetString(SaveDataKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = $"{SavePath}: 保存データが空です。";
                return GameDataLoadStatus.Failed;
            }

            try
            {
                saveData = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception exception)
            {
                error = $"{SavePath}: {exception.Message}";
                return GameDataLoadStatus.Failed;
            }

            if (saveData == null)
            {
                error = $"{SavePath}: JSON から保存データを復元できません。";
                return GameDataLoadStatus.Failed;
            }

            return GameDataLoadStatus.Loaded;
        }

        /// <summary>
        /// 移行前の JSON ファイルを読み込み、PlayerPrefs へ移行可能なデータとして返す
        /// </summary>
        /// <param name="savePath">移行前 JSON ファイルの絶対パス</param>
        /// <param name="saveData">読み込んだデータ。失敗時は null</param>
        /// <param name="error">読み込み失敗時の理由</param>
        /// <returns>正常読込時は Loaded、それ以外は Failed</returns>
        private static GameDataLoadStatus LoadFromLegacyFile(
            string savePath,
            out GameSaveData saveData,
            out string error)
        {
            saveData = null;
            error = string.Empty;

            try
            {
                var json = File.ReadAllText(savePath);
                saveData = JsonUtility.FromJson<GameSaveData>(json);
                if (saveData == null)
                {
                    error = $"{savePath}: 保存データが空か、JSON から復元できません。";
                    return GameDataLoadStatus.Failed;
                }

                return GameDataLoadStatus.Loaded;
            }
            catch (Exception exception)
            {
                // 移行前ファイルの読み込みと JSON 解析の失敗を境界で返し、元ファイルを変更しない。
                error = $"{savePath}: {exception.Message}";
                return GameDataLoadStatus.Failed;
            }
        }

        /// <summary>
        /// ゲームデータを JSON 化して PlayerPrefs へ保存する
        /// </summary>
        /// <param name="saveData">保存するゲームデータ</param>
        /// <param name="error">保存失敗時の理由</param>
        /// <returns>PlayerPrefs への保存に成功した場合は true</returns>
        internal static bool TrySave(GameSaveData saveData, out string error)
        {
            error = string.Empty;

            if (saveData == null)
            {
                error = "保存データが空です。";
                return false;
            }

            try
            {
                PlayerPrefs.SetString(SaveDataKey, JsonUtility.ToJson(saveData));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                // PlayerPrefs の保存失敗を呼び出し元へ返し、ランタイムの共有データは呼び出し元で更新しない。
                error = $"{SavePath}: {exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// 移行前の JSON 保存ファイルのパスを返す
        /// </summary>
        /// <returns>移行前ゲームデータ JSON の絶対パス</returns>
        private static string GetLegacySavePath()
        {
            return Path.Combine(Application.persistentDataPath, LegacySaveFileName);
        }
    }

    /// <summary>
    /// ゲームデータ保存ファイルの読み込み結果
    /// </summary>
    internal enum GameDataLoadStatus
    {
        Missing,
        Loaded,
        Failed
    }
}
