using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>
    /// Windows ビルドと出力先の選択・確認を行う Editor 専用ウィンドウ
    /// </summary>
    public sealed class WindowsBuildWindow : EditorWindow
    {
        private const string StylePath = "Assets/Editor/WindowsBuildWindow.uss";

        [SerializeField] private string outputDirectory;
        [SerializeField] private bool developmentBuild;
        private TextField outputField;
        private VisualElement configuration;
        private VisualElement sceneList;
        private VisualElement buildActions;
        private HelpBox status;
        private bool buildQueued;
        private bool runAfterBuild;

        /// <summary>
        /// プロジェクト別の出力先設定に使用するキーを返す
        /// </summary>
        private static string PreferencePrefix => $"RollABall.WindowsBuild.{Application.dataPath}.";

        /// <summary>
        /// メニューから Windows ビルドウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Roll-a-Ball/Build Windows")]
        public static void Open()
        {
            var window = GetWindow<WindowsBuildWindow>();
            window.titleContent = new GUIContent("Windows Build");
            window.minSize = new Vector2(540f, 400f);
            window.Show();
        }

        /// <summary>
        /// 保存済み設定を読み込み、Editor の状態と Scene List の変更を購読する
        /// </summary>
        private void OnEnable()
        {
            outputDirectory = EditorPrefs.GetString(PreferencePrefix + "Output", WindowsBuildUtility.DefaultOutputDirectory);
            developmentBuild = EditorPrefs.GetBool(PreferencePrefix + "Development", false);
            EditorApplication.update += RefreshAvailability;
            EditorBuildSettings.sceneListChanged += RefreshSceneList;
        }

        /// <summary>
        /// ウィンドウ設定を保存し、購読と未実行のビルド予約を解除する
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetString(PreferencePrefix + "Output", outputDirectory);
            EditorPrefs.SetBool(PreferencePrefix + "Development", developmentBuild);
            EditorApplication.update -= RefreshAvailability;
            EditorBuildSettings.sceneListChanged -= RefreshSceneList;
            EditorApplication.delayCall -= ExecuteBuild;
            buildQueued = false;
        }

        /// <summary>
        /// ビルド設定・対象シーン・実行ボタン・結果表示を構築する
        /// </summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("windows-build");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styleSheet != null && !rootVisualElement.styleSheets.Contains(styleSheet))
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            var scroll = new ScrollView();
            rootVisualElement.Add(scroll);
            var title = new Label("Windows 64bit / 1920 × 1080");
            title.AddToClassList("build-title");
            scroll.Add(title);

            configuration = new VisualElement();
            scroll.Add(configuration);
            var outputRow = new VisualElement();
            outputRow.AddToClassList("build-row");
            configuration.Add(outputRow);
            outputField = new TextField("出力先") { value = outputDirectory, isReadOnly = true };
            outputField.AddToClassList("output-field");
            outputRow.Add(outputField);
            outputRow.Add(new Button(ChooseOutputDirectory) { text = "参照…" });
            configuration.Add(new Button(OpenOutputDirectory) { text = "出力フォルダーを開く" });
            configuration.Add(new Button(WebGLBuildWindow.Open) { text = "WebGL のビルド・公開・Discord 通知を開く" });

            var development = new Toggle("Development Build") { value = developmentBuild };
            development.tooltip = "開発用のビルドにします。通常の配布ではオフにしてください。";
            development.RegisterValueChangedCallback(OnDevelopmentChanged);
            configuration.Add(development);

            var sceneTitle = new Label("ビルド対象（Build Profiles の共通 Scene List）");
            sceneTitle.AddToClassList("section-title");
            scroll.Add(sceneTitle);
            sceneList = new VisualElement();
            scroll.Add(sceneList);
            RefreshSceneList();

            buildActions = new VisualElement();
            buildActions.AddToClassList("build-row");
            scroll.Add(buildActions);
            var buildButton = new Button(BuildOnly) { text = "Windows ビルド" };
            buildButton.AddToClassList("build-button");
            buildActions.Add(buildButton);
            var runButton = new Button(BuildAndRun) { text = "ビルドして起動" };
            runButton.AddToClassList("build-button");
            buildActions.Add(runButton);

            status = new HelpBox("出力先を指定して「Windows ビルド」を押してください。", HelpBoxMessageType.Info);
            scroll.Add(status);
            RefreshAvailability();
        }

        /// <summary>
        /// OS のフォルダー選択画面から出力先を変更する
        /// </summary>
        private void ChooseOutputDirectory()
        {
            var selected = EditorUtility.OpenFolderPanel("Windows ビルドの出力先", outputDirectory, string.Empty);
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            outputDirectory = selected;
            outputField.SetValueWithoutNotify(selected);
        }

        /// <summary>
        /// 生成済みの実行ファイルまたは出力フォルダーを Explorer に表示する
        /// </summary>
        private void OpenOutputDirectory()
        {
            if (!Directory.Exists(outputDirectory))
            {
                SetStatus("出力フォルダーは最初のビルド時に作成されます。", HelpBoxMessageType.Info);
                return;
            }

            var executable = Path.Combine(outputDirectory, WindowsBuildUtility.ExecutableName);
            EditorUtility.RevealInFinder(File.Exists(executable) ? executable : outputDirectory);
        }

        /// <summary>
        /// Development Build の選択を保持する
        /// </summary>
        private void OnDevelopmentChanged(ChangeEvent<bool> change)
        {
            developmentBuild = change.newValue;
        }

        /// <summary>
        /// 有効な Scene List を登録順に表示し、先頭を起動シーンとして案内する
        /// </summary>
        private void RefreshSceneList()
        {
            if (sceneList == null)
            {
                return;
            }

            sceneList.Clear();
            var index = 0;
            foreach (var scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                var suffix = index == 0 ? "（起動シーン）" : string.Empty;
                sceneList.Add(new Label($"{index + 1}. {Path.GetFileNameWithoutExtension(scene.path)}{suffix}")
                {
                    tooltip = scene.path
                });
                index++;
            }

            if (index == 0)
            {
                sceneList.Add(new HelpBox("Scene List にシーンが登録されていません。", HelpBoxMessageType.Warning));
            }
        }

        /// <summary>
        /// 再生やコンパイル中のビルドと、予約中の連打を防ぐ
        /// </summary>
        private void RefreshAvailability()
        {
            if (buildActions == null)
            {
                return;
            }

            var available = !buildQueued && !WindowsBuildUtility.IsBusy;
            buildActions.SetEnabled(available);
            configuration.SetEnabled(available);
        }

        /// <summary>
        /// ビルドのみを予約する
        /// </summary>
        private void BuildOnly()
        {
            QueueBuild(false);
        }

        /// <summary>
        /// ビルド成功後の Player 起動を含めて予約する
        /// </summary>
        private void BuildAndRun()
        {
            QueueBuild(true);
        }

        /// <summary>
        /// 入力を検証し、結果表示を更新してから次の Editor 更新でビルドを実行する
        /// </summary>
        private void QueueBuild(bool autoRun)
        {
            if (buildQueued)
            {
                return;
            }

            if (!WindowsBuildUtility.TryCreateOptions(outputDirectory, developmentBuild, autoRun, out _, out var error))
            {
                SetStatus(error, HelpBoxMessageType.Error);
                return;
            }

            runAfterBuild = autoRun;
            buildQueued = true;
            SetStatus("Windows ビルドを実行しています…", HelpBoxMessageType.Info);
            RefreshAvailability();
            EditorApplication.delayCall += ExecuteBuild;
        }

        /// <summary>
        /// 予約したビルドを実行し、成否と実行ファイルの場所を表示する
        /// </summary>
        private void ExecuteBuild()
        {
            buildQueued = false;
            var report = WindowsBuildUtility.Build(outputDirectory, developmentBuild, runAfterBuild);
            if (report == null)
            {
                SetStatus("ビルドを中止しました。保存確認のキャンセル以外の場合は Console を確認してください。", HelpBoxMessageType.Warning);
            }
            else if (report.summary.result == BuildResult.Succeeded)
            {
                SetStatus($"ビルド完了（{report.summary.totalTime.TotalSeconds:F1} 秒）\n{report.summary.outputPath}", HelpBoxMessageType.Info);
            }
            else
            {
                SetStatus($"ビルド結果: {report.summary.result}。エラー {report.summary.totalErrors} 件。Console を確認してください。", HelpBoxMessageType.Error);
            }

            RefreshAvailability();
        }

        /// <summary>
        /// ビルドの状態や対応が必要な内容を表示する
        /// </summary>
        private void SetStatus(string message, HelpBoxMessageType type)
        {
            status.text = message;
            status.messageType = type;
        }
    }
}
