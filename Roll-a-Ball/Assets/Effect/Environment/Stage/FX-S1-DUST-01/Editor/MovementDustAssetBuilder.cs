using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 微細粉塵アセットとEffectTestScene専用の確認展示を作成する
/// </summary>
public static class MovementDustAssetBuilder
{
    private const string Root = "Assets/Effect/Environment/Stage/FX-S1-DUST-01";
    private const string ScenePath = "Assets/Scenes/TestScene/EffectTestScene.unity";

    /// <summary>
    /// 指定テストシーンだけに粉塵展示を追加し、参照を設定して保存する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Create Movement Dust Preview")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
        {
            Debug.LogError("Edit ModeでEffectTestSceneを開いてから実行してください。");
            return;
        }

        var templateButton = GameObject.Find("Stage2WindButton");
        var canvas = GameObject.Find("EffectTestCanvas");
        var shader = Shader.Find("Roll-a-Ball/Effects/Leaf Drift");
        if (templateButton == null || canvas == null || shader == null)
        {
            Debug.LogError("既存のEffectTestCanvas、Stage2WindButton、Leaf Drift Shaderが必要です。");
            return;
        }

        EnsureFolder(Root + "/Material");
        EnsureFolder(Root + "/Mesh");
        EnsureFolder(Root + "/Profile");
        EnsureFolder(Root + "/Prefab");
        EnsureFolder(Root + "/Texture");
        var grass = CreateProfile("Grass", new Color(0.64f, 0.60f, 0.43f, 0.4f),
            new Color(0.75f, 0.72f, 0.56f, 0.3f), new Vector2(8f, 20f), new Vector2(0.16f, 0.28f),
            MovementSurfaceDetail.GrassBlade, new Color(0.43f, 0.66f, 0.19f, 1f),
            new Vector2(4f, 12f), new Vector2(0.14f, 0.24f), new Vector2(0.36f, 0.55f), 0.5f, 0.62f, 0.18f);
        var dirt = CreateProfile("Dirt", new Color(0.69f, 0.56f, 0.39f, 0.46f),
            new Color(0.81f, 0.69f, 0.5f, 0.32f), new Vector2(10f, 24f), new Vector2(0.18f, 0.34f),
            MovementSurfaceDetail.Pebble, new Color(0.48f, 0.38f, 0.27f, 1f),
            new Vector2(3f, 8f), new Vector2(0.06f, 0.11f), new Vector2(0.32f, 0.48f), 0.62f, 1.15f, 0.65f);
        var softDust = CreateDustTexture();
        var material = CreateParticleMaterial("MovementDust", "Universal Render Pipeline/Particles/Unlit", softDust);
        var debrisMaterial = CreateParticleMaterial("MovementDebris", "Universal Render Pipeline/Particles/Lit", null);

        var mesh = CreateDustMesh();
        var grassBladeMesh = CreateGrassBladeMesh();
        var pebbleMesh = LoadPebbleMesh();
        var prefabPath = Root + "/Prefab/FX-S1-DUST-01_ground_dust.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var root = new GameObject("FX-S1-DUST-01_ground_dust");
            var effect = root.AddComponent<GroundDustEffect>();
            var dust = CreateParticles(root.transform, "Dust", material, mesh, 24);
            var sparkles = CreateParticles(root.transform, "Sparkles", material, mesh, 2);
            var details = CreateParticles(root.transform, "SurfaceDetails", material, grassBladeMesh, 16);
            SetReference(effect, "dust", dust);
            SetReference(effect, "sparkles", sparkles);
            SetReference(effect, "surfaceDetails", details);
            SetReference(effect, "grassBladeMesh", grassBladeMesh);
            SetReference(effect, "pebbleMesh", mesh);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        EnsureSurfaceDetailLayer(prefab, material, debrisMaterial, grassBladeMesh, pebbleMesh);
        if (GameObject.Find("MovementDustTestController") != null)
        {
            Debug.Log("Grass/Dirtの表面粒子を更新しました。既存のテストシーン設定は維持します。");
            return;
        }

        var preview = new GameObject("MovementDustTestPreview");
        preview.SetActive(false);
        var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Effect/Environment/Stage/FX-S1-LEAF-01/Material/WindDemoGrass.mat");
        var dirtMaterial = CreateGroundMaterial();
        CreateGround(preview.transform, "Grass", new Vector3(-2.1f, 0.08f, 0f), grassMaterial, grass);
        CreateGround(preview.transform, "Dirt", new Vector3(2.1f, 0.08f, 0f), dirtMaterial, dirt);

