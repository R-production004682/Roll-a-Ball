using System;
using System.IO;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// バージョン付きゲームデータを JSON ファイルへ読み書きする
    /// </summary>
    internal static class GameDataStorage
    {
        private const string SaveFileName = "roll-a-ball-save.json";
        private const string TemporaryFileSuffix = ".tmp";
        private const string BackupFileSuffix = ".backup";

        /// <summary>
        /// ゲームデータの保存先 JSON ファイル
        /// </summary>
        internal static string SavePath => GetSavePath();

        /// <summary>
        /// 保存ファイルを読み込み、未作成と読み込み失敗を区別して返す
        /// </summary>
        /// <param name="saveData">読み込んだデータ。未作成または失敗時は null</param>
        /// <param name="error">読み込み失敗時の理由</param>
        /// <returns>保存ファイルがない場合は Missing、正常読込時は Loaded、それ以外は Failed</returns>
        internal static GameDataLoadStatus Load(out GameSaveData saveData, out string error)
        {
            return Load(GetSavePath(), out saveData, out error);
        }

        /// <summary>
        /// 指定ファイルを読み込み、未作成と読み込み失敗を区別して返す
        /// </summary>
        /// <param name="savePath">読み込む JSON ファイルの絶対パス</param>
        /// <param name="saveData">読み込んだデータ。未作成または失敗時は null</param>
        /// <param name="error">読み込み失敗時の理由</param>
        /// <returns>保存ファイルがない場合は Missing、正常読込時は Loaded、それ以外は Failed</returns>
        internal static GameDataLoadStatus Load(
            string savePath,
            out GameSaveData saveData,
            out string error)
        {
            saveData = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(savePath))
            {
                error = "保存先のパスが空です。";
                return GameDataLoadStatus.Failed;
            }

            if (!File.Exists(savePath))
            {
                return GameDataLoadStatus.Missing;
            }

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
                // ファイル読み込みと JSON 解析の失敗を境界で返し、元ファイルを上書きしない。
                error = $"{savePath}: {exception.Message}";
                return GameDataLoadStatus.Failed;
            }
        }

        /// <summary>
        /// 新しい JSON を一時ファイルへ書き、既存保存を保ったまま置き換える
        /// </summary>
        /// <param name="saveData">保存するゲームデータ</param>
        /// <param name="error">保存失敗時の理由</param>
        /// <returns>ファイルの置き換えに成功した場合は true</returns>
        internal static bool TrySave(GameSaveData saveData, out string error)
        {
            return TrySave(saveData, GetSavePath(), out error);
        }

        /// <summary>
        /// 指定ファイルへ JSON を安全に置き換える
        /// </summary>
        /// <param name="saveData">保存するゲームデータ</param>
        /// <param name="savePath">保存先 JSON ファイルの絶対パス</param>
        /// <param name="error">保存失敗時の理由</param>
        /// <returns>ファイルの置き換えに成功した場合は true</returns>
        internal static bool TrySave(GameSaveData saveData, string savePath, out string error)
        {
            error = string.Empty;

            if (saveData == null || string.IsNullOrWhiteSpace(savePath))
            {
                error = "保存データまたは保存先のパスが空です。";
                return false;
            }

            var temporaryPath = savePath + TemporaryFileSuffix;
            var backupPath = savePath + BackupFileSuffix;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(saveData, true));

                if (File.Exists(savePath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    File.Replace(temporaryPath, savePath, backupPath);
                    return true;
                }

                File.Move(temporaryPath, savePath);
                return true;
            }
            catch (Exception exception)
            {
                // ストレージ API の失敗を呼び出し元へ伝え、既存ファイルは直接削除しない。
                error = $"{savePath}: {exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// Unity の永続データ領域にある保存ファイルのパスを返す
        /// </summary>
        /// <returns>ゲームデータ JSON の絶対パス</returns>
        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
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
