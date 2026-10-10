using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>
    /// Windows 64bit・1920×1080 のビルド設定と実行をまとめる
    /// </summary>
    public static class WindowsBuildUtility
    {
        public const int ScreenWidth = 1920;
        public const int ScreenHeight = 1080;
        public const string ExecutableName = "Roll-a-Ball.exe";

        /// <summary>
        /// このプロジェクトの標準ビルド出力先を返す
        /// </summary>
        public static string DefaultOutputDirectory => Path.Combine(
            Path.GetDirectoryName(Application.dataPath), "Builds", "Windows");

        /// <summary>
        /// 再生・コンパイル・インポート・ビルド中で操作を待つ必要があるかを返す
        /// </summary>
        public static bool IsBusy => EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer;

        /// <summary>
        /// 出力先と有効な Scene List を検証し、Windows Player のビルド設定を作る
        /// </summary>
        /// <param name="outputDirectory">実行ファイルを出力する絶対ディレクトリパス</param>
        /// <param name="development">Development Build を有効にするか</param>
        /// <param name="autoRun">成功後に Player を起動するか</param>
        /// <param name="options">検証済みのビルド設定</param>
        /// <param name="error">ビルドできない理由</param>
        /// <returns>ビルドの準備ができた場合は true</returns>
        public static bool TryCreateOptions(string outputDirectory, bool development, bool autoRun,
            out BuildPlayerOptions options, out string error)
        {
            options = default;
            error = string.Empty;
            if (IsBusy)
            {
                error = "再生・コンパイル・インポート・ビルドの終了後に実行してください。";
                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                error = "Unity Hub で、この Editor に Windows Build Support を追加してください。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(outputDirectory) ||
                outputDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                outputDirectory.Contains('*') || outputDirectory.Contains('?') ||
                !Path.IsPathRooted(outputDirectory))
            {
                error = "出力先を「参照」から選択してください。";
                return false;
            }

            var directory = Path.GetFullPath(outputDirectory);
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (IsSameOrChildDirectory(directory, projectRoot) &&
                !IsSameOrChildDirectory(directory, Path.Combine(projectRoot, "Builds")) &&
                !IsSameOrChildDirectory(directory, Path.Combine(projectRoot, "Build")))
            {
                error = "プロジェクト内の出力先は Build または Builds フォルダー内にしてください。";
                return false;
            }

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                error = "Build Profiles の Scene List に、ビルドするシーンを登録してください。";
                return false;
            }

            var missingScenes = scenes.Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null).ToArray();
            if (missingScenes.Length > 0)
            {
                error = "Scene List に存在しないシーンがあります。Build Profiles で修正してください。\n" +
                    string.Join("\n", missingScenes);
                return false;
            }

            var flags = BuildOptions.DetailedBuildReport;
            if (development)
            {
                flags |= BuildOptions.Development;
            }

            if (autoRun)
            {
                flags |= BuildOptions.AutoRunPlayer;
            }

            options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(directory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = flags
            };
            return true;
        }

        /// <summary>
        /// 未保存シーンの扱いを確認してから、1920×1080 の Windows Player をビルドする
        /// </summary>
        /// <param name="outputDirectory">実行ファイルを出力する絶対ディレクトリパス</param>
        /// <param name="development">Development Build を有効にするか</param>
        /// <param name="autoRun">成功後に Player を起動するか</param>
        /// <returns>ビルド結果。検証失敗または保存確認のキャンセル時は null</returns>
        public static BuildReport Build(string outputDirectory, bool development, bool autoRun)
        {
            if (!TryCreateOptions(outputDirectory, development, autoRun, out var options, out var error))
            {
                Debug.LogError($"Windows ビルドを開始できません: {error}");
                return null;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return null;
            }

            PlayerSettings.defaultScreenWidth = ScreenWidth;
            PlayerSettings.defaultScreenHeight = ScreenHeight;
            PlayerSettings.defaultIsNativeResolution = false;
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(outputDirectory);
            return BuildPipeline.BuildPlayer(options);
        }

        /// <summary>
        /// 正規化したパスが指定ディレクトリ自身またはその配下にあるかを判定する
        /// </summary>
        private static bool IsSameOrChildDirectory(string path, string parent)
        {
            var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(normalizedPath, normalizedParent, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