        var ball = new GameObject("MovementTestBall", typeof(SphereCollider));
        ball.transform.SetParent(preview.transform, false);
        ball.transform.localPosition = new Vector3(-3.4f, 0.69f, 0f);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "BallVisual";
        visual.transform.SetParent(ball.transform, false);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Effect/Environment/Stage/FX-S1-LIGHT-01/Material/LightTestBall.mat");
        var effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.transform);
        var movement = ball.AddComponent<PlayerMovementEffect>();
        SetReference(movement, "playerCollider", ball.GetComponent<Collider>());
        SetReference(movement, "effect", effectInstance.GetComponent<GroundDustEffect>());

        var button = Object.Instantiate(templateButton, canvas.transform);
        button.name = "MovementDustButton";
        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-24f, -24f);
        buttonRect.sizeDelta = new Vector2(320f, 86f);
        var buttonLabel = button.GetComponentInChildren<TMPro.TMP_Text>(true);
        buttonLabel.text = "移動粉塵（草 / 土）\nON / OFF";
        buttonLabel.raycastTarget = false;

        var status = new GameObject("MovementDustStatus", typeof(RectTransform),
            typeof(UnityEngine.UI.Image));
        status.transform.SetParent(canvas.transform, false);
        var statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(1f, 1f);
        statusRect.anchoredPosition = new Vector2(-24f, -122f);
        statusRect.sizeDelta = new Vector2(620f, 156f);
        var background = status.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.025f, 0.055f, 0.075f, 0.88f);
        background.raycastTarget = false;
        var text = Object.Instantiate(buttonLabel, status.transform);
        text.name = "StatusText";
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(20f, 10f);
        text.rectTransform.offsetMax = new Vector2(-20f, -10f);
        text.fontSize = 24f;
        text.enableAutoSizing = false;
        text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        text.text = "移動粉塵 / 15秒で自動巡回";
        status.SetActive(false);

        var driver = preview.AddComponent<MovementDustTestPreview>();
        SetReference(driver, "testBall", ball.transform);
        SetReference(driver, "ballVisual", visual.transform);
        SetReference(driver, "movementEffect", movement);
        SetReference(driver, "statusText", text);
        var controllerObject = new GameObject("MovementDustTestController");
        var controller = controllerObject.AddComponent<MovementDustTestController>();
        SetReference(controller, "toggleButton", button.GetComponent<UnityEngine.UI.Button>());
        SetReference(controller, "previewRoot", preview);
        SetReference(controller, "statusRoot", status);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("移動粉塵PrefabとEffectTestSceneのGrass/Dirt展示を作成しました。");
    }

    /// <summary>
    /// 既存アセットを保持しながら地面設定の初期値を作成する
    /// </summary>
    private static MovementSurfaceProfile CreateProfile(string name, Color color, Color accent,
        Vector2 rate, Vector2 size, MovementSurfaceDetail detail, Color detailColor,
        Vector2 detailRate, Vector2 detailSize, Vector2 detailLifetime,
        float detailLateralSpeed, float detailLiftSpeed, float detailGravity)
    {
        var path = Root + "/Profile/" + name + "MovementSurface.asset";
        var profile = AssetDatabase.LoadAssetAtPath<MovementSurfaceProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<MovementSurfaceProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        var serialized = new SerializedObject(profile);
        serialized.FindProperty("dustColor").colorValue = color;
        serialized.FindProperty("accentColor").colorValue = accent;
        serialized.FindProperty("particlesPerSecond").vector2Value = rate;
        serialized.FindProperty("particleSize").vector2Value = size;
        serialized.FindProperty("lifetime").vector2Value = new Vector2(0.35f, 0.55f);
        serialized.FindProperty("sparkleRatio").floatValue = 0.02f;
        serialized.FindProperty("surfaceDetail").enumValueIndex = (int)detail;
        serialized.FindProperty("detailColor").colorValue = detailColor;
        serialized.FindProperty("detailParticlesPerSecond").vector2Value = detailRate;
        serialized.FindProperty("detailSize").vector2Value = detailSize;
        serialized.FindProperty("detailLifetime").vector2Value = detailLifetime;
        serialized.FindProperty("detailLateralSpeed").floatValue = detailLateralSpeed;
        serialized.FindProperty("detailLiftSpeed").floatValue = detailLiftSpeed;
        serialized.FindProperty("detailGravity").floatValue = detailGravity;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }

    /// <summary>
    /// 中央の折り目と先細りの輪郭を持つ、長さ1m基準の草片メッシュを作成する
    /// </summary>
    private static Mesh CreateGrassBladeMesh()
    {
        var path = Root + "/Mesh/MovementGrassBlade.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var isNew = mesh == null;
        if (isNew)
        {
            mesh = new Mesh { name = "MovementGrassBlade" };
        }

        var vertices = new Vector3[15];
        var colors = new Color[15];
        var uvs = new Vector2[15];
        var triangles = new int[48];
        for (var row = 0; row < 5; row++)
        {
            var t = row / 4f;
            var width = Mathf.Lerp(0.09f, 0.002f, t * t);
            for (var col = 0; col < 3; col++)
            {
                var index = row * 3 + col;
                var side = col - 1;
                vertices[index] = new Vector3(side * width + 0.08f * t * t, t - 0.5f,
                    0.18f * t * t + (col == 1 ? 0.03f * Mathf.Sin(t * Mathf.PI) : 0f));
                colors[index] = Color.Lerp(new Color(0.66f, 0.72f, 0.49f), Color.white, t);
                uvs[index] = new Vector2(col * 0.5f, t);
            }

            if (row == 4)
            {
                continue;
            }

            for (var col = 0; col < 2; col++)
            {
                var a = row * 3 + col;
                var index = (row * 2 + col) * 6;
                triangles[index] = a;
                triangles[index + 1] = a + 1;
                triangles[index + 2] = a + 4;
                triangles[index + 3] = a;
                triangles[index + 4] = a + 4;
                triangles[index + 5] = a + 3;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (isNew)
        {
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
        }

        return mesh;
    }

    /// <summary>
    /// 既存粉塵Prefabに任意の草片・小石ParticleSystemを追加し地面別の寿命と物理を設定する
    /// </summary>
    private static void EnsureSurfaceDetailLayer(GameObject prefab, Material material, Material debrisMaterial,
        Mesh grassBladeMesh, Mesh pebbleMesh)
    {
        var path = Root + "/Prefab/FX-S1-DUST-01_ground_dust.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var effect = root.GetComponent<GroundDustEffect>();
        var details = root.transform.Find("SurfaceDetails");
        if (details == null)
        {
            var particleSystem = CreateParticles(root.transform, "SurfaceDetails", material, grassBladeMesh, 16);
            SetReference(effect, "surfaceDetails", particleSystem);
            details = particleSystem.transform;
        }

        SetReference(effect, "surfaceDetails", details.GetComponent<ParticleSystem>());
        SetReference(effect, "grassBladeMesh", grassBladeMesh);
        SetReference(effect, "pebbleMesh", pebbleMesh);
        var pebbleObject = root.transform.Find("Pebbles");
        var pebbles = pebbleObject != null ? pebbleObject.GetComponent<ParticleSystem>()
            : CreateParticles(root.transform, "Pebbles", debrisMaterial, pebbleMesh, 6);
        SetReference(effect, "pebbleParticles", pebbles);
        ConfigureDebris(details.GetComponent<ParticleSystem>(), debrisMaterial, grassBladeMesh, 8);
        ConfigureDebris(pebbles, debrisMaterial, pebbleMesh, 6);

        foreach (var name in new[] { "Dust", "Sparkles" })
        {
            var system = root.transform.Find(name).GetComponent<ParticleSystem>();
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.mesh = null;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = material;
            renderer.enableGPUInstancing = false;
            renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>
                { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
        }

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    /// <summary>
    /// 草と小石をそれぞれ独立した3D粒子として設定する
    /// </summary>
    private static void ConfigureDebris(ParticleSystem particles, Material material, Mesh mesh, int limit)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.maxParticles = limit;
        main.startRotation3D = true;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startLifetime = 0.28f;
        main.gravityModifier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-4f, 4f);
        rotation.y = new ParticleSystem.MinMaxCurve(-3f, 3f);
        rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.9f), new Keyframe(0.15f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.5f)));
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enableGPUInstancing = false;
        renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>
            { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
    }

    /// <summary>
    /// FBXの実Mesh幅を1m基準に統一し、Transform倍率に依存しない小石Meshを返す
    /// </summary>
    private static Mesh LoadPebbleMesh()
    {
        var path = Root + "/Mesh/FX-S1-DUST-01_faceted_clod.fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        if (!Mathf.Approximately(mesh.bounds.size.x, 1f))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale /= mesh.bounds.size.x;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        }

        return mesh;
    }

    /// <summary>
    /// URP標準粒子Shaderで半透明の粉塵と非発光の草・石用Materialを構成する
    /// </summary>
    private static Material CreateParticleMaterial(string name, string shaderName, Texture texture)
    {
        var path = Root + "/Material/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = Shader.Find(shaderName);
        material.shaderKeywords = new[] { "_SURFACE_TYPE_TRANSPARENT" };
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_ColorMode", 0f);
        material.SetFloat("_SoftParticlesEnabled", 0f);
        material.SetFloat("_CameraFadingEnabled", 0f);
        material.SetFloat("_ReceiveShadows", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.12f);
        material.SetColor("_EmissionColor", Color.black);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetShaderPassEnabled("DepthOnly", false);
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    /// <summary>
    /// 丸い発光粒に見えないよう、大小の密度むらを持つ粉塵のアルファマスクを作成する
    /// </summary>
    private static Texture2D CreateDustTexture()
    {
        var path = Root + "/Texture/MovementDustMask.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
        {
            return texture;
        }

        const int resolution = 64;
        texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true)
        {
            name = "MovementDustMask",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear
        };
        var pixels = new Color[resolution * resolution];
        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
            {
                var u = x / (resolution - 1f);
                var v = y / (resolution - 1f);
                var px = u * 2f - 1f;
                var py = v * 2f - 1f;
                var coarse = Mathf.PerlinNoise(u * 4.7f + 3.1f, v * 4.2f + 7.3f);
                var fine = Mathf.PerlinNoise(u * 17f + 6.3f, v * 17f + 2.8f);
                var radius = Mathf.Sqrt(px * px + py * py * 1.35f);
                var edge = Mathf.SmoothStep(0f, 1f, (0.88f - radius + (coarse - 0.5f) * 0.55f) * 3.2f);
                var density = edge * Mathf.Lerp(0.3f, 0.9f, coarse) * Mathf.Lerp(0.65f, 1f, fine);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, density);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, true);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    /// <summary>
    /// テクスチャ余白のない6面の小さな粉塵メッシュを作成する
    /// </summary>
    private static Mesh CreateDustMesh()
    {
        var path = Root + "/Mesh/MovementDustFacet.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }

        var vertices = new Vector3[18];
        var colors = new Color[18];
        var triangles = new int[18];
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Mathf.PI / 3f;
            var next = (i + 1) * Mathf.PI / 3f;
            vertices[i * 3] = new Vector3(0.02f, 0.02f, -0.05f);
            vertices[i * 3 + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.38f, 0f);
            vertices[i * 3 + 2] = new Vector3(Mathf.Cos(next) * 0.5f, Mathf.Sin(next) * 0.38f, 0f);
            for (var j = 0; j < 3; j++)
            {
                var index = i * 3 + j;
                var shade = 0.8f + i % 3 * 0.1f;
                colors[index] = new Color(shade, shade, shade, 1f);
                triangles[index] = index;
            }
        }

        var mesh = new Mesh { name = "MovementDustFacet", vertices = vertices, colors = colors, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// 自動放出なし、ワールド空間、固定上限のShurikenレイヤーを作成する
    /// </summary>
    private static ParticleSystem CreateParticles(Transform parent, string name, Material material, Mesh mesh, int limit)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startLifetime = 0.42f;
        main.startSize = 0.15f;
        main.maxParticles = limit;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.18f, 1f), new Keyframe(0.55f, 1.1f), new Keyframe(1f, 0.25f)));
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.08f),
                new GradientAlphaKey(0.65f, 0.45f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    /// <summary>
    /// 草地との境界が分かる非発光のテスト用土マテリアルを用意する
    /// </summary>
    private static Material CreateGroundMaterial()
    {
        var path = Root + "/Material/DustTestDirt.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        var source = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Effect/Environment/Stage/FX-S1-LIGHT-01/Material/ForestBark.mat");
        var material = new Material(source) { name = "DustTestDirt" };
        material.SetColor("_BaseColor", new Color(0.32f, 0.23f, 0.13f, 1f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// 地面種類の参照を持つ展示用Colliderを配置する
    /// </summary>
    private static void CreateGround(Transform parent, string name, Vector3 position,
        Material material, MovementSurfaceProfile profile)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = name + "Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = position;
        ground.transform.localScale = new Vector3(4.2f, 0.2f, 3f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
        SetReference(ground.AddComponent<MovementSurface>(), "profile", profile);
    }

    /// <summary>
    /// Inspectorの参照を明示的に保存する
    /// </summary>
    private static void SetReference(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// エフェクト格納先の不足フォルダだけをUnity APIで作成する
    /// </summary>
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var split = path.LastIndexOf('/');
        var parent = path.Substring(0, split);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }
}
