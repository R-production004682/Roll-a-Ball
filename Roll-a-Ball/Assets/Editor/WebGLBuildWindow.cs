using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>
    /// WebGL ビルド・GitHub Pages 公開・Discord ロール通知を操作する
    /// </summary>
    public sealed class WebGLBuildWindow : EditorWindow
    {
        private const string DefaultRepository = "R-production004682/Roll-a-Ball";
        private const string DefaultRoleId = "1538587867746668554";
        [SerializeField] private string repository;
        [SerializeField] private string roleId;
        private VisualElement actions;
        private VisualElement configuration;
        private HelpBox status;
        private bool queued;
        private bool publishAfterBuild;
        private string lastPublishStatus;

        /// <summary>
        /// プロジェクトごとのローカル設定キーを返す
        /// </summary>
        private static string PreferencePrefix => $"RollABall.WebGL.{Application.dataPath}.";

        /// <summary>
        /// WebGL のビルドと公開ウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Roll-a-Ball/Build WebGL and Publish")]
        public static void Open()
        {
            var window = GetWindow<WebGLBuildWindow>();
            window.titleContent = new GUIContent("WebGL Build & Publish");
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        /// <summary>
        /// 保存済みの通知先設定を読み込み、進捗確認を購読する
        /// </summary>
        private void OnEnable()
        {
            repository = EditorPrefs.GetString(PreferencePrefix + "Repository", DefaultRepository);
            roleId = EditorPrefs.GetString(PreferencePrefix + "Role", DefaultRoleId);
            EditorApplication.update += Refresh;
        }

        /// <summary>
        /// 設定とイベントの寿命を揃え、未実行のビルド予約を解除する
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetString(PreferencePrefix + "Repository", repository);
            EditorPrefs.SetString(PreferencePrefix + "Role", roleId);
            EditorApplication.update -= Refresh;
            EditorApplication.delayCall -= ExecuteBuild;
        }

        /// <summary>
        /// 公開先・通知先・ビルド操作を構築する
        /// </summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("windows-build");
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/WindowsBuildWindow.uss");
            if (style != null && !rootVisualElement.styleSheets.Contains(style))
            {
                rootVisualElement.styleSheets.Add(style);
            }

            var scroll = new ScrollView();
            rootVisualElement.Add(scroll);
            var title = new Label("WebGL / 1920 × 1080 / GitHub Pages");
            title.AddToClassList("build-title");
            scroll.Add(title);
            scroll.Add(new HelpBox("ビルド成功後に最新の WebGL を公開し、到達確認後に @roll-a-ball へ URL を通知します。", HelpBoxMessageType.Info));
            configuration = new VisualElement();
            scroll.Add(configuration);
            var repositoryField = new TextField("GitHub リポジトリ") { value = repository };
            repositoryField.RegisterValueChangedCallback(OnRepositoryChanged);
            configuration.Add(repositoryField);
            var roleField = new TextField("roll-a-ball ロール ID") { value = roleId };
            roleField.RegisterValueChangedCallback(OnRoleChanged);
            configuration.Add(roleField);
            configuration.Add(new Label("出力先: " + WebGLBuildUtility.OutputDirectory));
            scroll.Add(new HelpBox("Webhook は UserSettings に暗号化して保存します。初期設定・必要なツールは Docs/WebGLPublishing.md を確認してください。", HelpBoxMessageType.Info));
            actions = new VisualElement();
            actions.AddToClassList("build-row");
            scroll.Add(actions);
            var buildButton = new Button(BuildOnly) { text = "WebGL ビルドのみ" };
            buildButton.AddToClassList("build-button");
            actions.Add(buildButton);
            var publishButton = new Button(BuildAndPublish) { text = "WebGL ビルド・公開・通知" };
            publishButton.AddToClassList("build-button");
            actions.Add(publishButton);
            status = new HelpBox("公開時は GitHub CLI のログインが必要です。", HelpBoxMessageType.Info);
            scroll.Add(status);
            Refresh();
        }

        /// <summary>
        /// 公開リポジトリの変更を保持する
        /// </summary>
        private void OnRepositoryChanged(ChangeEvent<string> change)
        {
            repository = change.newValue.Trim();
        }

        /// <summary>
        /// メンション対象のロール ID を保持する
        /// </summary>
        private void OnRoleChanged(ChangeEvent<string> change)
        {
            roleId = change.newValue.Trim();
        }

        /// <summary>
        /// ローカルビルドを予約する
        /// </summary>
        private void BuildOnly()
        {
            QueueBuild(false);
        }

        /// <summary>
        /// 成功後の公開と通知を含むビルドを予約する
        /// </summary>
        private void BuildAndPublish()
        {
            QueueBuild(true);
        }

        /// <summary>
        /// 必要な設定を確認し、次の Editor 更新でビルドを開始する
        /// </summary>
        private void QueueBuild(bool publish)
        {
            if (queued || WebGLPublisher.IsPublishing)
            {
                return;
            }

            if (!WebGLBuildUtility.CanBuild(out var error) ||
                (publish && !WebGLPublisher.CanPublish(repository, roleId, out error)))
            {
                SetStatus(error, HelpBoxMessageType.Error);
                return;
            }

            publishAfterBuild = publish;
            queued = true;
            SetStatus("WebGL をビルドしています…", HelpBoxMessageType.Info);
            Refresh();
            EditorApplication.delayCall += ExecuteBuild;
        }

        /// <summary>
        /// 成功したビルドだけを公開処理へ渡す
        /// </summary>
        private void ExecuteBuild()
        {
            queued = false;
            var report = WebGLBuildUtility.Build();
            if (report == null || report.summary.result != BuildResult.Succeeded)
            {
                SetStatus("ビルドが中止または失敗しました。公開・通知は行いません。Console を確認してください。", HelpBoxMessageType.Error);
                return;
            }

            if (publishAfterBuild)
            {
                lastPublishStatus = null;
                WebGLPublisher.Start(repository, roleId);
                Refresh();
                return;
            }

            SetStatus("WebGL ビルド完了: " + WebGLBuildUtility.OutputDirectory, HelpBoxMessageType.Info);
        }

        /// <summary>
        /// 操作可能な状態とバックグラウンド公開の結果を反映する
        /// </summary>
        private void Refresh()
        {
            if (actions == null)
            {
                return;
            }

            var available = !queued && !WindowsBuildUtility.IsBusy && !WebGLPublisher.IsPublishing;
            actions.SetEnabled(available);
            configuration.SetEnabled(available);
            if (!string.IsNullOrEmpty(WebGLPublisher.Status) && lastPublishStatus != WebGLPublisher.Status)
            {
                lastPublishStatus = WebGLPublisher.Status;
                SetStatus(lastPublishStatus, WebGLPublisher.Failed ? HelpBoxMessageType.Error : HelpBoxMessageType.Info);
            }
        }

        /// <summary>
        /// ビルドまたは公開の結果を表示する
        /// </summary>
        private void SetStatus(string message, HelpBoxMessageType type)
        {
            status.text = message;
            status.messageType = type;
        }
    }
}
