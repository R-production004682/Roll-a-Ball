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
    /// GitHub Pages で配信できる WebGL Player を生成する
    /// </summary>
    public static class WebGLBuildUtility
    {
        /// <summary>
        /// WebGL 専用のビルド出力先を返す
        /// </summary>
        public static string OutputDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "WebGL");

        /// <summary>
        /// ビルド対象と WebGL モジュールの準備を確認する
        /// </summary>
        public static bool CanBuild(out string error)
        {
            error = string.Empty;
            if (WindowsBuildUtility.IsBusy || WebGLPublisher.IsPublishing)
            {
                error = "再生・コンパイル・インポート・ビルド・公開の終了後に実行してください。";
                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                error = "Unity Hub で、この Editor に Web Build Support を追加してください。";
                return false;
            }

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (scenes.Length == 0 || scenes.Any(scene => AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null))
            {
                error = "Build Profiles の共通 Scene List に、有効なシーンを登録してください。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 1920×1080・Gzip フォールバック付きの WebGL ビルドを実行する
        /// </summary>
        public static BuildReport Build()
        {
            if (!CanBuild(out var error))
            {
                Debug.LogError($"WebGL ビルドを開始できません: {error}");
                return null;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return null;
            }

            PlayerSettings.defaultWebScreenWidth = WindowsBuildUtility.ScreenWidth;
            PlayerSettings.defaultWebScreenHeight = WindowsBuildUtility.ScreenHeight;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.template = "PROJECT:RollABall";
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(OutputDirectory);
            return BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.DetailedBuildReport
            });
        }

        /// <summary>
        /// CLI からビルドし、失敗時は非ゼロの終了コードを返す
        /// </summary>
        public static void BuildFromCommandLine()
        {
            var report = Build();
            if (report == null || report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"WebGL ビルド完了: {OutputDirectory}");
        }
    }
}
