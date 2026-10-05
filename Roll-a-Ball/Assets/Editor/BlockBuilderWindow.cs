using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>
    /// Scene View の一人称操作で Prefab をセルへ配置する Editor 専用ツール
    /// </summary>
    public sealed class BlockBuilderWindow : EditorWindow
    {
        private const string RootPrefix = "BlockBuilder_v1|";
        private const string RootSettingsPrefix = "__BlockBuilderSettings_v1|";
        private const string BlockPrefix = "BB1|";
        private const string ChunkPrefix = "BBChunk|";
        private const string MeshGroupPrefix = "Mesh-";
        private const string DerivedMeshPrefix = "BBMesh|";
        private const int MaximumCellsPerBlock = 4096;
        private const float ReachInCells = 128f;
        private static bool creatingRepairWorker;
        private bool backgroundWorker;
        [SerializeField] private GameObject placementRoot;
        [SerializeField] private string placementRootGlobalId;
        private Preferences preferences;
        private readonly Dictionary<Vector3Int, List<Block>> cells = new();
        private readonly List<Block> blocks = new();
        private readonly Dictionary<string, string> chunkSignatures = new();
        private readonly HashSet<KeyCode> keys = new();
        private readonly HashSet<GameObject> selectedForChunking = new();
        private readonly HashSet<Block> rayVisited = new();
        private SceneView activeView;
        private SceneView pendingStartView;
        private double pendingStartDeadline;
        private CameraSnapshot cameraSnapshot;
        private Vector3 cameraPosition;
        private Vector2 angles;
        private Vector2 pendingLook;
        private readonly SceneCursorCapture cursorCapture = new();
        private int captureControl;
        private bool cacheDirty = true;
        private bool rebuilding;
        private bool recordDerivedUndo = true;
        private bool placing;
        private bool modeKeyHeld;
        private int strokeGroup = -1;
        private double previousUpdate;
        private double nextPlacement;
        private float grid = 1f;
        private int chunkSize = 16;
        private bool mergeColliders;
        private bool rootValid;
        private string rootValidationMessage;
        private Vector3Int cachedDimensions;
        private readonly GameObject[] slotPrefabs = new GameObject[9];
        private readonly ObjectField[] slotFields = new ObjectField[9];
        private DropdownField rotationField;
        private bool hasCandidate;
        private Vector3Int candidate;
        private Vector3Int aimedCell;
        private Block aimedBlock;
        private Vector2Int selectedChunk;
        private HelpBox modeIndicator;
        private Label statistics;
        private Label colliderWorkflowStatus;
        private Button beginChunkSelectionButton;
        private Button confirmChunkButton;
        private Button unchunkButton;
        private SceneView selectionView;
        private bool previousSelectionWantsMouseMove;
        private Block hoveredSelectionBlock;
        private bool selectingChunkBlocks;
        private string status = "専用ルートを作成、または以前作成したルートを指定してください";

        /// <summary>
        /// ウィンドウを閉じた後の Undo とシーン再読込でも派生 Collider を同期する
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterBackgroundRepair()
        {
            Undo.undoRedoPerformed -= QueueBackgroundRepair;
            Undo.undoRedoPerformed += QueueBackgroundRepair;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            QueueBackgroundRepair();
        }

        /// <summary>
        /// シーンを開いた後の整合確認を予約する
        /// </summary>
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            QueueBackgroundRepair();
        }

        /// <summary>
        /// Undo 処理が終了した後に一度だけ整合確認を実行する
        /// </summary>
        private static void QueueBackgroundRepair()
        {
            EditorApplication.delayCall -= RepairWithoutWindow;
            EditorApplication.delayCall += RepairWithoutWindow;
        }

        /// <summary>
        /// 開いているウィンドウが担当していないルートの派生データを修復する
        /// </summary>
        private static void RepairWithoutWindow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            var handledRoots = new HashSet<GameObject>(Resources.FindObjectsOfTypeAll<BlockBuilderWindow>()
                .Where(w => !w.backgroundWorker).Select(w => w.placementRoot));
            var roots = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(s => s.isLoaded).SelectMany(s => s.GetRootGameObjects())
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject)
                .Where(r => HasRootSettings(r) && !handledRoots.Contains(r)).ToArray();
            if (roots.Length == 0)
            {
                return;
            }
            creatingRepairWorker = true;
            var worker = CreateInstance<BlockBuilderWindow>();
            creatingRepairWorker = false;
            foreach (var root in roots)
            {
                worker.placementRoot = root;
                worker.recordDerivedUndo = false;
                worker.chunkSignatures.Clear();
                worker.RebuildCache();
            }
            DestroyImmediate(worker);
        }

        [Serializable]
        private sealed class Preferences
        {
            public string[] prefabGuids = new string[9];
            public float grid = 1f;
            public int chunkSize = 16;
            public float interval = 0.45f;
            public float speed = 10f;
            public float sensitivity = 0.15f;
            public Color guideColor = new(0.2f, 1f, 0.6f, 1f);
            public int slot;
            public bool fit = true;
            public int movementSpeedRevision;
            public int rotationSteps;
        }

        private sealed class Block
        {
            public GameObject gameObject;
            public Bounds bounds;
            public Collider[] colliders;
            public string originalFlags;
            public Vector3Int min;
            public Vector3Int max;
            public bool canMerge;
            public bool canMergeMesh;
            public string colliderGroupId;
        }

        private sealed class CameraSnapshot
        {
            public Vector3 pivot;
            public Quaternion rotation;
            public Quaternion lastSceneViewRotation;
            public float size;
            public bool orthographic;
            public bool in2D;
            public bool rotationLocked;
            public bool wantsMouseMove;
            public bool easing;
            public bool acceleration;
            public float fieldOfView;
        }

        /// <summary>
        /// Windows の実カーソルを中央周辺に制限し、読み取った移動を中央へ戻して消費する
        /// </summary>
        private sealed class SceneCursorCapture
        {
            public bool active;
            private Vector2Int center;
            private float pixelsPerPoint;
#if UNITY_EDITOR_WIN
            private NativeRect previousClip;
            private NativeRect clip;
            private IntPtr ownerWindow;
            private int hideCalls;

            [StructLayout(LayoutKind.Sequential)]
            private struct NativePoint
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct NativeRect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            /// <summary>
            /// デスクトップ上の実カーソル位置を取得する
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetCursorPos(out NativePoint point);

            /// <summary>
            /// デスクトップ上の実カーソル位置を変更する
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetCursorPos(int x, int y);

            /// <summary>
            /// 現在のカーソル移動制限を保存する
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetClipCursor(out NativeRect rect);

            /// <summary>
            /// カーソルの移動可能範囲を設定する
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ClipCursor(ref NativeRect rect);

            /// <summary>
            /// カーソルの移動制限を解除する
            /// </summary>
            [DllImport("user32.dll", EntryPoint = "ClipCursor", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ReleaseClip(IntPtr rect);

            /// <summary>
            /// 操作対象の最前面ウィンドウを取得する
            /// </summary>
            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            /// <summary>
            /// カーソルの表示カウンターを一段変更する
            /// </summary>
            [DllImport("user32.dll")]
            private static extern int ShowCursor([MarshalAs(UnmanagedType.Bool)] bool show);
#endif

            /// <summary>
            /// Scene View 中央へ移動して隣接パネルに出ない範囲で捕捉を開始する
            /// </summary>
            public bool Begin(Vector2 screenCenter, Vector2 viewportSize, float scale)
            {
#if UNITY_EDITOR_WIN
                if (!EditorApplication.isFocused || active || !GetClipCursor(out previousClip))
                {
                    return false;
                }
                pixelsPerPoint = scale;
                center = Vector2Int.RoundToInt(screenCenter * scale);
                var radius = Vector2Int.FloorToInt(Vector2.Min(viewportSize * 0.25f, Vector2.one * 128f) * scale);
                if (radius.x < 4 || radius.y < 4)
                {
                    return false;
                }
                clip = new NativeRect { left = center.x - radius.x, top = center.y - radius.y,
                    right = center.x + radius.x + 1, bottom = center.y + radius.y + 1 };
                ownerWindow = GetForegroundWindow();
                if (!ClipCursor(ref clip))
                {
                    return false;
                }
                active = true;
                if (!SetCursorPos(center.x, center.y))
                {
                    End();
                    return false;
                }
                // 自分で減らした回数だけ終了時に戻し、OS の表示カウンターを維持する
                for (var i = 0; i < 32; i++)
                {
                    hideCalls++;
                    if (ShowCursor(false) < 0)
                    {
                        return true;
                    }
                }
                End();
#endif
                return false;
            }

            /// <summary>
            /// 捕捉開始時のウィンドウが最前面にあるか確認する
            /// </summary>
            public bool HasFocus()
            {
#if UNITY_EDITOR_WIN
                return active && GetForegroundWindow() == ownerWindow;
#else
                return false;
#endif
            }

            /// <summary>
            /// 実カーソルの中央からの変位を一度だけ読み取り、ワープ差分を入力から除く
            /// </summary>
            public bool Sample(out Vector2 delta)
            {
                delta = Vector2.zero;
#if UNITY_EDITOR_WIN
                if (!HasFocus() || !GetCursorPos(out var point))
                {
                    return false;
                }
                if (!GetClipCursor(out var current) || current.left != clip.left || current.top != clip.top
                    || current.right != clip.right || current.bottom != clip.bottom)
                {
                    return false;
                }
                delta = new Vector2(point.x - center.x, point.y - center.y) / pixelsPerPoint;
                return delta == Vector2.zero || SetCursorPos(center.x, center.y);
#else
                return false;
#endif
            }

            /// <summary>
            /// ウィンドウ移動や表示倍率変更で照準位置が変わっていないか確認する
            /// </summary>
            public bool Matches(Vector2 screenCenter, float scale)
            {
                return active && Mathf.Approximately(pixelsPerPoint, scale)
                    && Vector2Int.RoundToInt(screenCenter * scale) == center;
            }

            /// <summary>
            /// 自分が設定した移動制限とカーソル表示カウンターを元に戻す
            /// </summary>
            public void End()
            {
#if UNITY_EDITOR_WIN
                if (!active)
                {
                    return;
                }
                active = false;
                if (GetClipCursor(out var current) && current.left == clip.left && current.top == clip.top
                    && current.right == clip.right && current.bottom == clip.bottom)
                {
                    if (GetForegroundWindow() == ownerWindow)
                    {
                        if (!ClipCursor(ref previousClip))
                        {
                            ReleaseClip(IntPtr.Zero);
                        }
                    }
                    else
                    {
                        ReleaseClip(IntPtr.Zero);
                    }
                }
                while (hideCalls > 0)
                {
                    ShowCursor(true);
                    hideCalls--;
                }
#endif
            }
        }

        /// <summary>
        /// Tools メニューから配置ツールを開く
        /// </summary>
        [MenuItem("Tools/Block Builder")]
        public static void Open()
        {
            GetWindow<BlockBuilderWindow>("Block Builder");
        }

        /// <summary>
        /// プロジェクトごとの設定と Editor イベントを読み込む
        /// </summary>
        private void OnEnable()
        {
            backgroundWorker = creatingRepairWorker;
            if (backgroundWorker)
            {
                return;
            }
            // Scene 参照が未復元でも保存済み ID を消さず、先に復元を試みる
            RestorePlacementRootReference();
            preferences = JsonUtility.FromJson<Preferences>(EditorPrefs.GetString(PreferenceKey(), "{}")) ?? new Preferences();
            if (preferences.prefabGuids == null || preferences.prefabGuids.Length != 9)
            {
                preferences.prefabGuids = new string[9];
            }
            if (preferences.movementSpeedRevision < 1)
            {
                preferences.speed = Mathf.Max(10f, ClampSetting(preferences.speed, 0.1f, 30f));
                preferences.movementSpeedRevision = 1;
                SavePreferences();
            }
            else
            {
                preferences.speed = ClampSetting(preferences.speed, 0.1f, 30f);
            }
            preferences.slot = Mathf.Clamp(preferences.slot, 0, 8);
            preferences.rotationSteps = (preferences.rotationSteps % 4 + 4) % 4;
            ReloadPrefabs();
            SceneView.beforeSceneGui += BeforeSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
            EditorApplication.update += Tick;
            EditorApplication.hierarchyChanged += Invalidate;
            EditorApplication.projectChanged += ReloadPrefabs;
            EditorApplication.focusChanged += OnApplicationFocus;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneClosing += OnPlacementRootSceneClosing;
            EditorApplication.quitting += StopAllModes;
            AssemblyReloadEvents.beforeAssemblyReload += StopAllModes;
            Undo.undoRedoPerformed += OnUndoRedo;
            Undo.postprocessModifications += OnModifications;
            cacheDirty = true;
        }

        /// <summary>
        /// 入力を解放して購読を解除する
        /// </summary>
        private void OnDisable()
        {
            if (backgroundWorker)
            {
                return;
            }
            StopAllModes();
            SavePreferences();
            SceneView.beforeSceneGui -= BeforeSceneGUI;
            SceneView.duringSceneGui -= DuringSceneGUI;
            EditorApplication.update -= Tick;
            EditorApplication.hierarchyChanged -= Invalidate;
            EditorApplication.projectChanged -= ReloadPrefabs;
            EditorApplication.focusChanged -= OnApplicationFocus;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneClosing -= OnPlacementRootSceneClosing;
            EditorApplication.quitting -= StopAllModes;
            AssemblyReloadEvents.beforeAssemblyReload -= StopAllModes;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.postprocessModifications -= OnModifications;
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnWindowKeyDown, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// ウィンドウへフォーカスが移ったときにショートカットを受け取れる状態にする
        /// </summary>
        private void OnFocus()
        {
            rootVisualElement.focusable = true;
            if (rootVisualElement.panel != null && rootVisualElement.focusController.focusedElement == null)
            {
                rootVisualElement.Focus();
            }
        }

        /// <summary>
        /// 同じプロジェクトを再起動しても利用できる設定キーを返す
        /// </summary>
        private static string PreferenceKey()
        {
            return "RollABall.BlockBuilder.v1." + Application.dataPath;
        }

        /// <summary>
        /// Prefab の GUID と操作設定を保存する
        /// </summary>
        private void SavePreferences()
        {
            if (preferences != null)
            {
                EditorPrefs.SetString(PreferenceKey(), JsonUtility.ToJson(preferences));
            }
        }

        /// <summary>
        /// UI Toolkit で Prefab 登録欄と操作設定を構築する
        /// </summary>
        public void CreateGUI()
        {
            rootValid = ReadRootSettings();
            var panel = new ScrollView();
            rootVisualElement.Clear();
            rootVisualElement.Add(panel);
            modeIndicator = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            panel.Add(modeIndicator);
            UpdateModeIndicator();
            panel.Add(new HelpBox("Scene View またはこのウィンドウで B 開始 / B・Esc 終了 / Alt で終了して通常操作\nWASD 移動（初期速度 10 セル / 秒）\n右クリック配置（長押し連続）・左クリック破壊\n中クリック / F: スポイト・R / Shift+R: 90度回転\n1～9 / ホイールでスロット変更", HelpBoxMessageType.Info));
            var rootField = new ObjectField("配置ルート") { objectType = typeof(GameObject), allowSceneObjects = true, value = placementRoot };
            rootField.RegisterValueChangedCallback(OnRootChanged);
            panel.Add(rootField);
            var rootOptions = new VisualElement();
            var gridField = new FloatField("新規ルートのグリッド") { value = preferences.grid, isDelayed = true };
            gridField.RegisterValueChangedCallback(OnGridChanged);
            rootOptions.Add(gridField);
            var chunkField = new IntegerField("新規ルートのチャンク幅") { value = preferences.chunkSize, isDelayed = true };
            chunkField.RegisterValueChangedCallback(OnChunkSizeChanged);
            rootOptions.Add(chunkField);
            panel.Add(rootOptions);
            panel.Add(new Button(CreateRoot) { text = "専用ルートを新規作成" });
            var colliderWorkflow = new Foldout { text = "Collider チャンク化（選択式）", value = true };
            colliderWorkflow.Add(new HelpBox("選択したブロックの表示メッシュを、凹凸を保つ MeshCollider にまとめます。固定ステージ用です。Prefab や Renderer は変更せず、操作は Undo できます。", HelpBoxMessageType.Info));
            beginChunkSelectionButton = new Button(BeginChunkSelection)
            {
                text = "選択されたブロックをまとめてチャンク化する",
                name = "beginChunkSelectionButton"
            };
            colliderWorkflow.Add(beginChunkSelectionButton);
            colliderWorkflowStatus = new Label();
            colliderWorkflow.Add(colliderWorkflowStatus);
            confirmChunkButton = new Button(ConfirmChunkSelection) { text = "選択を確定してチャンク化" };
            colliderWorkflow.Add(confirmChunkButton);
            unchunkButton = new Button(UnchunkSelectedBlocks) { text = "選択したブロックを個別 Collider に戻す" };
            colliderWorkflow.Add(unchunkButton);
            colliderWorkflow.Add(new Button(ShortenManagedNames)
            {
                text = "既存のブロック・Collider の名前を短くする",
                name = "shortenManagedNamesButton"
            });
            panel.Add(colliderWorkflow);
            UpdateColliderWorkflowUI();
            var fit = new Toggle("縦横比を保ってセルに収める") { value = preferences.fit };
            fit.RegisterValueChangedCallback(OnFitChanged);
            panel.Add(fit);
            rotationField = new DropdownField("配置回転（Y）", new List<string> { "0°", "90°", "180°", "270°" }, preferences.rotationSteps)
            {
                name = "placementRotationField"
            };
            rotationField.RegisterValueChangedCallback(OnPlacementRotationChanged);
            panel.Add(rotationField);
            var interval = new FloatField("連続配置間隔（秒）") { value = preferences.interval, isDelayed = true };
            interval.RegisterValueChangedCallback(OnIntervalChanged);
            panel.Add(interval);
            var speed = new FloatField("移動速度（セル / 秒）") { value = preferences.speed, isDelayed = true };
            speed.RegisterValueChangedCallback(OnSpeedChanged);
            panel.Add(speed);
            var sensitivity = new FloatField("マウス感度") { value = preferences.sensitivity, isDelayed = true };
            sensitivity.RegisterValueChangedCallback(OnSensitivityChanged);
            panel.Add(sensitivity);
            var color = new ColorField("配置候補の色") { value = preferences.guideColor };
            color.RegisterValueChangedCallback(OnColorChanged);
            panel.Add(color);
            for (var i = 0; i < 9; i++)
            {
                var field = new ObjectField($"スロット {i + 1}") { objectType = typeof(GameObject), allowSceneObjects = false, value = SlotPrefab(i), userData = i };
                field.RegisterValueChangedCallback(OnPrefabChanged);
                slotFields[i] = field;
                panel.Add(field);
            }
            var chunk = new Vector2IntField("選択チャンク（X / Z）") { value = selectedChunk };
            chunk.RegisterValueChangedCallback(OnChunkChanged);
            panel.Add(chunk);
            panel.Add(new Button(SelectAimedChunk) { text = "照準のチャンクを選択" });
            statistics = new Label();
            panel.Add(statistics);
            UpdateStatistics();
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnWindowKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnWindowKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.focusable = true;
            if (focusedWindow == this && rootVisualElement.panel != null
                && rootVisualElement.focusController.focusedElement == null)
            {
                rootVisualElement.Focus();
            }
        }

        /// <summary>
        /// Block Builder ウィンドウにフォーカスがある場合の B キーで Scene View を開始する
        /// </summary>
        private void OnWindowKeyDown(KeyDownEvent current)
        {
            if (focusedWindow != this || current.keyCode != KeyCode.B || current.modifiers != EventModifiers.None
                || EditorApplication.isPlayingOrWillChangePlaymode || cameraSnapshot != null || pendingStartView != null
                || IsTextFieldFocused())
            {
                return;
            }
            current.StopPropagation();
            var view = SceneView.lastActiveSceneView;
            if (view == null)
            {
                status = "配置モードを開始する Scene View がありません";
                UpdateStatistics();
                return;
            }
            RequestPlacementMode(view);
            view.Focus();
            view.Repaint();
        }

        /// <summary>
        /// 照準と同じ GUI 座標系で捕捉を開始するため描画コールバックへ開始を予約する
        /// </summary>
        private void RequestPlacementMode(SceneView view)
        {
            if (selectingChunkBlocks)
            {
                CancelChunkSelection();
            }
            modeKeyHeld = true;
            pendingStartView = view;
            pendingStartDeadline = EditorApplication.timeSinceStartup + 2d;
            view.Repaint();
        }

        /// <summary>
        /// テキスト入力中の B キーを配置モードのショートカットにしない
        /// </summary>
        private bool IsTextFieldFocused()
        {
            if (EditorGUIUtility.editingTextField)
            {
                return true;
            }
            var focusedElement = rootVisualElement.focusController.focusedElement as VisualElement;
            while (focusedElement != null && focusedElement != rootVisualElement)
            {
                if (focusedElement is TextField || focusedElement is FloatField || focusedElement is IntegerField
                    || focusedElement is ObjectField || focusedElement is Vector2IntField)
                {
                    return true;
                }
                focusedElement = focusedElement.parent;
            }
            return false;
        }

        /// <summary>
        /// 有限の設定値を許容範囲に収める
        /// </summary>
        private static float ClampSetting(float value, float min, float max)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? min : Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// 配置モードの状態をウィンドウへ明示し Scene View を再描画する
        /// </summary>
        private void UpdateModeIndicator()
        {
            if (modeIndicator == null)
            {
                return;
            }
            modeIndicator.text = selectingChunkBlocks
                ? "現在のモード: Collider チャンク対象選択（Scene View でブロックをクリックし、選択後に確定）"
                : cameraSnapshot == null
                    ? "現在のモード: 通常の Scene View（このウィンドウまたは Scene View で B を押して開始）"
                    : "現在のモード: FPS ブロック配置（WASD 移動・右クリック配置・左クリック破壊）";
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 配置ルートを切り替えてキャッシュを破棄する
        /// </summary>
        private void OnRootChanged(ChangeEvent<Object> change)
        {
            SetPlacementRoot(change.newValue as GameObject);
        }

        /// <summary>
        /// 配置ルートと復元用 ID を更新し操作状態と表示欄を同期する
        /// </summary>
        private void SetPlacementRoot(GameObject root)
        {
            StopMode();
            CancelChunkSelection();
            placementRoot = root;
            UpdatePlacementRootGlobalId();
            chunkSignatures.Clear();
            Invalidate();
            CreateGUI();
        }

        /// <summary>
        /// 配置ルートがあるシーンを閉じる前に参照を None へ戻す
        /// </summary>
        private void OnPlacementRootSceneClosing(Scene scene, bool removingScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || placementRoot == null || placementRoot.scene != scene)
            {
                return;
            }

            SetPlacementRoot(null);
            RebuildCache();
        }

        /// <summary>
        /// 新規ルートのセル幅を保存する
        /// </summary>
        private void OnGridChanged(ChangeEvent<float> change)
        {
            preferences.grid = ClampSetting(change.newValue, 0.05f, 100f);
            ((FloatField)change.target).SetValueWithoutNotify(preferences.grid);
            SavePreferences();
            Invalidate();
        }

        /// <summary>
        /// 新規ルートのチャンク幅を保存する
        /// </summary>
        private void OnChunkSizeChanged(ChangeEvent<int> change)
        {
            preferences.chunkSize = Mathf.Clamp(change.newValue, 1, 64);
            ((IntegerField)change.target).SetValueWithoutNotify(preferences.chunkSize);
            SavePreferences();
            Invalidate();
        }

        /// <summary>
        /// Scene View で対象ブロックを選ぶモードへ切り替える
        /// </summary>
        private void BeginChunkSelection()
        {
            var view = selectionView != null ? selectionView : activeView != null ? activeView : SceneView.lastActiveSceneView;
            if (selectingChunkBlocks)
            {
                CancelChunkSelection();
            }
            else
            {
                RestoreSelectionView();
            }
            StopMode();
            rootValid = ReadRootSettings();
            if (!rootValid || view == null)
            {
                status = view == null ? "選択に使う Scene View がありません" : rootValidationMessage;
                UpdateColliderWorkflowUI();
                UpdateStatistics();
                return;
            }
            RebuildCache();
            selectedForChunking.Clear();
            hoveredSelectionBlock = null;
            selectionView = view;
            previousSelectionWantsMouseMove = selectionView.wantsMouseMove;
            selectionView.wantsMouseMove = true;
            selectingChunkBlocks = true;
            status = "Scene View でブロックをクリックして選択し、ウィンドウで確定してください";
            selectionView.Focus();
            UpdateModeIndicator();
            UpdateColliderWorkflowUI();
            UpdateStatistics();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Scene View で選んだブロックを一つの Undo 操作でチャンク化する
        /// </summary>
        private void ConfirmChunkSelection()
        {
            var selected = blocks.Where(block => selectedForChunking.Contains(block.gameObject)).ToArray();
            if (!selectingChunkBlocks || selected.Length == 0 || selected.Any(block => !block.canMergeMesh))
            {
                status = selected.Any(block => !block.canMergeMesh)
                    ? "有効な静的 MeshRenderer がない、Trigger・Rigidbody 付き、または Collider を統合できないブロックが含まれています"
                    : "チャンク化するブロックを Scene View で選択してください";
                UpdateColliderWorkflowUI();
                UpdateStatistics();
                return;
            }

            if (selected.Select(block => block.gameObject.layer).Distinct().Count() != 1)
            {
                status = "衝突レイヤーを維持するため、同じ Layer のブロックだけを選択してください";
                UpdateStatistics();
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("選択ブロックをチャンク化");
            EnsureRootSettings();
            var groupId = NextColliderGroupId();
            foreach (var block in selected)
            {
                Undo.RecordObject(block.gameObject, "Group block colliders");
                block.gameObject.name = SetColliderGroupId(block.gameObject.name, groupId);
                PrefabUtility.RecordPrefabInstancePropertyModifications(block.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(placementRoot.scene);
            selectingChunkBlocks = false;
            RestoreSelectionView();
            selectedForChunking.Clear();
            hoveredSelectionBlock = null;
            RebuildCache();
            Undo.CollapseUndoOperations(undoGroup);
            var generated = placementRoot.transform.Cast<Transform>()
                .FirstOrDefault(child => ManagedName(child.name, ChunkPrefix) == ChunkPrefix + "Group|" + groupId);
            status = generated != null
                ? $"{selected.Length} 個のブロックを形状に沿った MeshCollider にまとめました（Undo で戻せます）"
                : "MeshCollider を生成できなかったため、個別 Collider を維持しました";
            UpdateModeIndicator();
            UpdateColliderWorkflowUI();
            UpdateStatistics();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 選択中ブロックを個別 Collider に戻し残りのグループを再構築する
        /// </summary>
        private void UnchunkSelectedBlocks()
        {
            var selected = blocks.Where(block => selectedForChunking.Contains(block.gameObject)
                && !string.IsNullOrEmpty(block.colliderGroupId)).ToArray();
            if (selected.Length == 0)
            {
                status = "Scene View でチャンク化済みブロックを選択してください";
                UpdateColliderWorkflowUI();
                UpdateStatistics();
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("選択ブロックを個別 Collider に戻す");
            foreach (var block in selected)
            {
                Undo.RecordObject(block.gameObject, "Ungroup block colliders");
                block.gameObject.name = SetColliderGroupId(block.gameObject.name, null);
                PrefabUtility.RecordPrefabInstancePropertyModifications(block.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(placementRoot.scene);
            selectedForChunking.Clear();
            RebuildCache();
            Undo.CollapseUndoOperations(undoGroup);
            status = $"{selected.Length} 個のブロックを個別 Collider に戻しました";
            UpdateColliderWorkflowUI();
            UpdateStatistics();
        }

        /// <summary>
        /// 未確定の選択を破棄して通常モードへ戻る
        /// </summary>
        private void CancelChunkSelection()
        {
            selectingChunkBlocks = false;
            RestoreSelectionView();
            selectedForChunking.Clear();
            hoveredSelectionBlock = null;
            status = "ブロック選択をキャンセルしました";
            UpdateModeIndicator();
            UpdateColliderWorkflowUI();
            UpdateStatistics();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 選択モード開始前の Scene View のマウス移動設定を復元する
        /// </summary>
        private void RestoreSelectionView()
        {
            if (selectionView != null)
            {
                selectionView.wantsMouseMove = previousSelectionWantsMouseMove;
                selectionView.Repaint();
            }
            selectionView = null;
        }

        /// <summary>
        /// ブロック名へチャンクグループ識別子を設定または除去する
        /// </summary>
        private static string SetColliderGroupId(string blockName, string groupId)
        {
            var baseName = CompactBlockName(blockName);
            // 空の Group は明示的な個別 Collider を表し、旧ルートでも再統合しない
            return baseName + "|Group=" + groupId;
        }

        /// <summary>
        /// Prefab 名と復元用の Collider フラグだけを残して管理ブロック名を短くする
        /// </summary>
        private static string CompactBlockName(string blockName)
        {
            var managed = ManagedName(blockName, BlockPrefix);
            if (managed == null)
            {
                return blockName;
            }
            var parts = managed.Split('|');
            var marker = blockName.LastIndexOf("|" + BlockPrefix, StringComparison.Ordinal);
            var label = parts.Length > 2 && !parts[2].StartsWith("Group=", StringComparison.Ordinal)
                ? parts[2] : marker >= 0 ? blockName.Substring(0, marker) : "Block";
            return label + "|" + BlockPrefix + parts[1];
        }

        /// <summary>
        /// 配置ルート内で重複しない短い MeshCollider グループ番号を発行する
        /// </summary>
        private string NextColliderGroupId()
        {
            var used = new HashSet<string>(placementRoot.transform.Cast<Transform>()
                .Select(child => ReadColliderGroupId(child.name)
                    ?? ManagedName(child.name, ChunkPrefix)?.Replace(ChunkPrefix + "Group|", string.Empty)), StringComparer.Ordinal);
            var number = 1;
            while (used.Contains(MeshGroupPrefix + number.ToString("D3", CultureInfo.InvariantCulture)))
            {
                number++;
            }
            return MeshGroupPrefix + number.ToString("D3", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 既存グループの対応と Collider の形状を保ちながら長い名前を Undo 対応で整理する
        /// </summary>
        private void ShortenManagedNames()
        {
            StopMode();
            CancelChunkSelection();
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null
                || !ReadRootSettings())
            {
                return;
            }
            RebuildCache();
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Block Builder の名前を短くする");
            var groups = blocks.Select(block => block.colliderGroupId).Where(id => !string.IsNullOrEmpty(id))
                .Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var groupNames = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < groups.Length; i++)
            {
                var prefix = groups[i].StartsWith(MeshGroupPrefix, StringComparison.Ordinal) ? MeshGroupPrefix : "Group-";
                groupNames.Add(groups[i], prefix + (i + 1).ToString("D3", CultureInfo.InvariantCulture));
            }
            var renamed = 0;
            foreach (var block in blocks)
            {
                var name = block.colliderGroupId == null ? CompactBlockName(block.gameObject.name)
                    : SetColliderGroupId(block.gameObject.name, block.colliderGroupId.Length == 0
                        ? string.Empty : groupNames[block.colliderGroupId]);
                renamed += RenameManagedObject(block.gameObject, name) ? 1 : 0;
            }
            foreach (Transform child in placementRoot.transform)
            {
                var key = ManagedName(child.name, ChunkPrefix);
                if (key == null)
                {
                    continue;
                }
                var groupPrefix = ChunkPrefix + "Group|";
                if (key.StartsWith(groupPrefix, StringComparison.Ordinal)
                    && groupNames.TryGetValue(key.Substring(groupPrefix.Length), out var groupId))
                {
                    key = groupPrefix + groupId;
                }
                renamed += RenameManagedObject(child.gameObject, key) ? 1 : 0;
                foreach (var collider in child.GetComponents<MeshCollider>())
                {
                    var mesh = collider.sharedMesh;
                    if (mesh != null && !AssetDatabase.Contains(mesh)
                        && mesh.name.StartsWith(DerivedMeshPrefix, StringComparison.Ordinal))
                    {
                        var meshName = DerivedMeshPrefix + key.Substring(ChunkPrefix.Length).Replace("Group|", string.Empty);
                        if (mesh.name != meshName)
                        {
                            Undo.RecordObject(mesh, "Shorten collision mesh name");
                            mesh.name = meshName;
                            EditorUtility.SetDirty(mesh);
                        }
                    }
                }
            }
            chunkSignatures.Clear();
            RebuildCache();
            Undo.CollapseUndoOperations(undoGroup);
            status = $"{renamed} 個のブロック・Collider の名前を整理しました（Undo で戻せます）";
            UpdateStatistics();
        }

        /// <summary>
        /// 名前が異なる管理オブジェクトだけを Undo と Prefab 差分へ記録する
        /// </summary>
        private static bool RenameManagedObject(GameObject target, string name)
        {
            if (target.name == name)
            {
                return false;
            }
            Undo.RecordObject(target, "Shorten managed object name");
            target.name = name;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return true;
        }

        /// <summary>
        /// 選択チャンクの進行状況と実行可能な操作をウィンドウへ反映する
        /// </summary>
        private void UpdateColliderWorkflowUI()
        {
            if (colliderWorkflowStatus == null)
            {
                return;
            }
            var selected = blocks.Where(block => selectedForChunking.Contains(block.gameObject)).ToArray();
            var groupedCount = selected.Count(block => !string.IsNullOrEmpty(block.colliderGroupId));
            var invalidCount = selected.Count(block => !block.canMergeMesh);
            colliderWorkflowStatus.text = selectingChunkBlocks
                ? $"選択モード中: {selected.Length} 個選択 / 統合不可 {invalidCount} 個。Scene View でクリックして選択を切り替えます。"
                : $"通常モード: {selected.Length} 個選択中。チャンク化済み {groupedCount} 個。";
            beginChunkSelectionButton.text = selectingChunkBlocks
                ? "Scene View でブロックを選択中…"
                : "選択されたブロックをまとめてチャンク化する";
            beginChunkSelectionButton?.SetEnabled(rootValid);
            confirmChunkButton?.SetEnabled(selectingChunkBlocks && selected.Length > 0 && invalidCount == 0);
            unchunkButton?.SetEnabled(groupedCount > 0);
        }

        /// <summary>
        /// セルへのサイズ調整設定を保存する
        /// </summary>
        private void OnFitChanged(ChangeEvent<bool> change)
        {
            preferences.fit = change.newValue;
            SavePreferences();
        }

        /// <summary>
        /// 連続配置の最小間隔を制限して保存する
        /// </summary>
        private void OnIntervalChanged(ChangeEvent<float> change)
        {
            preferences.interval = ClampSetting(change.newValue, 0.1f, 10f);
            ((FloatField)change.target).SetValueWithoutNotify(preferences.interval);
            SavePreferences();
        }

        /// <summary>
        /// 移動速度を保存する
        /// </summary>
        private void OnSpeedChanged(ChangeEvent<float> change)
        {
            preferences.speed = ClampSetting(change.newValue, 0.1f, 30f);
            ((FloatField)change.target).SetValueWithoutNotify(preferences.speed);
            SavePreferences();
        }

        /// <summary>
        /// マウス感度を保存する
        /// </summary>
        private void OnSensitivityChanged(ChangeEvent<float> change)
        {
            preferences.sensitivity = ClampSetting(change.newValue, 0.01f, 1f);
            ((FloatField)change.target).SetValueWithoutNotify(preferences.sensitivity);
            SavePreferences();
        }

        /// <summary>
        /// 配置ガイドの色を保存する
        /// </summary>
        private void OnColorChanged(ChangeEvent<Color> change)
        {
            preferences.guideColor = change.newValue;
            SavePreferences();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Prefab アセットだけを GUID として登録する
        /// </summary>
        private void OnPrefabChanged(ChangeEvent<Object> change)
        {
            var field = (ObjectField)change.target;
            var index = (int)field.userData;
            var prefab = change.newValue as GameObject;
            if (prefab != null && (!PrefabUtility.IsPartOfPrefabAsset(prefab) || prefab.transform.parent != null))
            {
                field.SetValueWithoutNotify(SlotPrefab(index));
                return;
            }
            AssignSlotPrefab(index, prefab);
        }

        /// <summary>
        /// 登録済み Prefab と GUID を更新しスポイト後もスロット欄へ反映する
        /// </summary>
        private void AssignSlotPrefab(int slot, GameObject prefab)
        {
            preferences.prefabGuids[slot] = prefab == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            slotPrefabs[slot] = prefab;
            slotFields[slot]?.SetValueWithoutNotify(prefab);
            SavePreferences();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 配置回転欄で選んだ角度を次の配置へ反映する
        /// </summary>
        private void OnPlacementRotationChanged(ChangeEvent<string> change)
        {
            SetPlacementRotation(rotationField.index);
        }

        /// <summary>
        /// 追加の Y 回転を四方向へ正規化して保存しガイドと角度表示を更新する
        /// </summary>
        private void SetPlacementRotation(int steps)
        {
            preferences.rotationSteps = (steps % 4 + 4) % 4;
            rotationField?.SetValueWithoutNotify(rotationField.choices[preferences.rotationSteps]);
            SavePreferences();
            UpdateStatistics();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 照準下の管理ブロックから Prefab と四方向の回転を現在のスロットへ拾う
        /// </summary>
        private void PickAimedPrefab()
        {
            EndStroke();
            if (cacheDirty)
            {
                RebuildCache();
            }
            UpdateAim();
            if (aimedBlock == null || aimedBlock.gameObject == null || !rootValid
                || aimedBlock.gameObject.transform.parent != placementRoot.transform
                || ManagedName(aimedBlock.gameObject.name, BlockPrefix) == null)
            {
                status = "スポイト: 配置ルート内のブロックに照準を合わせてください";
                UpdateStatistics();
                return;
            }
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(aimedBlock.gameObject);
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || prefab.transform.parent != null)
            {
                status = "スポイト: 元の Prefab 参照がありません。スロットへ Prefab を直接登録してください";
                UpdateStatistics();
                return;
            }
            var relative = aimedBlock.gameObject.transform.localRotation * Quaternion.Inverse(prefab.transform.localRotation);
            var steps = (Mathf.RoundToInt(relative.eulerAngles.y / 90f) % 4 + 4) % 4;
            var canCopyRotation = Quaternion.Angle(relative, Quaternion.Euler(0, steps * 90f, 0)) < 0.1f;
            preferences.rotationSteps = canCopyRotation ? steps : 0;
            rotationField?.SetValueWithoutNotify(rotationField.choices[preferences.rotationSteps]);
            AssignSlotPrefab(preferences.slot, prefab);
            status = $"スポイト: {prefab.name} をスロット {preferences.slot + 1} に登録（Y {preferences.rotationSteps * 90}°）"
                + (canCopyRotation ? "" : "。自由回転は引き継がず Prefab の元の向きを使います");
            UpdateStatistics();
        }

        /// <summary>
        /// 選択中のチャンク境界を変更する
        /// </summary>
        private void OnChunkChanged(ChangeEvent<Vector2Int> change)
        {
            selectedChunk = change.newValue;
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 最後に照準が指していたセルのチャンクを選択する
        /// </summary>
        private void SelectAimedChunk()
        {
            selectedChunk = ChunkOf(aimedCell);
            CreateGUI();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 保存済み GUID からスロットの Prefab を取得する
        /// </summary>
        private GameObject SlotPrefab(int slot)
        {
            return slotPrefabs[slot];
        }

        /// <summary>
        /// アセット変更時だけスロット参照を再解決する
        /// </summary>
        private void ReloadPrefabs()
        {
            for (var i = 0; i < 9; i++)
            {
                var guid = preferences.prefabGuids[i];
                slotPrefabs[i] = string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        /// <summary>
        /// 実行時コンポーネントを追加せず設定用の子オブジェクトへ固定グリッド設定を保存する
        /// </summary>
        private static string RootSettingsName(float size, int width, bool merge)
        {
            return RootSettingsPrefix + size.ToString("R", CultureInfo.InvariantCulture) + "|" + width + "|" + (merge ? "1" : "0");
        }

        /// <summary>
        /// 表示名から管理部分を取り出し旧形式の名前にも対応する
        /// </summary>
        private static string ManagedName(string objectName, string prefix)
        {
            if (objectName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return objectName;
            }
            var index = objectName.LastIndexOf("|" + prefix, StringComparison.Ordinal);
            return index < 0 ? null : objectName.Substring(index + 1);
        }

        /// <summary>
        /// 名前に依存せず設定用の子を持つルートと旧形式のルートを識別する
        /// </summary>
        private static bool HasRootSettings(GameObject root)
        {
            return (root.name.Contains(RootPrefix) && ManagedName(root.name, BlockPrefix) == null
                && ManagedName(root.name, ChunkPrefix) == null) || root.transform.Cast<Transform>()
                .Any(child => child.name.StartsWith(RootSettingsPrefix, StringComparison.Ordinal));
        }

        /// <summary>
        /// 初回編集時に固定グリッド設定を保存しルート指定やモード開始だけでは子を追加しない
        /// </summary>
        private void EnsureRootSettings()
        {
            if (placementRoot == null || EditorUtility.IsPersistent(placementRoot) || !placementRoot.scene.IsValid()
                || PrefabStageUtility.GetCurrentPrefabStage() != null
                || placementRoot.transform.Cast<Transform>().Any(child => child.name.StartsWith(RootSettingsPrefix, StringComparison.Ordinal)))
            {
                return;
            }
            var source = PrefabUtility.GetCorrespondingObjectFromSource(placementRoot);
            if (!TryReadLegacySettings(placementRoot.name) && (source == null || !TryReadLegacySettings(source.name)))
            {
                grid = preferences.grid;
                chunkSize = preferences.chunkSize;
                mergeColliders = false;
            }
            var settings = new GameObject(RootSettingsName(grid, chunkSize, mergeColliders));
            SceneManager.MoveGameObjectToScene(settings, placementRoot.scene);
            settings.transform.SetParent(placementRoot.transform, false);
            Undo.RegisterCreatedObjectUndo(settings, "Initialize block root settings");
            EditorSceneManager.MarkSceneDirty(placementRoot.scene);
        }

        /// <summary>
        /// 接頭辞付きの旧ルート名からグリッド設定を読み取る
        /// </summary>
        private bool TryReadLegacySettings(string objectName)
        {
            var index = objectName.IndexOf(RootPrefix, StringComparison.Ordinal);
            return index >= 0 && TryReadSettings(objectName.Substring(index).Split('|'));
        }

        /// <summary>
        /// 設定値がすべて有効な場合だけ現在のグリッド設定へ反映する
        /// </summary>
        private bool TryReadSettings(string[] parts)
        {
            if (parts.Length != 4 || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var size)
                || !float.IsFinite(size) || size < 0.05f || size > 100f
                || !int.TryParse(parts[2], out var width) || width < 1 || width > 64
                || (parts[3] != "0" && parts[3] != "1"))
            {
                return false;
            }
            grid = size;
            chunkSize = width;
            mergeColliders = parts[3] == "1";
            return true;
        }

        /// <summary>
        /// 配置先と設定データを検証しルートのローカル座標を配置基準とする
        /// </summary>
        private bool ReadRootSettings()
        {
            RestorePlacementRootReference();
            rootValidationMessage = "Scene 内のオブジェクトを配置ルートに指定してください";
            if (placementRoot == null || EditorUtility.IsPersistent(placementRoot) || !placementRoot.scene.IsValid() || !placementRoot.scene.isLoaded)
            {
                return false;
            }
            var determinant = placementRoot.transform.localToWorldMatrix.determinant;
            rootValidationMessage = "配置ルートと親の Scale に 0 や無効な値が含まれています";
            if (!float.IsFinite(determinant) || Mathf.Abs(determinant) < 0.0000001f)
            {
                return false;
            }
            var settings = placementRoot.transform.Cast<Transform>()
                .Where(child => child.name.StartsWith(RootSettingsPrefix, StringComparison.Ordinal)).ToArray();
            rootValidationMessage = "配置ルートの設定データが不正または重複しています";
            var valid = settings.Length == 1 ? TryReadSettings(settings[0].name.Split('|'))
                : settings.Length == 0 && TryReadUninitializedRootSettings();
            if (valid)
            {
                rootValidationMessage = string.Empty;
            }
            return valid;
        }

        /// <summary>
        /// 保存前のルートは旧形式またはウィンドウの設定を読みシーンを変更しない
        /// </summary>
        private bool TryReadUninitializedRootSettings()
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(placementRoot);
            if (TryReadLegacySettings(placementRoot.name) || (source != null && TryReadLegacySettings(source.name)))
            {
                return true;
            }
            if (placementRoot.name.Contains(RootPrefix) || (source != null && source.name.Contains(RootPrefix)))
            {
                return false;
            }
            grid = preferences.grid;
            chunkSize = preferences.chunkSize;
            mergeColliders = false;
            return true;
        }

        /// <summary>
        /// Scene オブジェクトの GlobalObjectId を EditorWindow のシリアライズ状態へ同期する
        /// </summary>
        private void UpdatePlacementRootGlobalId()
        {
            placementRootGlobalId = placementRoot != null && !EditorUtility.IsPersistent(placementRoot)
                ? GlobalObjectId.GetGlobalObjectIdSlow(placementRoot).ToString()
                : string.Empty;
        }

        /// <summary>
        /// 参照が失効した配置ルートをシーン内 GlobalObjectId から復元する
        /// </summary>
        private void RestorePlacementRootReference()
        {
            if (placementRoot != null && placementRoot.scene.IsValid())
            {
                UpdatePlacementRootGlobalId();
                return;
            }
            if (string.IsNullOrEmpty(placementRootGlobalId)
                || !GlobalObjectId.TryParse(placementRootGlobalId, out var globalId))
            {
                return;
            }
            placementRoot = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
            if (placementRoot != null)
            {
                UpdatePlacementRootGlobalId();
            }
        }

        /// <summary>
        /// 既存オブジェクトを流用せず専用の空ルートを作る
        /// </summary>
        private void CreateRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return;
            }
            StopMode();
            if (selectingChunkBlocks)
            {
                CancelChunkSelection();
            }
            placementRoot = new GameObject(GameObjectUtility.GetUniqueNameForSibling(null, "BlockRoot"));
            UpdatePlacementRootGlobalId();
            Undo.RegisterCreatedObjectUndo(placementRoot, "Create block placement root");
            EnsureRootSettings();
            chunkSignatures.Clear();
            Invalidate();
            CreateGUI();
        }

        /// <summary>
        /// 外部編集後に次の Editor 更新でキャッシュを更新する
        /// </summary>
        private void Invalidate()
        {
            if (!rebuilding)
            {
                cacheDirty = true;
            }
        }

        /// <summary>
        /// Undo 後の派生 Collider と占有セルを同期する
        /// </summary>
        private void OnUndoRedo()
        {
            EndStroke();
            recordDerivedUndo = false;
            Invalidate();
        }

        /// <summary>
        /// Inspector での Transform や Renderer 編集を検知する
        /// </summary>
        private UndoPropertyModification[] OnModifications(UndoPropertyModification[] modifications)
        {
            Invalidate();
            return modifications;
        }

        /// <summary>
        /// アプリケーションのフォーカス喪失時に入力を解放する
        /// </summary>
        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                modeKeyHeld = false;
                StopAllModes();
            }
        }

        /// <summary>
        /// Play モードへ入る前に編集操作を終了する
        /// </summary>
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            StopAllModes();
        }

        /// <summary>
        /// 配置モードと選択モードを終了して Scene View の設定を復元する
        /// </summary>
        private void StopAllModes()
        {
            StopMode();
            if (selectingChunkBlocks)
            {
                CancelChunkSelection();
            }
        }

        /// <summary>
        /// Scene View 固有の標準入力より前にモード中の入力だけを処理する
        /// </summary>
        private void BeforeSceneGUI(SceneView view)
        {
            var current = Event.current;
            if (focusedWindow != view || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (activeView == view)
                {
                    modeKeyHeld = false;
                    StopMode();
                }
                return;
            }
            if (selectingChunkBlocks)
            {
                if (current.type == EventType.KeyDown && current.keyCode == KeyCode.B
                    && current.modifiers == EventModifiers.None && !EditorGUIUtility.editingTextField)
                {
                    RequestPlacementMode(view);
                    current.Use();
                }
                else
                {
                    HandleChunkSelectionGUI(view, current);
                }
                return;
            }
            if (pendingStartView != null)
            {
                if (current.type == EventType.KeyUp && current.keyCode == KeyCode.B)
                {
                    modeKeyHeld = false;
                    current.Use();
                }
                else if (current.type == EventType.KeyDown && current.keyCode == KeyCode.B)
                {
                    current.Use();
                }
                else if (current.alt || (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape))
                {
                    StopMode();
                }
                return;
            }
            if (current.type == EventType.KeyUp && current.keyCode == KeyCode.B)
            {
                modeKeyHeld = false;
            }
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.B && modeKeyHeld)
            {
                current.Use();
                return;
            }
            if (activeView == null)
            {
                if (current.type == EventType.KeyDown && current.keyCode == KeyCode.B && current.modifiers == EventModifiers.None
                    && !EditorGUIUtility.editingTextField && GUIUtility.hotControl == 0)
                {
                    RequestPlacementMode(view);
                    current.Use();
                }
                return;
            }
            if (view != activeView)
            {
                return;
            }
            if (current.alt || current.command || current.control)
            {
                StopMode();
                return;
            }
            if (current.type == EventType.KeyDown && (current.keyCode == KeyCode.B || current.keyCode == KeyCode.Escape))
            {
                modeKeyHeld = current.keyCode == KeyCode.B;
                StopMode();
                current.Use();
                return;
            }
            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(captureControl);
            }
            if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
            {
                CollectMouseLook();
                current.Use();
            }
            else if (current.type == EventType.ScrollWheel)
            {
                ChangeSlot(preferences.slot + (current.delta.y > 0 ? 1 : -1));
                current.Use();
            }
            else if (current.type == EventType.KeyDown || current.type == EventType.KeyUp)
            {
                HandleKey(current);
            }
            else if (current.type == EventType.MouseDown)
            {
                if (current.button == 2)
                {
                    PickAimedPrefab();
                    current.Use();
                    return;
                }
                UpdateAim();
                if (current.button == 1)
                {
                    BeginStroke();
                    PlaceCandidate();
                    nextPlacement = EditorApplication.timeSinceStartup + preferences.interval;
                }
                else if (current.button == 0)
                {
                    DestroyAimedBlock();
                }
                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                if (current.button == 1)
                {
                    EndStroke();
                }
                current.Use();
            }
        }

        /// <summary>
        /// 選択モード中は Scene View の通常選択を抑え、対象ブロックだけを内部選択する
        /// </summary>
        private void HandleChunkSelectionGUI(SceneView view, Event current)
        {
            if (view != selectionView)
            {
                return;
            }
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                CancelChunkSelection();
                current.Use();
                return;
            }
            if (current.alt || !view.cameraViewport.Contains(current.mousePosition))
            {
                return;
            }
            if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
            {
                hoveredSelectionBlock = FindBlockForSelection(HandleUtility.GUIPointToWorldRay(current.mousePosition));
                SceneView.RepaintAll();
                if (current.type == EventType.MouseDrag && current.button == 0)
                {
                    current.Use();
                }
                return;
            }
            if (current.button != 0)
            {
                return;
            }
            if (current.type == EventType.MouseDown)
            {
                var block = FindBlockForSelection(HandleUtility.GUIPointToWorldRay(current.mousePosition));
                if (block != null)
                {
                    if (!selectedForChunking.Add(block.gameObject))
                    {
                        selectedForChunking.Remove(block.gameObject);
                    }
                    hoveredSelectionBlock = block;
                    status = selectedForChunking.Contains(block.gameObject)
                        ? "ブロックを選択しました。続けて選択するか、ウィンドウで確定してください"
                        : "ブロックの選択を解除しました";
                    UpdateColliderWorkflowUI();
                    UpdateStatistics();
                    SceneView.RepaintAll();
                }
                current.Use();
                return;
            }
            if (current.type == EventType.MouseUp)
            {
                current.Use();
            }
        }

        /// <summary>
        /// レイ上で最も手前にある管理対象ブロックを返し外部 Collider の遮蔽を尊重する
        /// </summary>
        private Block FindBlockForSelection(Ray ray)
        {
            if (!rootValid || placementRoot == null)
            {
                return null;
            }
            var worldRay = ray;
            ray = RootLocalRay(worldRay, out var distanceScale);
            var maximumDistance = grid * ReachInCells;
            var nearestDistance = float.PositiveInfinity;
            Block nearestBlock = null;
            foreach (var block in blocks)
            {
                if (!block.gameObject.activeInHierarchy || !block.bounds.IntersectRay(ray, out var distance)
                    || distance < 0 || distance > maximumDistance || distance >= nearestDistance)
                {
                    continue;
                }
                nearestDistance = distance;
                nearestBlock = block;
            }
            if (nearestBlock == null)
            {
                return null;
            }
            var nearestExternalHit = Physics.RaycastAll(worldRay, maximumDistance / distanceScale, ~0, QueryTriggerInteraction.Ignore)
                .Where(hit => hit.collider != null && !IsManagedBuilderCollider(hit.collider))
                .Select(hit => hit.distance * distanceScale).DefaultIfEmpty(float.PositiveInfinity).Min();
            return nearestDistance <= nearestExternalHit + grid * 0.001f ? nearestBlock : null;
        }

        /// <summary>
        /// 配置ルート直下の管理ブロックと生成チャンクを選択遮蔽物から除外する
        /// </summary>
        private bool IsManagedBuilderCollider(Collider collider)
        {
            var item = collider.transform;
            while (item.parent != null && item.parent != placementRoot.transform)
            {
                item = item.parent;
            }
            return item.parent == placementRoot.transform
                && (ManagedName(item.name, BlockPrefix) != null || ManagedName(item.name, ChunkPrefix) != null);
        }

        /// <summary>
        /// キーリピートを除外して水平移動と垂直移動の押下状態を処理する
        /// </summary>
        private void HandleKey(Event current)
        {
            var key = current.keyCode;
            var down = current.type == EventType.KeyDown;
            if (key == KeyCode.R || key == KeyCode.F)
            {
                if (down && keys.Add(key))
                {
                    if (key == KeyCode.R)
                    {
                        SetPlacementRotation(preferences.rotationSteps + (current.shift ? -1 : 1));
                    }
                    else
                    {
                        PickAimedPrefab();
                    }
                }
                else if (!down)
                {
                    keys.Remove(key);
                }
                current.Use();
                return;
            }
            if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9)
            {
                if (down)
                {
                    ChangeSlot(key - KeyCode.Alpha1);
                }
                current.Use();
                return;
            }
            if (key == KeyCode.Space || key == KeyCode.LeftShift || key == KeyCode.RightShift)
            {
                current.Use();
                return;
            }
            if (key != KeyCode.W && key != KeyCode.A && key != KeyCode.S && key != KeyCode.D
                && key != KeyCode.Q && key != KeyCode.E)
            {
                return;
            }
            if (down)
            {
                keys.Add(key);
            }
            else if (!down)
            {
                keys.Remove(key);
            }
            current.Use();
        }

        /// <summary>
        /// 視点設定を保存しマウス捕捉を一度だけ開始する
        /// </summary>
        private void StartMode(SceneView view, int control)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                view.ShowNotification(new GUIContent("Prefab Mode を閉じ、Scene 上のインスタンスを配置ルートに指定してください"));
                return;
            }
            RestorePlacementRootReference();
            if (!ReadRootSettings())
            {
                view.ShowNotification(new GUIContent(rootValidationMessage));
                return;
            }
            RebuildCache();
            if (!cursorCapture.Begin(GUIUtility.GUIToScreenPoint(view.cameraViewport.center),
                view.cameraViewport.size, EditorGUIUtility.pixelsPerPoint))
            {
                modeKeyHeld = false;
                status = "カーソルを固定できませんでした。Windows Editor の Scene View にフォーカスして開始してください";
                view.ShowNotification(new GUIContent(status));
                Debug.LogWarning("Block Builder: " + status);
                return;
            }
            cameraSnapshot = new CameraSnapshot
            {
                pivot = view.pivot, rotation = view.rotation, lastSceneViewRotation = view.lastSceneViewRotation, size = view.size, orthographic = view.orthographic,
                in2D = view.in2DMode, rotationLocked = view.isRotationLocked, wantsMouseMove = view.wantsMouseMove,
                easing = view.cameraSettings.easingEnabled, acceleration = view.cameraSettings.accelerationEnabled,
                fieldOfView = view.cameraSettings.fieldOfView
            };
            activeView = view;
            cameraPosition = view.camera.transform.position;
            var euler = view.camera.transform.rotation.eulerAngles;
            angles = new Vector2(Mathf.Clamp(Mathf.DeltaAngle(0, euler.x), -85, 85), euler.y);
            view.in2DMode = false;
            view.isRotationLocked = false;
            view.orthographic = false;
            view.cameraSettings.easingEnabled = false;
            view.cameraSettings.accelerationEnabled = false;
            view.cameraSettings.fieldOfView = 60f;
            view.wantsMouseMove = true;
            captureControl = control;
            GUIUtility.hotControl = captureControl;
            pendingLook = Vector2.zero;
            previousUpdate = EditorApplication.timeSinceStartup;
            ApplyCamera();
            UpdateModeIndicator();
        }

        /// <summary>
        /// 全終了経路でマウス捕捉と元の Scene View 設定を復元する
        /// </summary>
        private void StopMode()
        {
            cursorCapture.End();
            pendingStartView = null;
            pendingStartDeadline = 0;
            EndStroke();
            if (cameraSnapshot == null)
            {
                return;
            }
            if (GUIUtility.hotControl == captureControl)
            {
                GUIUtility.hotControl = 0;
            }
            if (activeView != null)
            {
                activeView.in2DMode = cameraSnapshot.in2D;
                activeView.isRotationLocked = cameraSnapshot.rotationLocked;
                activeView.wantsMouseMove = cameraSnapshot.wantsMouseMove;
                activeView.cameraSettings.easingEnabled = cameraSnapshot.easing;
                activeView.cameraSettings.accelerationEnabled = cameraSnapshot.acceleration;
                activeView.cameraSettings.fieldOfView = cameraSnapshot.fieldOfView;
                activeView.LookAt(cameraSnapshot.pivot, cameraSnapshot.rotation, cameraSnapshot.size, cameraSnapshot.orthographic, true);
                activeView.lastSceneViewRotation = cameraSnapshot.lastSceneViewRotation;
                activeView.Repaint();
            }
            keys.Clear();
            pendingLook = Vector2.zero;
            activeView = null;
            cameraSnapshot = null;
            aimedBlock = null;
            UpdateModeIndicator();
        }

        /// <summary>
        /// Editor 更新ごとに一度だけ移動とカメラ更新を行う
        /// </summary>
        private void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StopMode();
                return;
            }
            if (pendingStartView != null && EditorApplication.timeSinceStartup >= pendingStartDeadline)
            {
                pendingStartView = null;
                pendingStartDeadline = 0;
                modeKeyHeld = false;
                status = "Scene View に切り替えられなかったため、配置モード開始を中止しました";
                UpdateStatistics();
            }
            if (cacheDirty)
            {
                RebuildCache();
            }
            if (cameraSnapshot == null)
            {
                return;
            }
            if (activeView == null || focusedWindow != activeView || !EditorApplication.isFocused
                || !cursorCapture.HasFocus() || !rootValid)
            {
                modeKeyHeld = false;
                StopMode();
                return;
            }
            if (!CollectMouseLook())
            {
                return;
            }
            var now = EditorApplication.timeSinceStartup;
            var delta = Mathf.Clamp((float)(now - previousUpdate), 0, 0.05f);
            previousUpdate = now;
            angles.x = Mathf.Clamp(angles.x + pendingLook.y * preferences.sensitivity, -85f, 85f);
            angles.y = Mathf.Repeat(angles.y + pendingLook.x * preferences.sensitivity, 360f);
            pendingLook = Vector2.zero;
            var move = new Vector3((keys.Contains(KeyCode.D) ? 1 : 0) - (keys.Contains(KeyCode.A) ? 1 : 0),
                (keys.Contains(KeyCode.Q) ? 1 : 0) - (keys.Contains(KeyCode.E) ? 1 : 0),
                (keys.Contains(KeyCode.W) ? 1 : 0) - (keys.Contains(KeyCode.S) ? 1 : 0));
            cameraPosition += Quaternion.Euler(0, angles.y, 0) * Vector3.ClampMagnitude(move, 1) * (preferences.speed * grid * delta);
            ApplyCamera();
            UpdateAim();
            if (placing && now >= nextPlacement)
            {
                // 遅延分をまとめて配置せず一更新につき最大一個とする
                PlaceCandidate();
                nextPlacement = now + preferences.interval;
            }
            activeView.Repaint();
        }

        /// <summary>
        /// 実カーソルの移動だけを蓄積し、捕捉失敗時にはモードを安全に終了する
        /// </summary>
        private bool CollectMouseLook()
        {
            if (!cursorCapture.Sample(out var delta))
            {
                StopMode();
                return false;
            }
            pendingLook = Vector2.ClampMagnitude(pendingLook + delta, 100f);
            return true;
        }

        /// <summary>
        /// 補間を使わず保持中の姿勢を Scene View に一度だけ反映する
        /// </summary>
        private void ApplyCamera()
        {
            var rotation = Quaternion.Euler(angles.x, angles.y, 0);
            activeView.size = 1f;
            activeView.LookAt(cameraPosition + rotation * Vector3.forward * activeView.cameraDistance, rotation, 1f, false, true);
        }

        /// <summary>
        /// ホットバーの選択と保存を更新する
        /// </summary>
        private void ChangeSlot(int slot)
        {
            preferences.slot = (slot % 9 + 9) % 9;
            SavePreferences();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// ワールド座標を負座標にも対応したセルへ変換する
        /// </summary>
        private Vector3Int CellOf(Vector3 position)
        {
            return Vector3Int.FloorToInt(position / grid);
        }

        /// <summary>
        /// 遠方のワールド座標の丸め誤差を考慮した境界許容値を返す
        /// </summary>
        private float BoundsTolerance(Bounds bounds)
        {
            var center = bounds.center;
            var magnitude = Mathf.Max(Mathf.Abs(center.x), Mathf.Abs(center.y), Mathf.Abs(center.z));
            return Mathf.Min(grid * 0.05f, Mathf.Max(grid * 0.001f, magnitude * 0.000001f));
        }

        /// <summary>
        /// セル座標から XZ チャンク座標を求める
        /// </summary>
        private Vector2Int ChunkOf(Vector3Int cell)
        {
            return new Vector2Int(Mathf.FloorToInt((float)cell.x / chunkSize), Mathf.FloorToInt((float)cell.z / chunkSize));
        }

        /// <summary>
        /// Renderer を優先して配置ルートのローカル座標で範囲を計算する
        /// </summary>
        private bool TryBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                Encapsulate(ref bounds, ref found, TransformBounds(renderer.localBounds,
                    placementRoot.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix));
            }
            if (!found)
            {
                foreach (var collider in target.GetComponentsInChildren<Collider>(true))
                {
                    // 無効化中も形状から求めることでルート回転時のワールド AABB 拡大を避ける
                    var matrix = placementRoot.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
                    if (collider is BoxCollider box)
                    {
                        var local = new Bounds(box.center, box.size);
                        Encapsulate(ref bounds, ref found, TransformBounds(local, matrix));
                    }
                    else if (collider is SphereCollider sphere)
                    {
                        Encapsulate(ref bounds, ref found, TransformBounds(new Bounds(sphere.center, Vector3.one * sphere.radius * 2f), matrix));
                    }
                    else if (collider is MeshCollider mesh && mesh.sharedMesh != null)
                    {
                        Encapsulate(ref bounds, ref found, TransformBounds(mesh.sharedMesh.bounds, matrix));
                    }
                    else if (collider is CapsuleCollider capsule)
                    {
                        var size = Vector3.one * capsule.radius * 2f;
                        size[capsule.direction] = Mathf.Max(size[capsule.direction], capsule.height);
                        Encapsulate(ref bounds, ref found, TransformBounds(new Bounds(capsule.center, size), matrix));
                    }
                    else if (collider.enabled && collider.gameObject.activeInHierarchy)
                    {
                        Encapsulate(ref bounds, ref found, TransformBounds(collider.bounds, placementRoot.transform.worldToLocalMatrix));
                    }
                }
            }
            return found && bounds.size.sqrMagnitude > 0.000001f;
        }

        /// <summary>
        /// 範囲の八頂点を指定した座標系へ変換する
        /// </summary>
        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            var result = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                result.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            }
            return result;
        }

        /// <summary>
        /// 最初の範囲を初期値として複数範囲をまとめる
        /// </summary>
        private static void Encapsulate(ref Bounds result, ref bool found, Bounds next)
        {
            if (found)
            {
                result.Encapsulate(next);
            }
            else
            {
                result = next;
                found = true;
            }
        }

        /// <summary>
        /// シーン変更時だけ専用ルートの直下から占有キャッシュを再構築する
        /// </summary>
        private void RebuildCache()
        {
            cacheDirty = false;
            cells.Clear();
            blocks.Clear();
            cachedDimensions = Vector3Int.zero;
            rootValid = ReadRootSettings();
            if (!rootValid)
            {
                selectingChunkBlocks = false;
                RestoreSelectionView();
                selectedForChunking.Clear();
                recordDerivedUndo = true;
                status = rootValidationMessage;
                UpdateStatistics();
                UpdateColliderWorkflowUI();
                return;
            }
            rebuilding = true;
            foreach (Transform child in placementRoot.transform)
            {
                var managedName = ManagedName(child.name, BlockPrefix);
                if (managedName == null)
                {
                    continue;
                }
                var parts = managedName.Split('|');
                if (parts.Length < 2 || !TryBounds(child.gameObject, out var bounds))
                {
                    continue;
                }
                var colliders = child.GetComponentsInChildren<Collider>(true);
                var tolerance = Mathf.Min(BoundsTolerance(bounds), Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) * 0.25f);
                var block = new Block
                {
                    gameObject = child.gameObject, bounds = bounds, colliders = colliders, originalFlags = parts[1],
                    min = CellOf(bounds.min + Vector3.one * tolerance), max = CellOf(bounds.max - Vector3.one * tolerance),
                    colliderGroupId = ReadColliderGroupId(child.name)
                };
                var count = (long)(block.max.x - block.min.x + 1) * (block.max.y - block.min.y + 1) * (block.max.z - block.min.z + 1);
                block.canMerge = child.gameObject.activeInHierarchy && parts[1].Length == colliders.Length && colliders.All(c => !c.isTrigger && c.attachedRigidbody == null)
                    && child.GetComponentsInChildren<Rigidbody>(true).Length == 0 && count > 0 && count <= MaximumCellsPerBlock
                    && (colliders.Length == 0 || parts[1].Contains('1'));
                block.canMergeMesh = block.canMerge && child.GetComponentInParent<Rigidbody>() == null
                    && HasStaticMeshGeometry(child.gameObject);
                if (count <= 0 || count > MaximumCellsPerBlock)
                {
                    RestoreColliders(block);
                    status = "4096 セルを超えるブロックはサイズを縮小してください";
                    continue;
                }
                blocks.Add(block);
                foreach (var cell in BlockCells(block))
                {
                    if (!cells.TryGetValue(cell, out var occupants))
                    {
                        occupants = new List<Block>();
                        cells.Add(cell, occupants);
                    }
                    occupants.Add(block);
                }
            }
            SynchronizeChunks();
            if (blocks.Count > 0)
            {
                var min = blocks[0].min;
                var max = blocks[0].max;
                foreach (var block in blocks)
                {
                    min = Vector3Int.Min(min, block.min);
                    max = Vector3Int.Max(max, block.max);
                }
                cachedDimensions = max - min + Vector3Int.one;
            }
            rebuilding = false;
            recordDerivedUndo = true;
            selectedForChunking.RemoveWhere(gameObject => gameObject == null || gameObject.transform.parent != placementRoot.transform);
            UpdateStatistics();
            UpdateColliderWorkflowUI();
        }

        /// <summary>
        /// 管理ブロック名から手動 Collider グループ識別子を読み取る
        /// </summary>
        private static string ReadColliderGroupId(string blockName)
        {
            var marker = blockName.LastIndexOf("|Group=", StringComparison.Ordinal);
            return marker < 0 ? null : blockName.Substring(marker + 7);
        }

        /// <summary>
        /// ブロックの範囲に含まれるセルだけを列挙する
        /// </summary>
        private static IEnumerable<Vector3Int> BlockCells(Block block)
        {
            for (var x = block.min.x; x <= block.max.x; x++)
            {
                for (var y = block.min.y; y <= block.max.y; y++)
                {
                    for (var z = block.min.z; z <= block.max.z; z++)
                    {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        /// <summary>
        /// チャンクごとの占有差分だけを Collider に反映する
        /// </summary>
        private void SynchronizeChunks()
        {
            var existing = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (Transform child in placementRoot.transform)
            {
                var chunkKey = ManagedName(child.name, ChunkPrefix);
                if (chunkKey != null)
                {
                    existing[chunkKey] = child.gameObject;
                }
            }
            var desired = new Dictionary<string, HashSet<Vector3Int>>(StringComparer.Ordinal);
            var contributors = new Dictionary<string, HashSet<Block>>(StringComparer.Ordinal);
            var blockChunks = new Dictionary<Block, HashSet<string>>();
            foreach (var block in blocks)
            {
                if (!block.canMerge || (block.colliderGroupId != null
                    && block.colliderGroupId.StartsWith(MeshGroupPrefix, StringComparison.Ordinal) && !block.canMergeMesh))
                {
                    RestoreColliders(block);
                    continue;
                }
                if (block.colliderGroupId == string.Empty || (block.colliderGroupId == null && !mergeColliders))
                {
                    RestoreColliders(block);
                    continue;
                }
                // 旧形式の自動チャンクは既存分だけを維持し、新しいチャンクへ広がるブロックは個別判定を残す
                if (block.colliderGroupId == null && BlockCells(block).Any(cell =>
                    !existing.ContainsKey(ChunkPrefix + ChunkOf(cell).x + "|" + ChunkOf(cell).y)))
                {
                    RestoreColliders(block);
                    continue;
                }
                foreach (var cell in BlockCells(block))
                {
                    var chunkName = !string.IsNullOrEmpty(block.colliderGroupId)
                        ? ChunkPrefix + "Group|" + block.colliderGroupId
                        : ChunkPrefix + ChunkOf(cell).x + "|" + ChunkOf(cell).y;
                    if (!desired.TryGetValue(chunkName, out var occupied))
                    {
                        occupied = new HashSet<Vector3Int>();
                        desired.Add(chunkName, occupied);
                        contributors.Add(chunkName, new HashSet<Block>());
                    }
                    occupied.Add(cell);
                    contributors[chunkName].Add(block);
                    if (!blockChunks.TryGetValue(block, out var chunkNames))
                    {
                        chunkNames = new HashSet<string>(StringComparer.Ordinal);
                        blockChunks.Add(block, chunkNames);
                    }
                    chunkNames.Add(chunkName);
                }
            }
            foreach (var pair in existing)
            {
                if (!desired.ContainsKey(pair.Key))
                {
                    DestroyDerived(pair.Value);
                    chunkSignatures.Remove(pair.Key);
                }
            }
            var succeeded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in desired)
            {
                var meshGroup = pair.Key.StartsWith(ChunkPrefix + "Group|" + MeshGroupPrefix, StringComparison.Ordinal);
                var sorted = pair.Value.OrderBy(c => c.y).ThenBy(c => c.z).ThenBy(c => c.x).ToArray();
                var signature = string.Join(";", sorted.Select(c => $"{c.x},{c.y},{c.z}"));
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var geometryValid = !meshGroup || CollectChunkGeometry(contributors[pair.Key], vertices, triangles, out signature);
                existing.TryGetValue(pair.Key, out var old);
                if (geometryValid && old != null && (meshGroup
                    ? MeshChunkMatches(old, signature, contributors[pair.Key].First().gameObject.layer)
                    : ChunkMatches(old, pair.Value, signature, pair.Key)))
                {
                    succeeded.Add(pair.Key);
                    continue;
                }
                // 元 Collider を有効に戻してから生成し、失敗時にも物理判定を失わない
                foreach (var block in contributors[pair.Key])
                {
                    RestoreColliders(block);
                }
                var replacement = !geometryValid ? null : meshGroup
                    ? BuildMeshChunk(pair.Key, vertices, triangles, contributors[pair.Key].First().gameObject.layer)
                    : BuildChunk(pair.Key, pair.Value);
                if (replacement == null)
                {
                    if (old != null)
                    {
                        DestroyDerived(old);
                    }
                    chunkSignatures.Remove(pair.Key);
                    continue;
                }
                if (old != null)
                {
                    DestroyDerived(old);
                }
                chunkSignatures[pair.Key] = signature;
                succeeded.Add(pair.Key);
            }
            foreach (var block in blocks)
            {
                if (blockChunks.TryGetValue(block, out var chunkNames) && chunkNames.All(succeeded.Contains))
                {
                    foreach (var collider in block.colliders)
                    {
                        if (collider.enabled)
                        {
                            if (recordDerivedUndo)
                            {
                                Undo.RecordObject(collider, "Update block colliders");
                            }
                            collider.enabled = false;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 保存済み Collider のセル範囲を照合し無変更チャンクの再生成を防ぐ
        /// </summary>
        private bool ChunkMatches(GameObject chunk, HashSet<Vector3Int> desired, string signature, string chunkName)
        {
            var colliders = chunk.GetComponents<BoxCollider>();
            if (colliders.Length == 0 || colliders.Any(c => !c.enabled || c.isTrigger)
                || chunk.transform.localPosition != Vector3.zero || chunk.transform.localRotation != Quaternion.identity || chunk.transform.localScale != Vector3.one)
            {
                return false;
            }
            // Collider 自体の Inspector 編集も検出するため形状を署名に含める
            var shapeSignature = signature + ":" + string.Join(";", colliders.Select(c => c.center.ToString("R") + c.size.ToString("R")));
            if (chunkSignatures.TryGetValue(chunkName, out var known) && known == shapeSignature)
            {
                return true;
            }
            var actual = new HashSet<Vector3Int>();
            foreach (var collider in colliders)
            {
                var min = CellOf(collider.center - collider.size * 0.5f + Vector3.one * grid * 0.01f);
                var max = CellOf(collider.center + collider.size * 0.5f - Vector3.one * grid * 0.01f);
                var expectedCenter = ((Vector3)min + (Vector3)max + Vector3.one) * grid * 0.5f;
                var expectedSize = ((Vector3)(max - min) + Vector3.one) * grid;
                if (Vector3.Distance(expectedCenter, collider.center) > grid * 0.001f || Vector3.Distance(expectedSize, collider.size) > grid * 0.001f)
                {
                    return false;
                }
                var count = (long)(max.x - min.x + 1) * (max.y - min.y + 1) * (max.z - min.z + 1);
                if (count <= 0 || count > desired.Count)
                {
                    return false;
                }
                foreach (var cell in BlockCells(new Block { min = min, max = max }))
                {
                    actual.Add(cell);
                }
            }
            var matches = actual.SetEquals(desired);
            if (matches)
            {
                chunkSignatures[chunkName] = shapeSignature;
            }
            return matches;
        }

        /// <summary>
        /// 通常編集では派生オブジェクトの削除も同じ Undo に含める
        /// </summary>
        private void DestroyDerived(GameObject target)
        {
            var meshes = target.GetComponents<MeshCollider>().Select(collider => collider.sharedMesh)
                .Where(mesh => mesh != null && !AssetDatabase.Contains(mesh)
                    && mesh.name.StartsWith(DerivedMeshPrefix, StringComparison.Ordinal)
                    && !Resources.FindObjectsOfTypeAll<MeshCollider>().Any(collider => collider.gameObject != target
                        && collider.sharedMesh == mesh)).Distinct().ToArray();
            if (recordDerivedUndo)
            {
                Undo.DestroyObjectImmediate(target);
                foreach (var mesh in meshes)
                {
                    Undo.DestroyObjectImmediate(mesh);
                }
            }
            else
            {
                DestroyImmediate(target);
                foreach (var mesh in meshes)
                {
                    DestroyImmediate(mesh);
                }
            }
        }

        /// <summary>
        /// LOD は最高詳細度だけを使い有効な固定メッシュの Renderer を列挙する
        /// </summary>
        private static MeshRenderer[] StaticMeshRenderers(GameObject block)
        {
            var excluded = new HashSet<Renderer>();
            foreach (var lod in block.GetComponentsInChildren<LODGroup>(true))
            {
                var levels = lod.GetLODs();
                var first = levels.Length > 0 ? new HashSet<Renderer>(levels[0].renderers) : new HashSet<Renderer>();
                foreach (var renderer in levels.Skip(1).SelectMany(level => level.renderers))
                {
                    if (!first.Contains(renderer))
                    {
                        excluded.Add(renderer);
                    }
                }
            }
            return block.GetComponentsInChildren<MeshRenderer>().Where(renderer => renderer.enabled
                && renderer.gameObject.activeInHierarchy && !excluded.Contains(renderer)).ToArray();
        }

        /// <summary>
        /// 動的変形を含まず三角形の表示メッシュが揃っているか確認する
        /// </summary>
        private static bool HasStaticMeshGeometry(GameObject block)
        {
            if (block.GetComponentsInChildren<SkinnedMeshRenderer>().Any(renderer => renderer.enabled))
            {
                return false;
            }
            var renderers = StaticMeshRenderers(block);
            return renderers.Length > 0 && renderers.All(renderer =>
            {
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                return renderer.gameObject.layer == block.layer && mesh != null && mesh.vertexCount > 0
                    && mesh.subMeshCount > 0 && Mathf.Abs(renderer.transform.localToWorldMatrix.determinant) > 0.0000001f
                    && Enumerable.Range(0, mesh.subMeshCount).All(index => mesh.GetTopology(index) == MeshTopology.Triangles);
            });
        }

        /// <summary>
        /// 表示メッシュをルート座標へ変換し負スケールの面方向を補正して形状署名を作る
        /// </summary>
        private bool CollectChunkGeometry(IEnumerable<Block> contributors, List<Vector3> vertices, List<int> triangles, out string signature)
        {
            signature = string.Empty;
            var source = contributors.OrderBy(block => block.gameObject.transform.GetSiblingIndex()).ToArray();
            if (source.Any(block => !block.canMergeMesh) || source.Select(block => block.gameObject.layer).Distinct().Count() != 1)
            {
                return false;
            }
            foreach (var block in source)
            {
                foreach (var renderer in StaticMeshRenderers(block.gameObject))
                {
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var matrix = placementRoot.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    var offset = vertices.Count;
                    // Read/Write 無効の FBX も Importer を変更せず Editor 専用 API で読む
                    using (var data = MeshUtility.AcquireReadOnlyMeshData(mesh))
                    using (var positions = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp))
                    {
                        data[0].GetVertices(positions);
                        foreach (var position in positions)
                        {
                            var transformed = matrix.MultiplyPoint3x4(position);
                            if (!float.IsFinite(transformed.x) || !float.IsFinite(transformed.y) || !float.IsFinite(transformed.z))
                            {
                                return false;
                            }
                            vertices.Add(transformed);
                        }
                        for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                        {
                            using (var indices = new NativeArray<int>(data[0].GetSubMesh(submesh).indexCount, Allocator.Temp))
                            {
                                data[0].GetIndices(indices, submesh, true);
                                for (var index = 0; index + 2 < indices.Length; index += 3)
                                {
                                    var a = offset + indices[index];
                                    var b = offset + indices[index + 1];
                                    var c = offset + indices[index + 2];
                                    if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude <= 0f)
                                    {
                                        continue;
                                    }
                                    triangles.Add(a);
                                    triangles.Add(matrix.determinant < 0 ? c : b);
                                    triangles.Add(matrix.determinant < 0 ? b : c);
                                }
                            }
                        }
                    }
                }
            }
            var hash = new Hash128();
            hash.Append(vertices);
            hash.Append(triangles);
            signature = DerivedMeshPrefix + hash;
            return triangles.Count > 0;
        }

        /// <summary>
        /// 保存済みメッシュと物理設定を照合して変更のないグループの再生成を省く
        /// </summary>
        private static bool MeshChunkMatches(GameObject chunk, string signature, int layer)
        {
            var colliders = chunk.GetComponents<Collider>();
            if (colliders.Length != 1 || colliders[0] is not MeshCollider collider || !collider.enabled
                || collider.convex || collider.isTrigger || collider.sharedMesh == null || !chunk.activeSelf
                || chunk.layer != layer || chunk.transform.localPosition != Vector3.zero
                || chunk.transform.localRotation != Quaternion.identity || chunk.transform.localScale != Vector3.one)
            {
                return false;
            }
            var mesh = collider.sharedMesh;
            var hash = new Hash128();
            hash.Append(mesh.vertices);
            hash.Append(mesh.triangles);
            return signature == DerivedMeshPrefix + hash;
        }

        /// <summary>
        /// 凹形状を維持するシーン保存可能なメッシュと固定ステージ用 Collider を生成する
        /// </summary>
        private GameObject BuildMeshChunk(string chunkName, List<Vector3> vertices, List<int> triangles, int layer)
        {
            var mesh = new Mesh
            {
                name = DerivedMeshPrefix + chunkName.Substring(ChunkPrefix.Length).Replace("Group|", string.Empty),
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            var result = new GameObject(chunkName) { layer = layer };
            SceneManager.MoveGameObjectToScene(result, placementRoot.scene);
            result.transform.SetParent(placementRoot.transform, false);
            var collider = result.AddComponent<MeshCollider>();
            collider.convex = false;
            collider.sharedMesh = mesh;
            if (collider.bounds.size.sqrMagnitude <= 0f)
            {
                DestroyImmediate(result);
                DestroyImmediate(mesh);
                Debug.LogWarning("Block Builder: MeshCollider の生成に失敗したため個別 Collider を維持します", placementRoot);
                return null;
            }
            if (recordDerivedUndo)
            {
                Undo.RegisterCreatedObjectUndo(mesh, "Create chunk collision mesh");
                Undo.RegisterCreatedObjectUndo(result, "Create mesh chunk collider");
            }
            EditorSceneManager.MarkSceneDirty(placementRoot.scene);
            return result;
        }

        /// <summary>
        /// 占有セルの X 方向連続区間を統合した BoxCollider を生成する
        /// </summary>
        private GameObject BuildChunk(string chunkName, HashSet<Vector3Int> occupied)
        {
            var result = new GameObject(chunkName);
            SceneManager.MoveGameObjectToScene(result, placementRoot.scene);
            result.transform.SetParent(placementRoot.transform, false);
            foreach (var row in occupied.GroupBy(c => new Vector2Int(c.y, c.z)))
            {
                var xs = row.Select(c => c.x).OrderBy(x => x).ToArray();
                var start = xs[0];
                var end = start;
                for (var i = 1; i <= xs.Length; i++)
                {
                    if (i < xs.Length && xs[i] == end + 1)
                    {
                        end = xs[i];
                        continue;
                    }
                    var collider = result.AddComponent<BoxCollider>();
                    if (collider == null)
                    {
                        DestroyImmediate(result);
                        Debug.LogWarning("Block Builder: チャンク Collider の生成に失敗したため個別 Collider を維持します", placementRoot);
                        return null;
                    }
                    collider.center = new Vector3((start + end + 1) * 0.5f, row.Key.x + 0.5f, row.Key.y + 0.5f) * grid;
                    collider.size = new Vector3(end - start + 1, 1, 1) * grid;
                    if (i < xs.Length)
                    {
                        start = end = xs[i];
                    }
                }
            }
            if (recordDerivedUndo)
            {
                Undo.RegisterCreatedObjectUndo(result, "Update chunk colliders");
            }
            EditorSceneManager.MarkSceneDirty(placementRoot.scene);
            return result;
        }

        /// <summary>
        /// 配置時の Collider 有効状態を管理名から復元する
        /// </summary>
        private void RestoreColliders(Block block)
        {
            for (var i = 0; i < block.colliders.Length; i++)
            {
                var enabled = i >= block.originalFlags.Length || block.originalFlags[i] == '1';
                if (block.colliders[i].enabled != enabled)
                {
                    if (recordDerivedUndo)
                    {
                        Undo.RecordObject(block.colliders[i], "Restore block colliders");
                    }
                    block.colliders[i].enabled = enabled;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(block.colliders[i]);
                }
            }
        }

        /// <summary>
        /// 中央照準からキャッシュ済みセルを辿って最も近いブロックを求める
        /// </summary>
        private void UpdateAim()
        {
            if (activeView == null || !rootValid)
            {
                return;
            }
            var worldRay = new Ray(cameraPosition, Quaternion.Euler(angles.x, angles.y, 0) * Vector3.forward);
            var ray = RootLocalRay(worldRay, out var distanceScale);
            var distance = grid * ReachInCells;
            var hitPoint = Vector3.zero;
            var normal = Vector3.up;
            var found = false;
            aimedBlock = null;
            foreach (var hit in Physics.RaycastAll(worldRay, distance / distanceScale, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(placementRoot.transform) || hit.collider.gameObject.scene != placementRoot.scene)
                {
                    continue;
                }
                if (hit.distance * distanceScale < distance)
                {
                    distance = hit.distance * distanceScale;
                    hitPoint = placementRoot.transform.InverseTransformPoint(hit.point);
                    normal = placementRoot.transform.localToWorldMatrix.transpose.MultiplyVector(hit.normal).normalized;
                    found = true;
                }
            }
            rayVisited.Clear();
            // 半セル刻みでは角のセルを飛ばすため、各軸の境界まで正確に進める
            var cell = CellOf(ray.origin);
            var step = new Vector3Int(ray.direction.x >= 0 ? 1 : -1, ray.direction.y >= 0 ? 1 : -1, ray.direction.z >= 0 ? 1 : -1);
            var next = new Vector3(BoundaryDistance(ray, cell, step, 0), BoundaryDistance(ray, cell, step, 1), BoundaryDistance(ray, cell, step, 2));
            var increments = new Vector3(AxisIncrement(ray.direction.x), AxisIncrement(ray.direction.y), AxisIncrement(ray.direction.z));
            var travelled = 0f;
            for (var i = 0; i < 512 && travelled <= distance; i++)
            {
                if (cells.TryGetValue(cell, out var occupants))
                {
                    foreach (var block in occupants)
                    {
                        if (!block.gameObject.activeInHierarchy || !rayVisited.Add(block) || !block.bounds.IntersectRay(ray, out var at) || at < 0 || at >= distance)
                        {
                            continue;
                        }
                        distance = at;
                        aimedBlock = block;
                        hitPoint = ray.GetPoint(at);
                        normal = BoundsNormal(block.bounds, hitPoint);
                        found = true;
                    }
                }
                var axis = next.x <= next.y && next.x <= next.z ? 0 : next.y <= next.z ? 1 : 2;
                travelled = next[axis];
                cell[axis] += step[axis];
                next[axis] += increments[axis];
            }
            if (!found && new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var planeDistance) && planeDistance <= distance)
            {
                hitPoint = ray.GetPoint(planeDistance);
                normal = Vector3.up;
                found = true;
            }
            candidate = CellOf(found ? hitPoint + normal * grid * 0.001f : ray.GetPoint(distance));
            aimedCell = aimedBlock != null ? CellOf(hitPoint - normal * grid * 0.001f) : candidate;
            hasCandidate = found && !cells.ContainsKey(candidate);
            UpdateStatistics();
        }

        /// <summary>
        /// ワールド Ray を配置ルートの座標へ変換し距離換算率を返す
        /// </summary>
        private Ray RootLocalRay(Ray worldRay, out float distanceScale)
        {
            var matrix = placementRoot.transform.worldToLocalMatrix;
            var direction = matrix.MultiplyVector(worldRay.direction);
            distanceScale = direction.magnitude;
            return new Ray(matrix.MultiplyPoint3x4(worldRay.origin), direction / distanceScale);
        }

        /// <summary>
        /// Ray が次のセル境界に達する距離を計算する
        /// </summary>
        private float BoundaryDistance(Ray ray, Vector3Int cell, Vector3Int step, int axis)
        {
            return Mathf.Abs(ray.direction[axis]) < 0.000001f ? float.PositiveInfinity
                : ((cell[axis] + (step[axis] > 0 ? 1 : 0)) * grid - ray.origin[axis]) / ray.direction[axis];
        }

        /// <summary>
        /// Ray が一セル進む軸別距離を返す
        /// </summary>
        private float AxisIncrement(float direction)
        {
            return Mathf.Abs(direction) < 0.000001f ? float.PositiveInfinity : grid / Mathf.Abs(direction);
        }

        /// <summary>
        /// AABB の交点に最も近い面の法線を求める
        /// </summary>
        private static Vector3 BoundsNormal(Bounds bounds, Vector3 point)
        {
            var relative = point - bounds.center;
            var distances = bounds.extents - new Vector3(Mathf.Abs(relative.x), Mathf.Abs(relative.y), Mathf.Abs(relative.z));
            var axis = distances.x <= distances.y && distances.x <= distances.z ? 0 : distances.y <= distances.z ? 1 : 2;
            var result = Vector3.zero;
            result[axis] = relative[axis] < 0 ? -1 : 1;
            return result;
        }

        /// <summary>
        /// 一回の右ボタン長押しを一つの Undo グループとして開始する
        /// </summary>
        private void BeginStroke()
        {
            EndStroke();
            Undo.IncrementCurrentGroup();
            strokeGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Place blocks");
            placing = true;
        }

        /// <summary>
        /// 連続配置の Undo を一操作にまとめる
        /// </summary>
        private void EndStroke()
        {
            placing = false;
            if (strokeGroup >= 0)
            {
                Undo.CollapseUndoOperations(strokeGroup);
                strokeGroup = -1;
                Undo.IncrementCurrentGroup();
            }
        }

        /// <summary>
        /// Prefab の元の向きに Y 回転を加え等比調整し全体の最下点を未占有セルの底面へ揃える
        /// </summary>
        private void PlaceCandidate()
        {
            var prefab = SlotPrefab(preferences.slot);
            if (!hasCandidate || prefab == null || !rootValid || cells.ContainsKey(candidate))
            {
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab, placementRoot.transform) as GameObject;
            if (instance == null)
            {
                return;
            }
            instance.transform.localRotation = Quaternion.Euler(0, preferences.rotationSteps * 90f, 0) * prefab.transform.localRotation;
            if (!TryBounds(instance, out var bounds))
            {
                DestroyImmediate(instance);
                status = "Renderer または Collider の範囲がある Prefab を指定してください";
                return;
            }
            if (preferences.fit)
            {
                instance.transform.localScale *= grid / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                TryBounds(instance, out bounds);
            }
            var cellBottomCenter = ((Vector3)candidate + new Vector3(0.5f, 0, 0.5f)) * grid;
            var modelBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            instance.transform.localPosition += cellBottomCenter - modelBottomCenter;
            TryBounds(instance, out bounds);
            var tolerance = Mathf.Min(BoundsTolerance(bounds), Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) * 0.25f);
            var min = CellOf(bounds.min + Vector3.one * tolerance);
            var max = CellOf(bounds.max - Vector3.one * tolerance);
            var volume = (long)(max.x - min.x + 1) * (max.y - min.y + 1) * (max.z - min.z + 1);
            var probe = new Block { min = min, max = max };
            if (volume > MaximumCellsPerBlock || BlockCells(probe).Any(cells.ContainsKey))
            {
                DestroyImmediate(instance);
                status = "配置範囲が既存ブロックと重なるか、大きすぎます";
                return;
            }
            var flags = string.Concat(instance.GetComponentsInChildren<Collider>(true).Select(c => c.enabled ? "1" : "0"));
            EnsureRootSettings();
            instance.name = prefab.name + "|" + BlockPrefix + flags;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            Undo.RegisterCreatedObjectUndo(instance, "Place block");
            status = "配置完了";
            RebuildCache();
            UpdateAim();
        }

        /// <summary>
        /// 専用ルート直下の管理対象ブロックだけを Undo 対応で破壊する
        /// </summary>
        private void DestroyAimedBlock()
        {
            if (aimedBlock == null || aimedBlock.gameObject == null || aimedBlock.gameObject.transform.parent != placementRoot.transform
                || ManagedName(aimedBlock.gameObject.name, BlockPrefix) == null)
            {
                return;
            }
            EndStroke();
            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(aimedBlock.gameObject);
            status = "破壊完了";
            aimedBlock = null;
            RebuildCache();
            UpdateAim();
        }

        /// <summary>
        /// ガイドとホットバーを通常の Selection に触れず描画する
        /// </summary>
        private void DuringSceneGUI(SceneView view)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            // Layout と Repaint でも GUI 原点が異なるため捕捉と位置確認は Repaint に揃える
            var control = GUIUtility.GetControlID("BlockBuilderCapture".GetHashCode(), FocusType.Passive);
            var aimScreenPoint = GUIUtility.GUIToScreenPoint(view.cameraViewport.center);
            if (pendingStartView == view && focusedWindow == view && Event.current.type == EventType.Repaint)
            {
                pendingStartView = null;
                pendingStartDeadline = 0;
                StartMode(view, control);
            }
            if (Event.current.type == EventType.Repaint && view == activeView
                && !cursorCapture.Matches(aimScreenPoint, EditorGUIUtility.pixelsPerPoint))
            {
                StopMode();
            }
            if (Event.current.type == EventType.Repaint && rootValid)
            {
                using var rootDrawingScope = new Handles.DrawingScope(placementRoot.transform.localToWorldMatrix);
                using (new Handles.DrawingScope(new Color(0.3f, 0.6f, 1f, 0.8f)))
                {
                    Handles.DrawWireCube(new Vector3((selectedChunk.x + 0.5f) * chunkSize * grid, 0, (selectedChunk.y + 0.5f) * chunkSize * grid),
                        new Vector3(chunkSize * grid, grid * 0.05f, chunkSize * grid));
                }
                if (selectingChunkBlocks && view == selectionView)
                {
                    foreach (var block in blocks.Where(candidateBlock => selectedForChunking.Contains(candidateBlock.gameObject)))
                    {
                        using (new Handles.DrawingScope(new Color(0.2f, 1f, 0.4f, 1f)))
                        {
                            Handles.DrawWireCube(block.bounds.center, block.bounds.size + Vector3.one * grid * 0.025f);
                        }
                    }
                    if (hoveredSelectionBlock != null && hoveredSelectionBlock.gameObject != null
                        && !selectedForChunking.Contains(hoveredSelectionBlock.gameObject))
                    {
                        using (new Handles.DrawingScope(Color.cyan))
                        {
                            Handles.DrawWireCube(hoveredSelectionBlock.bounds.center,
                                hoveredSelectionBlock.bounds.size + Vector3.one * grid * 0.035f);
                        }
                    }
                }
                if (view == activeView)
                {
                    if (hasCandidate)
                    {
                        using (new Handles.DrawingScope(preferences.guideColor))
                        {
                            var center = ((Vector3)candidate + Vector3.one * 0.5f) * grid;
                            Handles.DrawWireCube(center, Vector3.one * grid);
                            var direction = Quaternion.Euler(0, preferences.rotationSteps * 90f, 0) * Vector3.forward;
                            var side = Vector3.Cross(Vector3.up, direction);
                            var tip = center + direction * grid * 0.38f;
                            Handles.DrawLine(center - direction * grid * 0.3f, tip);
                            Handles.DrawLine(tip, tip - direction * grid * 0.16f + side * grid * 0.12f);
                            Handles.DrawLine(tip, tip - direction * grid * 0.16f - side * grid * 0.12f);
                        }
                    }
                    if (aimedBlock != null)
                    {
                        using (new Handles.DrawingScope(Color.yellow))
                        {
                            Handles.DrawWireCube(aimedBlock.bounds.center, aimedBlock.bounds.size + Vector3.one * grid * 0.015f);
                        }
                    }
                }
            }
            Handles.BeginGUI();
            if (!selectingChunkBlocks)
            {
                DrawHotbar(view);
            }
            var inPlacementMode = view == activeView && cameraSnapshot != null;
            var inChunkSelection = view == selectionView && selectingChunkBlocks;
            var modeRect = new Rect(view.cameraViewport.x + 8, 48, Mathf.Max(1, Mathf.Min(420, view.cameraViewport.width - 16)), 24);
            EditorGUI.DrawRect(modeRect, inChunkSelection ? new Color(0.32f, 0.24f, 0.08f, 0.92f)
                : inPlacementMode ? new Color(0.12f, 0.32f, 0.2f, 0.9f) : new Color(0, 0, 0, 0.65f));
            GUI.Label(modeRect, inChunkSelection ? "現在のモード: Collider チャンク対象選択（クリックで選択 / Alt で Scene 操作）"
                : inPlacementMode ? "現在のモード: FPS ブロック配置"
                : "現在のモード: 通常の Scene View（B キーで開始 / Block Builder ウィンドウでも可）");
            if (inChunkSelection)
            {
                var info = new Rect(view.cameraViewport.x + 8, 76, Mathf.Max(1, Mathf.Min(530, view.cameraViewport.width - 16)), 36);
                EditorGUI.DrawRect(info, new Color(0, 0, 0, 0.68f));
                GUI.Label(info, $"選択中 {selectedForChunking.Count} 個。クリックで選択を切り替え、ウィンドウの「選択を確定してチャンク化」を押してください。Esc でキャンセル");
            }
            if (inPlacementMode)
            {
                var center = GUIUtility.ScreenToGUIPoint(aimScreenPoint);
                EditorGUI.DrawRect(new Rect(center.x - 7, center.y - 1, 14, 2), Color.white);
                EditorGUI.DrawRect(new Rect(center.x - 1, center.y - 7, 2, 14), Color.white);
                var info = new Rect(view.cameraViewport.x + 8, 76, Mathf.Max(1, Mathf.Min(530, view.cameraViewport.width - 16)), 72);
                EditorGUI.DrawRect(info, new Color(0, 0, 0, 0.65f));
                var prefab = SlotPrefab(preferences.slot);
                GUI.Label(info, $"照準セル {aimedCell}   チャンク {ChunkOf(aimedCell)}\nスロット {preferences.slot + 1}: {(prefab != null ? prefab.name : "未登録")}   Y回転 {preferences.rotationSteps * 90}°\n中クリック / F: スポイト / R・Shift+R: 回転\n右: 配置 / 左: 破壊 / B・Esc・Alt: 終了");
            }
            Handles.EndGUI();
        }

        /// <summary>
        /// 下部にサムネイル付き九スロットを表示する
        /// </summary>
        private void DrawHotbar(SceneView view)
        {
            var width = Mathf.Min(54f, (view.position.width - 20) / 9f);
            var left = (view.position.width - width * 9) * 0.5f;
            for (var i = 0; i < 9; i++)
            {
                var rect = new Rect(left + width * i, view.position.height - width - 30, width - 2, width);
                var prefab = SlotPrefab(i);
                var thumbnail = prefab == null ? null : AssetPreview.GetAssetPreview(prefab) ?? AssetPreview.GetMiniThumbnail(prefab);
                var content = new GUIContent((i + 1).ToString(), thumbnail, prefab == null ? "未登録" : prefab.name);
                if (GUI.Button(rect, content) && activeView == null)
                {
                    ChangeSlot(i);
                }
                if (i == preferences.slot)
                {
                    var color = new Color(0.2f, 1f, 0.6f, 1f);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 3), color);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3, rect.width, 3), color);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), color);
                    EditorGUI.DrawRect(new Rect(rect.xMax - 3, rect.y, 3, rect.height), color);
                }
            }
        }

        /// <summary>
        /// キャッシュから配置範囲と操作対象を表示する
        /// </summary>
        private void UpdateStatistics()
        {
            UpdateColliderWorkflowUI();
            if (statistics == null)
            {
                return;
            }
            statistics.text = $"横幅 {cachedDimensions.x} / 奥行き {cachedDimensions.z} / 高さ {cachedDimensions.y} セル\nブロック数 {blocks.Count} / グリッド {grid} / チャンク幅 {chunkSize}\n照準セル {aimedCell} / チャンク {ChunkOf(aimedCell)}\n配置候補 {candidate} / Y回転 {preferences.rotationSteps * 90}°\n{status}";
        }
    }
}

