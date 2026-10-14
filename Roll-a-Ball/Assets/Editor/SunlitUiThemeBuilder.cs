using System;
using System.IO;
using System.Linq;
using Roll_a_Ball.OutGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>既存の画面とイベント参照を保ちながら、木漏れ日の絵本の共通 UI を適用する。</summary>
    public static class SunlitUiThemeBuilder
    {
        private const string Art = "Assets/Art/UI/SunlitStorybook";
        private const string Ui = "Assets/Prefabs/UI/";
        private const string GuidePath = Ui + "Hud/ControlsGuide.prefab";
        private static readonly Color Ink = Hex("344438");
        private static readonly Color Cream = Hex("F6F1DF");
        private static readonly Color Green = Hex("456646");
        private static readonly Color Sage = Hex("88A57A");
        private static TMP_FontAsset font;
        private static Sprite panel, primary, secondary, rounded, circle, coin, forest, ornament;

        [MenuItem("Tools/Roll-a-Ball/UI/木漏れ日の絵本を適用")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode を終了してから適用してください。");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("現在のシーンを保存してから適用してください。");
            LoadAssets();
            Edit("Screens/TitleScreen.prefab", Title);
            BuildHome();
            Edit("Common/StageSelectCoinDisplay.prefab", Currency);
            Edit("Hud/GameplayCoinDisplay.prefab", Currency);
            BuildGuide();
            Edit("Screens/StageSelectScreen.prefab", StageSelect);
            Edit("Dialogs/PauseDialog.prefab", Pause);
            Edit("Dialogs/ConfirmDialog.prefab", Confirmation);
            Edit("Dialogs/ShopPurchaseDialog.prefab", SharedDialogStyle);
            Edit("Settings/SettingsDialog.prefab", Settings);
            Edit("Results/ResultCanvas.prefab", Result);
            Edit("Hud/GameplayHud.prefab", Hud);
            BindGameplayScene();
            BindMenuScene();
            ApplySharedFeedback();
            BindAmbientAudio();
            AssetDatabase.SaveAssets();
            Debug.Log("木漏れ日の絵本 UI: Title / StageSelect / HUD / 操作ガイド / Pause / Settings / Result を適用しました。");
        }

        /// <summary>ゲーム内のコインを、そのメッシュとマテリアルのまま透過アイコンへ描画する。</summary>
        [MenuItem("Tools/Roll-a-Ball/UI/コインモデルを画像へ出力")]
        public static void ExportCoin()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InGame/10pt_coin.prefab");
            var mesh = source.GetComponent<MeshFilter>().sharedMesh;
            var material = source.GetComponent<Renderer>().sharedMaterial;
            var preview = new PreviewRenderUtility();
            Texture2D texture = null;
            try
            {
                preview.BeginPreview(new Rect(0, 0, 512, 512), GUIStyle.none);
                preview.camera.orthographic = true;
                preview.camera.orthographicSize = 0.83f;
                preview.camera.nearClipPlane = 0.01f;
                preview.camera.farClipPlane = 10f;
                preview.camera.transform.position = new Vector3(0, 0, -3);
                preview.camera.transform.rotation = Quaternion.identity;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = Color.clear;
                preview.ambientColor = new Color(0.42f, 0.42f, 0.38f);
                preview.lights[0].intensity = 1.3f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35, -35, 0);
                preview.lights[1].intensity = 0.6f;
                preview.lights[1].transform.rotation = Quaternion.Euler(340, 140, 0);
                preview.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 20, 0), source.transform.localScale), material, 0);
                preview.Render(true, false);
                var rendered = preview.EndPreview();
                var previousTarget = RenderTexture.active;
                try
                {
                    RenderTexture.active = (RenderTexture)rendered;
                    texture = new Texture2D(rendered.width, rendered.height, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, rendered.width, rendered.height), 0, 0);
                    texture.Apply();
                }
                finally { RenderTexture.active = previousTarget; }
                Directory.CreateDirectory(Art + "/Textures");
                File.WriteAllBytes(Art + "/Textures/UI-C-ICON_coin_diamond.png", texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                preview.Cleanup();
            }
            AssetDatabase.ImportAsset(Art + "/Textures/UI-C-ICON_coin_diamond.png", ImportAssetOptions.ForceUpdate);
        }

        private static void LoadAssets()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/static/NotoSansJP-Medium SDF.asset");
            if (font == null) throw new InvalidOperationException("日本語フォントがありません。");
            foreach (var path in Directory.GetFiles(Art + "/Textures", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var name = Path.GetFileName(path);
                importer.spriteBorder = name.Contains("birch_panel") ? Vector4.one * 180
                    : name.Contains("leaf_primary") ? new Vector4(260, 230, 260, 200)
                    : name.Contains("storybook_wood") ? new Vector4(330, 270, 330, 270)
                    : name.Contains("birch_secondary") ? new Vector4(190, 220, 190, 220)
                    : name.Contains("secondary") || name.Contains("rounded") || name.Contains("keycap") ? Vector4.one * 58 : Vector4.zero;
                importer.spritePixelsPerUnit = name.Contains("birch_panel") ? 400 : name.Contains("birch_secondary") ? 1200 : name.Contains("leaf_primary") ? 600 : 300;
                importer.SaveAndReimport();
            }
            panel = Sprite("UI-C-PANEL-01_birch_panel");
            primary = Sprite("UI-C-BTN-01_leaf_primary");
            secondary = Sprite("UI-C-BTN-02_birch_secondary"); rounded = Icon("rounded"); circle = Icon("circle");
            coin = Icon("coin_diamond"); forest = Sprite("UI-C-BG-01_sunlit_forest");
            ornament = Sprite("UI-C-DECOR-01_leaf_cluster");
        }

        private static void Edit(string path, Action<Transform> edit)
        {
            var contents = PrefabUtility.LoadPrefabContents(Ui + path);
            try
            {
                var scaler = contents.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }
                edit(contents.transform);
                PrefabUtility.SaveAsPrefabAsset(contents, Ui + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static void Currency(Transform root)
        {
            Style(root.GetComponent<TMP_Text>(), 32, Ink);
            root.GetComponent<TMP_Text>().text = "0";
            foreach (var component in root.GetComponents<MonoBehaviour>())
            {
                var serialized = new SerializedObject(component);
                var prefix = serialized.FindProperty("showPrefix");
                if (prefix == null) continue;
                prefix.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Title(Transform root)
        {
            foreach (var name in new[] { "ForestBackground", "LogoPanel", "Test_CurrentSceneName" })
            {
                var old = root.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            Ref(root.GetComponent<TitleScreen>(), "confirmSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/SE/Test/Test_OK_SE.mp3"));
            Center(Rect(root, "AnyPressKey"), 620, 90, 0, -155);
            var promptBase = Image(root.Find("AnyPressKey"), "PromptBackground", rounded, new Color(Ink.r, Ink.g, Ink.b, 0.78f));
            Stretch(promptBase.rectTransform); promptBase.transform.SetAsFirstSibling();
            var prompt = Text(root, "AnyPressKey/Label", 36, "Any Press Key");
            Stretch(prompt.rectTransform); prompt.color = Cream;
            var glowPath = Art + "/WarmPrompt.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(glowPath);
            if (material == null)
            {
                material = new Material(font.material) { name = "WarmPrompt" };
                AssetDatabase.CreateAsset(material, glowPath);
            }
            if (material.HasProperty("_GlowColor"))
            {
                material.EnableKeyword("GLOW_ON");
                material.SetColor("_GlowColor", Hex("EBC782"));
                material.SetFloat("_GlowOuter", 0.28f);
                material.SetFloat("_GlowPower", 0.25f);
            }
            prompt.fontSharedMaterial = material;
            Hide(root, "AnyPressKey");
        }

        private static void BuildHome()
        {
            const string path = "Assets/Resources/UI/ForestHomeTransition.prefab";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/UI"));
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject("ForestHomeTransition", typeof(RectTransform));
            try
            {
                var canvas = Get<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
                var scaler = Get<UnityEngine.UI.CanvasScaler>(root); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
                Get<UnityEngine.UI.GraphicRaycaster>(root);
                var transition = Get<ForestHomeTransition>(root);
                var visualRect = Ensure(root.transform, "Visuals"); Stretch(visualRect);
                var visual = Get<CanvasGroup>(visualRect.gameObject); visual.blocksRaycasts = false;
                var backdrop = Image(visualRect, "ForestBackdrop", forest, Color.white); Stretch(backdrop.rectTransform);
                var rig = Ensure(visualRect, "CameraRig"); Center(rig, 1920, 1080, 0, 0);
                var hole = new Rect(0.239f, 0.209f, 0.522f, 0.678f);
                var openingSize = new Vector2(hole.width * 1920, hole.height * 1080);
                var openingCenter = new Vector2((hole.center.x - 0.5f) * 1920, (hole.center.y - 0.5f) * 1080);
                var shutters = new RectTransform[2];
                for (var i = 0; i < shutters.Length; i++)
                {
                    var shutter = Get<UnityEngine.UI.RawImage>(Ensure(rig, i == 0 ? "LeftShutter" : "RightShutter").gameObject);
                    shutter.texture = Sprite("UI-T-SHUTTERS-02_pair").texture;
                    // 木の本体だけを使い、透過余白を開口部のサイズに含めない。
                    shutter.uvRect = new Rect(i * 0.5f, 0f, 0.5f, 1f);
                    shutter.raycastTarget = false;
                    Center(shutter.rectTransform, openingSize.x * 0.5f + 2, openingSize.y + 4,
                        openingCenter.x + (i == 0 ? -1 : 1) * openingSize.x * 0.5f, openingCenter.y);
                    shutter.rectTransform.pivot = new Vector2(i, 0.5f);
                    shutter.rectTransform.localScale = Vector3.one;
                    shutters[i] = shutter.rectTransform;
                }
                var frameRect = Ensure(rig, "RoomFrame"); Get<CanvasRenderer>(frameRect.gameObject);
                var frame = Get<WindowFrameGraphic>(frameRect.gameObject); Stretch(frame.rectTransform);
                frame.raycastTarget = false;
                var frameData = new SerializedObject(frame); frameData.FindProperty("roomTexture").objectReferenceValue = Sprite("UI-T-ROOM-02_decorated").texture;
                frameData.FindProperty("aperture").rectValue = hole; frameData.ApplyModifiedPropertiesWithoutUndo(); frame.SetAllDirty();
                var leaves = DecorateHomeWithIvy(rig);
                var titleRect = Ensure(rig, "Title"); Stretch(titleRect);
                var title = Get<CanvasGroup>(titleRect.gameObject); title.blocksRaycasts = false;
                var logo = Label(titleRect, "Wordmark", "roll-a-ball", 78, 760, 120, openingCenter.x, openingCenter.y + 140); logo.color = Cream;
                logo.characterSpacing = 7f;
                var crest = IconAt(titleRect, "LeafCrest", "leaf", 62, openingCenter.x, openingCenter.y + 250, Cream);
                crest.transform.localRotation = Quaternion.Euler(0, 0, -12);
                foreach (var side in new[] { -1, 1 })
                {
                    var rule = Image(titleRect, "Rule" + side, null, new Color(Cream.r, Cream.g, Cream.b, 0.65f));
                    Center(rule.rectTransform, 85, 1, side * 106, openingCenter.y + 250);
                }
                var logoPath = Art + "/TitleWordmark.mat";
                var logoMaterial = AssetDatabase.LoadAssetAtPath<Material>(logoPath);
                if (logoMaterial == null) { logoMaterial = new Material(font.material) { name = "TitleWordmark" }; AssetDatabase.CreateAsset(logoMaterial, logoPath); }
                if (logoMaterial.HasProperty("_OutlineWidth"))
                {
                    logoMaterial.SetFloat("_OutlineWidth", 0.12f); logoMaterial.SetColor("_OutlineColor", Ink);
                    logoMaterial.SetFloat("_OutlineSoftness", 0.12f);
                }
                logo.fontSharedMaterial = logoMaterial;
                var promptRect = Ensure(titleRect, "AnyPressKey"); Center(promptRect, 660, 80, openingCenter.x, openingCenter.y - 97);
                var prompt = Get<CanvasGroup>(promptRect.gameObject); prompt.blocksRaycasts = false;
                var text = Label(promptRect, "Label", "Any Press Key", 34, 620, 70, 0, 0); text.color = Cream;
                var warm = AssetDatabase.LoadAssetAtPath<Material>(Art + "/WarmPrompt.mat"); if (warm != null) text.fontSharedMaterial = warm;
                var black = Image(root.transform, "BlackFade", null, Color.black); Stretch(black.rectTransform); black.raycastTarget = true;
                var fade = Get<CanvasGroup>(black.gameObject); fade.alpha = 0f; fade.blocksRaycasts = false;
                var exit = Label(black.transform, "ExitLabel", "", 28, 820, 160, 0, 0); exit.color = Cream;
                Ref(transition, "cameraRig", rig); Ref(transition, "leftShutter", shutters[0]); Ref(transition, "rightShutter", shutters[1]);
                Refs(transition, "branches", leaves.Cast<UnityEngine.Object>().ToArray());
                Ref(transition, "visuals", visual); Ref(transition, "titleGroup", title); Ref(transition, "promptGroup", prompt);
                Ref(transition, "blackFade", fade); Ref(transition, "forestBackdrop", backdrop.gameObject); Ref(transition, "exitLabel", exit);
                var settings = new SerializedObject(transition); settings.FindProperty("openingCenter").vector2Value = openingCenter;
                settings.FindProperty("openingSize").vector2Value = openingSize; settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root); }
        }

        [MenuItem("Tools/Roll-a-Ball/UI/ツタの装飾を適用")]
        public static void ApplyIvyDecoration()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode を終了してから適用してください。");
            Edit("Screens/StageSelectScreen.prefab", root => DecorateStageWithIvy(root.Find("MenuBoard")));
            const string path = "Assets/Resources/UI/ForestHomeTransition.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var leaves = DecorateHomeWithIvy(root.transform.Find("Visuals/CameraRig"));
                Refs(root.GetComponent<ForestHomeTransition>(), "branches", leaves.Cast<UnityEngine.Object>().ToArray());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static RectTransform[] DecorateHomeWithIvy(Transform rig)
        {
            var leaves = new RectTransform[2];
            for (var i = 0; i < leaves.Length; i++)
            {
                var leaf = Image(rig, "ForegroundLeaves" + i, Sprite("UI-C-DECOR-02_ivy_hanging"), Color.white);
                Center(leaf.rectTransform, 240, 450, i == 0 ? -888 : 895, i == 0 ? 245 : -155);
                leaf.preserveAspect = true;
                leaf.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? -8 : 7);
                leaves[i] = leaf.rectTransform;
            }
            return leaves;
        }

        private static void DecorateStageWithIvy(Transform board)
        {
            Hide(board, "LeafTopRight"); Hide(board, "LeafBottomLeft");
            var names = new[] { "LeafTopLeft", "LeafBottomRight" };
            for (var i = 0; i < names.Length; i++)
            {
                var leaf = Image(board, names[i], Sprite("UI-C-DECOR-04_ivy_corner"), Color.white);
                Center(leaf.rectTransform, 310, 310, i == 0 ? -585 : 585, i == 0 ? 295 : -295);
                leaf.gameObject.SetActive(true); leaf.preserveAspect = true;
                leaf.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 0 : 180);
            }
        }

        private static void StageSelect(Transform root)
        {
            Skin(root.Find("Background").GetComponent<UnityEngine.UI.Image>(), forest, Color.white, false);
            var wash = Image(root, "WarmWash", null, new Color(Cream.r, Cream.g, Cream.b, 0.79f));
            Stretch(wash.rectTransform); wash.transform.SetSiblingIndex(1);
            var board = Image(root, "MenuBoard", panel, Color.white);
            Center(board.rectTransform, 1480, 900, 0, -5); board.transform.SetSiblingIndex(2);
            DecorateStageWithIvy(board.transform);
            var titlePlaque = Image(root, "TitlePlaque", secondary, Color.white);
            Center(titlePlaque.rectTransform, 675, 125, 0, 370); titlePlaque.transform.SetSiblingIndex(3);
            titlePlaque.gameObject.SetActive(false);
            Hide(root, "SectionLabel");
            Text(root, "Title", 40, "ステージセレクト"); Center(Rect(root, "Title"), 660, 70, 0, 352);
            var wallet = Image(root, "WalletBackground", primary, Color.white);
            Skin(wallet, rounded, Hex("EAE2CD"));
            Center(wallet.rectTransform, 220, 66, -540, 352); wallet.transform.SetSiblingIndex(3);
            var currency = Text(root, "CurrencyLabel", 32); Center(currency.rectTransform, 164, 48, -724, 432);
            Center(currency.rectTransform, 134, 48, -512, 352); currency.color = Ink;
            IconAt(root, "WalletCoin", "coin_diamond", 42, -611, 352);
            Button(root, "SettingsButton", 68, 68, 612, 352, false);
            Skin(root.Find("SettingsButton").GetComponent<UnityEngine.UI.Image>(), circle, Cream, false);
            root.Find("SettingsButton").GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
            root.Find("SettingsButton").GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Hide(root, "SettingsButton/Label"); IconAt(root.Find("SettingsButton"), "Gear", "settings", 40, 0, 0);
            Button(root, "ShopButton", 320, 86, -705, -448, false);
            Button(root, "CustomizeButton", 340, 86, 677, -448, false);
            var card = root.Find("StageCard"); Center((RectTransform)card, 1040, 650, 0, -35);
            card.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            var namePlaque = Image(card, "StageNamePlaque", secondary, Color.white);
            Center(namePlaque.rectTransform, 650, 95, 0, -190); namePlaque.transform.SetAsFirstSibling();
            namePlaque.gameObject.SetActive(false);
            var previewFrame = Image(card, "PreviewFrame", panel, Color.white);
            Center(previewFrame.rectTransform, 952, 392, 0, 27); previewFrame.transform.SetSiblingIndex(1);
            Text(root, "StageCard/StageNameLabel", 34); Center(Rect(root, "StageCard/StageNameLabel"), 600, 54, 0, -190);
            Text(root, "StageCard/StageNumberLabel", 21); Center(Rect(root, "StageCard/StageNumberLabel"), 195, 34, -333, 263);
            Center(Rect(root, "StageCard/StagePreviewButton"), 916, 352, 0, 27);
            Button(root, "StageCard/PlayButton", 390, 110, 0, -285, true);
            var locked = root.Find("StageCard/StagePreviewButton/LockedOverlay");
            Skin(locked.GetComponent<UnityEngine.UI.Image>(), null, new Color(Green.r, Green.g, Green.b, 0.88f), false);
            IconAt(locked, "LockIcon", "lock", 64, 0, 45);
            var lockText = Text(root, "StageCard/StagePreviewButton/LockedOverlay/LockLabel", 25, "ひとつ前のステージを\nクリアして解放");
            Center(lockText.rectTransform, 590, 82, 0, -44); lockText.color = Cream;
            Button(root, "PreviousButton", 76, 76, -574, -8, false);
            Button(root, "NextButton", 76, 76, 574, -8, false);
            foreach (var direction in new[] { "PreviousButton", "NextButton" })
            {
                Skin(root.Find(direction).GetComponent<UnityEngine.UI.Image>(), circle, Cream, false);
                root.Find(direction).GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
                root.Find(direction).GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
                Hide(root, direction + "/Label");
                IconAt(root.Find(direction), "Arrow", direction == "PreviousButton" ? "left" : "right", 38, 0, 0);
            }
            var rank = root.Find("RankingPanel"); Center((RectTransform)rank, 365, 380, 635, -35);
            rank.gameObject.SetActive(false);
            Skin(rank.GetComponent<UnityEngine.UI.Image>(), panel, Color.white);
            Text(root, "RankingPanel/RankingTitle", 27, "ベストタイム"); Center(Rect(root, "RankingPanel/RankingTitle"), 290, 46, 0, 123);
            for (var i = 1; i <= 3; i++)
            {
                Text(root, "RankingPanel/Time" + i, 25); Center(Rect(root, "RankingPanel/Time" + i), 290, 44, 0, 63 - i * 60);
            }
            var status = Label(card, "StatusLabel", "選択中", 21, 124, 34, 340, 263);
            var statusIcon = IconAt(card, "StatusIcon", "leaf", 26, 257, 263);
            var presentation = Get<StageCardPresentation>(root.gameObject);
            Ref(presentation, "statusLabel", status); Ref(presentation, "statusIcon", statusIcon);
            foreach (Transform child in card) if (child.name.StartsWith("StageDot")) child.gameObject.SetActive(false);
            Refs(presentation, "stageDots", Array.Empty<UnityEngine.Object>());
            Ref(presentation, "lockSprite", Icon("lock")); Ref(presentation, "checkSprite", Icon("check")); Ref(presentation, "leafSprite", Icon("leaf"));
            Ref(root.GetComponent<StageSelectScreen>(), "stagePresentation", presentation);
            var serialized = new SerializedObject(root.GetComponent<StageSelectScreen>());
            var stages = serialized.FindProperty("stages");
            var names = new[] { "木漏れ日の林道", "迷いの密集林", "せせらぎの丸太橋", "険しい山道", "天空の頂上" };
            for (var i = 0; i < stages.arraySize; i++)
            {
                var stage = stages.GetArrayElementAtIndex(i);
                var number = stage.FindPropertyRelative("stageNumber").intValue;
                if (number > 0 && number <= names.Length) stage.FindPropertyRelative("displayName").stringValue = names[number - 1];
                // 実際のステージ画像を優先する。未設定の場合だけ共通の森の絵を使う。
                var thumbnail = stage.FindPropertyRelative("previewTexture");
                if (thumbnail.objectReferenceValue == null || AssetDatabase.GetAssetPath(thumbnail.objectReferenceValue).EndsWith("OutGameTestPreview.asset"))
                    thumbnail.objectReferenceValue = forest.texture;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Pause(Transform root)
        {
            Skin(root.GetComponent<UnityEngine.UI.Image>(), null, new Color(Green.r, Green.g, Green.b, 0.55f), false);
            Hide(root, "Shadow"); var body = root.Find("Panel"); Center((RectTransform)body, 640, 640, 0, 0);
            Skin(body.GetComponent<UnityEngine.UI.Image>(), panel, Color.white);
            Hide(body, "LeafCornerRight"); Hide(body, "LeafCornerBottom");
            Skin(root.Find("Panel/Ball").GetComponent<UnityEngine.UI.Image>(), Icon("leaf"), Color.white, false); Hide(root, "Panel/Ball/Ball Shine");
            Hide(root, "Panel/Ball");
            Hide(root, "Panel/Brand"); Text(root, "Panel/Heading", 44, "ポーズ");
            Center(Rect(root, "Panel/Heading"), 500, 72, 0, 187);
            Hide(root, "Panel/Caption");
            Button(root, "Panel/ResumeButton", 430, 100, 0, 44, true);
            Button(root, "Panel/SettingsButton", 430, 86, 0, -51, false);
            Button(root, "Panel/StageSelectButton", 430, 86, 0, -148, false);
            Text(root, "Panel/Hint", 19, "M / Esc でゲームに戻る"); Center(Rect(root, "Panel/Hint"), 500, 36, 0, -243);
            Close(root, "Panel/CloseButton", 250, 244);
        }

        private static void SharedDialogStyle(Transform root)
        {
            Skin(root.GetComponent<UnityEngine.UI.Image>(), null, new Color(Green.r, Green.g, Green.b, 0.55f), false);
            var body = root.Find("Panel");
            Skin(body.GetComponent<UnityEngine.UI.Image>(), panel, Color.white);
            foreach (var text in body.GetComponentsInChildren<TMP_Text>(true)) Style(text, text.fontSize, Ink);
            foreach (var button in body.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var rect = (RectTransform)button.transform;
                Button(body, button.name, rect.rect.width, Mathf.Max(90, rect.rect.height), rect.anchoredPosition.x, rect.anchoredPosition.y, button.name != "CancelButton", 26);
            }
        }

        private static void Confirmation(Transform root)
        {
            SharedDialogStyle(root);
            var body = root.Find("Panel");
            Center((RectTransform)body, 720, 420, 0, 0);
            var message = Text(body, "Message", 30, "音量を初期設定に戻しますか？");
            Center(message.rectTransform, 620, 100, 0, 55);
            Button(body, "CancelButton", 280, 100, -150, -112, false, 26);
            Button(body, "ConfirmButton", 280, 100, 150, -112, true, 26);
            Text(body, "ConfirmButton/Label", 26, "初期設定に戻す").color = Cream;
        }

        private static void Settings(Transform root)
        {
            var dialog = root.Find("SettingsDialog");
            Skin(dialog.GetComponent<UnityEngine.UI.Image>(), null, new Color(Green.r, Green.g, Green.b, 0.55f), false);
            Hide(dialog, "Shadow"); var body = dialog.Find("Panel"); Center((RectTransform)body, 780, 730, 0, 0);
            Skin(body.GetComponent<UnityEngine.UI.Image>(), panel, Color.white);
            Hide(body, "LeafCorner");
            Skin(body.Find("Mint Ball").GetComponent<UnityEngine.UI.Image>(), Icon("leaf"), Color.white, false); Hide(body, "Mint Ball/Shine");
            Hide(body, "Mint Ball");
            foreach (var text in body.GetComponentsInChildren<TMP_Text>(true)) Style(text, text.fontSize, Ink);
            Text(body, "Heading", 42, "設定"); Text(body, "Brand", 18); Text(body, "Context", 20);
            Center(Rect(body, "Heading"), 500, 65, 0, 265); Center(Rect(body, "Brand"), 350, 28, 0, 244);
            Hide(body, "Brand"); Hide(body, "Context");
            if (body.Find("AmbientSlider") == null)
            {
                foreach (var source in new[] { "SeCard", "SeLabel", "SeValue", "SeSlider" })
                {
                    var copy = UnityEngine.Object.Instantiate(body.Find(source).gameObject, body);
                    copy.name = source.Replace("Se", "Ambient");
                }
            }
            Ref(root.GetComponent<SettingsDialogController>(), "ambientSlider", body.Find("AmbientSlider").GetComponent<UnityEngine.UI.Slider>());
            Ref(root.GetComponent<SettingsDialogController>(), "ambientValue", body.Find("AmbientValue").GetComponent<TMP_Text>());
            Text(body, "BgmLabel", 25, "BGM 音量"); Text(body, "SeLabel", 25, "効果音 音量");
            foreach (var card in new[] { "BgmCard", "SeCard" }) Skin(body.Find(card).GetComponent<UnityEngine.UI.Image>(), rounded, Hex("F0E7CF"));
            Slider(body.Find("BgmSlider")); Slider(body.Find("SeSlider"));
            Hide(body, "BgmCaption");
            var rows = new[] { "Bgm", "Se", "Ambient" };
            for (var row = 0; row < rows.Length; row++)
            {
                var prefix = rows[row]; var y = 152 - row * 114;
                Center(Rect(body, prefix + "Card"), 690, 100, 0, y);
                Skin(body.Find(prefix + "Card").GetComponent<UnityEngine.UI.Image>(), rounded, Hex("F0E7CF"));
                body.Find(prefix + "Card").SetAsFirstSibling();
                Center(Rect(body, prefix + "Label"), 400, 38, -48, y + 25);
                Center(Rect(body, prefix + "Value"), 100, 38, 270, y + 25);
                Center(Rect(body, prefix + "Slider"), 478, 36, -48, y - 22);
                Slider(body.Find(prefix + "Slider"));
                Mute(body, prefix + "Mute", body.Find(prefix + "Slider").GetComponent<UnityEngine.UI.Slider>(), y - 22);
            }
            Text(body, "AmbientLabel", 25, "環境音");
            Button(body, "TestSeButton", 132, 52, 265, -156, false, 18);
            Skin(body.Find("TestSeButton").GetComponent<UnityEngine.UI.Image>(), rounded, Hex("EAE2CD"));
            body.Find("TestSeButton").GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Text(body, "TestSeButton/Label", 18, "音を確認");
            Text(body, "SaveHint", 19, "変更は自動で保存されます");
            Button(body, "DefaultsButton", 282, 82, -205, -218, false, 22);
            Button(body, "BackButton", 390, 96, 150, -218, true, 22);
            var quit = Ensure(body, "QuitButton"); Get<UnityEngine.UI.Image>(quit.gameObject);
            var quitButton = Get<UnityEngine.UI.Button>(quit.gameObject);
            Label(quit, "Label", "ゲームをやめる", 23, 500, 40, 0, 0);
            Button(body, "QuitButton", 600, 64, 0, -295, false, 23);
            Skin(quit.GetComponent<UnityEngine.UI.Image>(), rounded, Hex("EAE2CD")); quit.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Text(body, "QuitButton/Label", 23, "ゲームをやめる").color = Hex("805E45");
            for (var i = quitButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEditor.Events.UnityEventTools.RemovePersistentListener(quitButton.onClick, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(quitButton.onClick, root.GetComponent<SettingsDialogController>().QuitGame);
            Close(body, "CloseButton", 330, 290);
        }

        private static void Slider(Transform root)
        {
            root.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            Skin(root.Find("Track").GetComponent<UnityEngine.UI.Image>(), rounded, Hex("CCBE9F"));
            Skin(root.Find("Fill Area/Fill").GetComponent<UnityEngine.UI.Image>(), rounded, Sage);
            root.Find("Track").GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = 4;
            root.Find("Fill Area/Fill").GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = 4;
            Skin(root.Find("Handle Area/Handle").GetComponent<UnityEngine.UI.Image>(), circle, Cream, false);
            Skin(root.Find("Handle Area/Handle/Handle Dot").GetComponent<UnityEngine.UI.Image>(), circle, Hex("C99C64"), false);
            root.Find("Handle Area/Handle").GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        }

        private static void Mute(Transform body, string name, UnityEngine.UI.Slider slider, float y)
        {
            var background = Image(body, name, rounded, Cream); Center(background.rectTransform, 108, 44, 270, y);
            var toggle = Get<UnityEngine.UI.Toggle>(background.gameObject);
            background.raycastTarget = true; toggle.targetGraphic = background;
            var indicator = Image(background.transform, "Indicator", circle, Green); Center(indicator.rectTransform, 15, 15, -32, 0);
            toggle.graphic = indicator; toggle.isOn = true;
            var label = Label(background.transform, "State", "ON", 19, 62, 30, 14, 0);
            var binding = Get<VolumeMuteToggle>(background.gameObject);
            Ref(binding, "volumeSlider", slider); Ref(binding, "soundToggle", toggle); Ref(binding, "stateLabel", label);
        }

        private static void Result(Transform root)
        {
            var backdrop = Image(root, "Backdrop", null, new Color(Green.r, Green.g, Green.b, 0.58f));
            Stretch(backdrop.rectTransform); backdrop.transform.SetAsFirstSibling();
            var body = root.Find("ResultPanel"); Center((RectTransform)body, 730, 720, 0, 0);
            Skin(body.GetComponent<UnityEngine.UI.Image>(), panel, Color.white);
            Hide(body, "LeafCornerTopLeft"); Hide(body, "LeafCornerBottomRight");
            Text(body, "ClearText", 48, "STAGE CLEAR"); Center(Rect(body, "ClearText"), 550, 70, 0, 258);
            Label(body, "TimeCaption", "クリアタイム", 22, 380, 32, 0, 187);
            Label(body, "ResultTime", "00:00", 62, 420, 78, 0, 135);
            Label(body, "CoinsCaption", "このステージで集めたコイン", 23, 550, 40, 0, 49);
            IconAt(body, "CoinIcon", "coin_diamond", 70, -82, -5);
            Label(body, "ResultCoins", "+0", 48, 240, 70, 61, -5);
            Label(body, "WalletCaption", "所持コイン", 21, 400, 30, 0, -76);
            Label(body, "ResultWallet", "0 → 0", 34, 580, 55, 0, -118);
            Button(body, "StageSelectButton", 365, 110, -122, -251, true, 26);
            Text(body, "StageSelectButton/StageSelect", 26, "ステージセレクトへ").color = Cream;
            Button(body, "RetryButton", 207, 90, 216, -251, false, 25);
            Text(body, "RetryButton/RetryText (TMP)", 25, "もう一度");
            Hide(body, "LeafLeft"); Hide(body, "LeafRight");
        }

        private static void Hud(Transform root)
        {
            var coinBackground = root.Find("CoinBackground").GetComponent<UnityEngine.UI.Image>();
            Skin(coinBackground, rounded, new Color(Cream.r, Cream.g, Cream.b, 0.94f)); Corner(coinBackground.rectTransform, Vector2.up, 220, 72, 24, -24);
            var coinImage = Image(root, "CoinIcon", coin, Color.white); Corner(coinImage.rectTransform, Vector2.up, 38, 44, 40, -38);
            var amount = Text(root, "GameplayCoinDisplay", 32, "0"); Corner(amount.rectTransform, Vector2.up, 136, 44, 92, -38);
            amount.alignment = TextAlignmentOptions.MidlineLeft;
            amount.color = Ink;
            var banner = Image(root, "StageBanner", rounded, new Color(Ink.r, Ink.g, Ink.b, 0.84f));
            Corner(banner.rectTransform, new Vector2(0.5f, 1), 350, 72, 0, -24);
            Label(banner.transform, "StageName", "木漏れ日の林道", 25, 318, 34, 0, 10).color = Cream;
            Label(banner.transform, "JourneyLabel", "STAGE 01", 17, 200, 24, 0, -20).color = Cream;
            foreach (Transform child in banner.transform) if (child.name.StartsWith("StageDot")) child.gameObject.SetActive(false);
            var oldGuide = root.Find("ControlsGuide");
            if (oldGuide == null)
            {
                var guide = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GuidePath), root);
                guide.name = "ControlsGuide"; Stretch((RectTransform)guide.transform);
            }
        }

        private static void BuildGuide()
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuidePath) != null;
            var contents = existingPrefab ? PrefabUtility.LoadPrefabContents(GuidePath) : new GameObject("ControlsGuide", typeof(RectTransform));
            try
            {
                Stretch((RectTransform)contents.transform);
                var move = Image(contents.transform, "MoveGroup", rounded, new Color(Ink.r, Ink.g, Ink.b, 0.83f));
                Corner(move.rectTransform, Vector2.zero, 270, 150, 24, 24);
                Label(move.transform, "Caption", "移動", 19, 130, 26, 0, 50).color = Cream;
                // W/A/S/D のキーのすぐ下に対応する矢印キーを置く。入力の追加は別対応。
                Key(move.transform, "W", "W", -81, 10); Key(move.transform, "Up", "↑", -81, -34);
                Key(move.transform, "A", "A", -27, 10); Key(move.transform, "Left", "←", -27, -34);
                Key(move.transform, "S", "S", 27, 10); Key(move.transform, "Down", "↓", 27, -34);
                Key(move.transform, "D", "D", 81, 10); Key(move.transform, "Right", "→", 81, -34);
                var look = Image(contents.transform, "LookGroup", rounded, new Color(Ink.r, Ink.g, Ink.b, 0.83f));
                Corner(look.rectTransform, Vector2.right, 260, 68, -24, 24);
                foreach (var name in new[] { "Mouse", "RotateLeft", "RotateRight", "CameraCaption" })
                {
                    var unused = look.transform.Find(name);
                    if (unused != null) UnityEngine.Object.DestroyImmediate(unused.gameObject);
                }
                Key(look.transform, "M", "M", -82, 0); Key(look.transform, "Esc", "Esc", -21, 0, 64);
                Label(look.transform, "MenuCaption", "メニュー", 20, 85, 34, 69, 0).color = Cream;
                PrefabUtility.SaveAsPrefabAsset(contents, GuidePath);
            }
            finally
            {
                if (existingPrefab) PrefabUtility.UnloadPrefabContents(contents);
                else UnityEngine.Object.DestroyImmediate(contents);
            }
        }

        private static void Key(Transform parent, string name, string label, float x, float y, float width = 42)
        {
            var background = Image(parent, "Key" + name, Icon("keycap"), Color.white);
            background.type = UnityEngine.UI.Image.Type.Simple;
            Center(background.rectTransform, width, 40, x, y);
            var text = Label(background.transform, "Label", label, label.Length > 1 ? 17 : 23, width - 8, 34, 0, 2);
            text.fontStyle = FontStyles.Bold;
        }

        private static void ApplySharedFeedback()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    {
                        var animation = Get<ButtonPressAnimation>(button.gameObject);
                        var settings = new SerializedObject(animation);
                        settings.FindProperty("hoverScale").floatValue = 1.018f;
                        settings.FindProperty("pressedScale").vector2Value = new Vector2(0.975f, 0.975f);
                        settings.FindProperty("clickOvershootScale").floatValue = 1.028f;
                        settings.FindProperty("pressDuration").floatValue = 0.065f;
                        settings.FindProperty("releaseDuration").floatValue = 0.17f;
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        var sound = Get<ButtonSoundEffect>(button.gameObject);
                        Ref(sound, "clickSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/SE/Test/Test_OK_SE.mp3"));
                    }
                    var dialog = root.GetComponent<SettingsDialogController>() != null ? root.transform.Find("SettingsDialog") : root.transform;
                    var panelRoot = dialog.Find("Panel") ?? dialog.Find("ResultPanel");
                    if (panelRoot != null && (path.Contains("Dialogs/") || path.Contains("Settings/") || path.Contains("Results/")))
                    {
                        var motion = Get<DialogMotion>(dialog.gameObject);
                        Ref(motion, "panel", panelRoot);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static void BindAmbientAudio()
        {
            const string path = "Assets/Prefabs/UI/Common/ForestAmbience.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject("ForestAmbience");
            try
            {
                var names = new[] { "ForestSound", "BirdSong" };
                for (var i = 0; i < names.Length; i++)
                {
                    var child = root.transform.Find(names[i]);
                    if (child == null) { child = new GameObject(names[i]).transform; child.SetParent(root.transform, false); }
                    var source = Get<AudioSource>(child.gameObject);
                    source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/BGM/Stage1/Nature - Essentials/" + names[i] + ".mp3");
                    source.loop = true; source.playOnAwake = true; source.spatialBlend = 0f; source.ignoreListenerPause = true;
                    var binding = Get<AudioVolumeBinding>(child.gameObject);
                    var settings = new SerializedObject(binding);
                    settings.FindProperty("channel").enumValueIndex = (int)AudioVolumeBinding.Channel.Es;
                    settings.FindProperty("baseVolume").floatValue = i == 0 ? 0.4f : 0.18f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root); }
            var previous = SceneManager.GetActiveScene().path;
            foreach (var scenePath in new[] { "Assets/Scenes/GameplayScene.unity", "Assets/Scenes/HomeScene.unity" })
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                if (!scene.GetRootGameObjects().Any(g => g.name == "ForestAmbience"))
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
        }

        private static void BindMenuScene()
        {
            const string menuPath = "Assets/Scenes/HomeScene.unity";
            var previous = SceneManager.GetActiveScene().path;
            var scene = previous == menuPath ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(menuPath);
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ScreenManager>(true)).Single();
            var title = AssetDatabase.LoadAssetAtPath<GameObject>(Ui + "Screens/TitleScreen.prefab").GetComponent<TitleScreen>();
            var stageSelect = AssetDatabase.LoadAssetAtPath<GameObject>(Ui + "Screens/StageSelectScreen.prefab").GetComponent<StageSelectScreen>();
            var settings = new SerializedObject(manager);
            var screens = settings.FindProperty("screenPrefabs");
            if (!Enumerable.Range(0, screens.arraySize).Any(i => screens.GetArrayElementAtIndex(i).objectReferenceValue == title))
            {
                screens.arraySize++;
                screens.GetArrayElementAtIndex(screens.arraySize - 1).objectReferenceValue = title;
            }
            settings.FindProperty("initialScreen").objectReferenceValue = title;
            settings.FindProperty("screenBehindInitial").objectReferenceValue = stageSelect;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(menuPath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != menuPath && s.path != "Assets/Scenes/TitleScene.unity")).ToArray();
            if (!string.IsNullOrEmpty(previous) && previous != menuPath) EditorSceneManager.OpenScene(previous);
        }

        private static void BindGameplayScene()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = previous == "Assets/Scenes/GameplayScene.unity" ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
            var hud = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "GameplayHud").transform;
            var result = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "ResultCanvas");
            var session = Get<StageSessionPresentation>(hud.gameObject);
            Ref(session, "stageName", hud.Find("StageBanner/StageName").GetComponent<TMP_Text>());
            Ref(session, "journeyLabel", hud.Find("StageBanner/JourneyLabel").GetComponent<TMP_Text>());
            Refs(session, "stageDots", Array.Empty<UnityEngine.Object>());
            Ref(session, "resultTime", result.Find("ResultPanel/ResultTime").GetComponent<TMP_Text>());
            Ref(session, "resultCoins", result.Find("ResultPanel/ResultCoins").GetComponent<TMP_Text>());
            Ref(session, "resultWallet", result.Find("ResultPanel/ResultWallet").GetComponent<TMP_Text>());
            var source = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(Ui + "Screens/StageSelectScreen.prefab").GetComponent<StageSelectScreen>());
            var destination = new SerializedObject(session); destination.CopyFromSerializedProperty(source.FindProperty("stages")); destination.ApplyModifiedPropertiesWithoutUndo();
            var pause = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PauseMenuController>(true)).First();
            Ref(pause, "controlsGuide", hud.Find("ControlsGuide").gameObject);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != scene.path) EditorSceneManager.OpenScene(previous);
        }

        private static void Close(Transform root, string path, float x, float y)
        {
            Button(root, path, 60, 60, x, y, false);
            var background = root.Find(path).GetComponent<UnityEngine.UI.Image>();
            Skin(background, circle, Cream, false); background.raycastTarget = true;
            Hide(root, path + "/Label"); IconAt(root.Find(path), "CloseIcon", "close", 27, 0, 0);
        }

        private static UnityEngine.Object[] Dots(Transform root, float y, float size = 13, float spacing = 26)
        {
            return Enumerable.Range(0, 5).Select(i => (UnityEngine.Object)IconAt(root, "StageDot" + i, "circle", size, (i - 2) * spacing, y, i == 0 ? Green : Hex("C9BD9E"))).ToArray();
        }

        private static void Button(Transform root, string path, float width, float height, float x, float y, bool main, float size = 28)
        {
            var target = root.Find(path); if (target == null) throw new InvalidOperationException(path);
            Center((RectTransform)target, width, height, x, y);
            var image = target.GetComponent<UnityEngine.UI.Image>(); Skin(image, main ? primary : secondary, Color.white); image.raycastTarget = true;
            var button = target.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 0.95f, 0.82f); colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.8f, 0.87f, 0.76f); colors.disabledColor = new Color(0.75f, 0.75f, 0.7f, 0.55f); colors.fadeDuration = 0.12f; button.colors = colors;
            foreach (var text in target.GetComponentsInChildren<TMP_Text>(true))
            {
                Style(text, size, main ? Cream : Ink); Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(18, main ? 16 : 9); text.rectTransform.offsetMax = new Vector2(-18, main ? -16 : -9);
            }
        }

        private static TMP_Text Text(Transform root, string path, float size, string value = null)
        {
            var text = root.Find(path).GetComponent<TMP_Text>(); Style(text, size, Ink); if (value != null) text.text = value; return text;
        }
        private static UnityEngine.UI.Image Ornament(Transform root, string name, float size, float x, float y)
        {
            var image = Image(root, name, ornament, Color.white); Center(image.rectTransform, size, size, x, y); image.preserveAspect = true; return image;
        }
        private static TMP_Text Label(Transform root, string name, string value, float size, float width, float height, float x, float y)
        {
            var rect = Ensure(root, name); var text = Get<TextMeshProUGUI>(rect.gameObject); Style(text, size, Ink); text.text = value; Center(rect, width, height, x, y); return text;
        }
        private static void Style(TMP_Text text, float size, Color color)
        {
            text.font = font; text.fontSharedMaterial = font.material; text.fontSize = size; text.color = color;
            text.fontStyle = FontStyles.Normal; text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
        }
        private static UnityEngine.UI.Image IconAt(Transform root, string name, string icon, float size, float x, float y, Color? color = null)
        {
            var image = Image(root, name, Icon(icon), color ?? Color.white); Center(image.rectTransform, size, size, x, y); image.preserveAspect = true; return image;
        }
        private static UnityEngine.UI.Image Image(Transform root, string name, Sprite sprite, Color color)
        {
            var image = Get<UnityEngine.UI.Image>(Ensure(root, name).gameObject); Skin(image, sprite, color, sprite != null && sprite.border != Vector4.zero); return image;
        }
        private static void Skin(UnityEngine.UI.Image image, Sprite sprite, Color color, bool sliced = true)
        {
            image.sprite = sprite; image.color = color; image.type = sliced && sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = false;
        }
        private static RectTransform Ensure(Transform root, string name)
        {
            var existing = root.Find(name); if (existing != null) return (RectTransform)existing;
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(root, false); return rect;
        }
        private static RectTransform Rect(Transform root, string path) => (RectTransform)root.Find(path);
        private static T Get<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
        private static void Center(RectTransform rect, float width, float height, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y); rect.localScale = Vector3.one;
        }
        private static void Corner(RectTransform rect, Vector2 anchor, float width, float height, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y); rect.localScale = Vector3.one;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * 0.5f; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        }
        private static void Hide(Transform root, string path) { var target = root.Find(path); if (target != null) target.gameObject.SetActive(false); }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Textures/" + name + ".png") ?? throw new InvalidOperationException("画像がありません: " + name);
        private static Sprite Icon(string name) => Sprite("UI-C-ICON_" + name);
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        private static void Ref(Component target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Refs(Component target, string field, UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(field); property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
