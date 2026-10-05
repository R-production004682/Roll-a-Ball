using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Roll_a_Ball.OutGame.Editor
{
    /// <summary>
    /// Stage_01 形式の Prefab を収集し、再生・ビルド前に参照を同期する
    /// </summary>
    [InitializeOnLoad]
    public sealed class StagePrefabCatalogBuilder : AssetPostprocessor, IPreprocessBuildWithReport
    {
        private const string StageFolder = "Assets/Prefabs/Stage";
        private const string CatalogPath = "Assets/Resources/StagePrefabCatalog.asset";

        /// <summary>
        /// 初回インポート後とプレイ開始前の同期を登録する
        /// </summary>
        static StagePrefabCatalogBuilder()
        {
            EditorApplication.delayCall += Synchronize;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// ビルド前処理の実行順序を返す
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// ビルドへ含めるステージ Prefab の参照を最新化する
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            Synchronize();
        }

        /// <summary>
        /// Prefab の追加・削除・移動時に同期を予約する
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths)
                .Any(path => path == StageFolder || path.StartsWith(StageFolder + "/", StringComparison.Ordinal)))
            {
                EditorApplication.delayCall -= Synchronize;
                EditorApplication.delayCall += Synchronize;
            }
        }

        /// <summary>
        /// ドメイン再読み込みを無効にしていても再生前にカタログを同期する
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Synchronize();
            }
        }

        /// <summary>
        /// 指定フォルダー直下の命名規約に合う Prefab だけを番号順に保存する
        /// </summary>
        public static void Synchronize()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var prefabs = AssetDatabase.IsValidFolder(StageFolder)
                ? AssetDatabase.FindAssets("t:Prefab", new[] { StageFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => Path.GetDirectoryName(path)?.Replace('\\', '/') == StageFolder)
                    .Select(path => (path, number: GetStageNumber(Path.GetFileNameWithoutExtension(path))))
                    .Where(stage => stage.number > 0)
                    .OrderBy(stage => stage.number)
                    .ToArray()
                : Array.Empty<(string path, int number)>();

            var catalog = AssetDatabase.LoadAssetAtPath<StagePrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                catalog = ScriptableObject.CreateInstance<StagePrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("stages");
            entries.arraySize = prefabs.Length;
            for (var index = 0; index < prefabs.Length; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("stageNumber").intValue = prefabs[index].number;
                entry.FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[index].path);
            }

            if (serializedCatalog.ApplyModifiedPropertiesWithoutUndo())
            {
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }

        /// <summary>
        /// Stage_01、Stage_02 の命名規約に一致する場合だけ番号を取得する
        /// </summary>
        private static int GetStageNumber(string name)
        {
            const string prefix = "Stage_";
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !int.TryParse(name.Substring(prefix.Length), out var number) || number <= 0)
            {
                return 0;
            }

            return name == $"{prefix}{number:00}" ? number : 0;
        }
    }
}
