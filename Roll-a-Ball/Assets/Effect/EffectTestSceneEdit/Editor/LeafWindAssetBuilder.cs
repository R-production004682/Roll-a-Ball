using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// そよ風の葉・光粒のPrefabとEffectTestScene展示を生成する
/// </summary>
public static class LeafWindAssetBuilder
{
    private const string Root = "Assets/Effect/Stage1/FX-S1-LEAF-01";
    private const string TestRoot = "Assets/Effect/Test";
    private const string PrefabPath = Root + "/FX-S1-LEAF-01_leaf_particles.prefab";
    private const string TestPrefabPath = TestRoot + "/LeafWindTestPreview.prefab";

    /// <summary>
    /// そよ風Prefabと必要な低ポリアセットを初回だけ生成する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Create Stage 1 Leaf Wind Assets")]
    public static void CreateAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.LogWarning("そよ風の葉Prefabは作成済みです。既存のInspector設定を調整してください。");
            return;
        }

        var leafShader = Shader.Find("Roll-a-Ball/Effects/Leaf Drift");
        var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (leafShader == null || particleShader == null)
        {
            Debug.LogError("そよ風用ShaderまたはURP Particles/Unlitのインポートを確認してください。");
            return;
        }

        var leafMesh = CreateLeafMesh();
        var leafMaterial = CreateLeafMaterial(leafShader);
        var glowMaterial = CreateGlowMaterial(particleShader);
        var root = new GameObject("FX-S1-LEAF-01_leaf_particles");
        root.SetActive(false);
        var windZone = CreateWindZone(root.transform);
        var leafSystem = CreateLeafParticles(root.transform, leafMesh, leafMaterial);
        var lightSystem = CreateLightParticles(root.transform, glowMaterial);
        var effect = root.AddComponent<LeafWindEffect>();
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("leafParticles").objectReferenceValue = leafSystem;
        serialized.FindProperty("lightParticles").objectReferenceValue = lightSystem;
        serialized.FindProperty("windZone").objectReferenceValue = windZone;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("そよ風の葉Prefabを作成しました: " + PrefabPath);
    }

    /// <summary>
    /// 既存のそよ風PrefabへWind Zoneを追加して風の設定を更新する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Update Stage 1 Leaf Wind Zone")]
    public static void UpdateWindZone()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogWarning("そよ風Prefabが見つかりません。先にPrefabを作成してください。");
            return;
        }

        var windZone = root.GetComponentInChildren<WindZone>(true);
        if (windZone == null)
        {
            windZone = CreateWindZone(root.transform);
        }

        var effect = root.GetComponent<LeafWindEffect>();
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("windZone").objectReferenceValue = windZone;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("そよ風PrefabへWind Zoneを追加しました: " + PrefabPath);
    }

    /// <summary>
    /// そよ風Prefabに方向性のある風を設定する
    /// </summary>
    private static WindZone CreateWindZone(Transform parent)
    {
        var objectRoot = new GameObject("WindZone");
        objectRoot.transform.SetParent(parent, false);
        var windZone = objectRoot.AddComponent<WindZone>();
        windZone.mode = WindZoneMode.Directional;
        windZone.windMain = 0.34f;
        windZone.windTurbulence = 0.16f;
        windZone.windPulseMagnitude = 0.1f;
        windZone.windPulseFrequency = 0.25f;
        return windZone;
    }

    /// <summary>
    /// 葉の輪郭と折れを表現する低ポリメッシュを作成する
    /// </summary>
    private static Mesh CreateLeafMesh()
    {
        const string path = Root + "/LeafDriftMesh.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }

        var mesh = new Mesh { name = "LeafDriftMesh" };
        mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0.37f, 0.13f, 0.045f),
            new Vector3(0.82f, 0.015f, 0f),
            new Vector3(0.38f, -0.17f, 0.035f),
            new Vector3(0.34f, 0f, 0.12f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0.5f),
            new Vector2(0.45f, 1f),
            new Vector2(1f, 0.52f),
            new Vector2(0.45f, 0f),
            new Vector2(0.42f, 0.5f)
        };
        mesh.colors = new[]
        {
            Color.white,
            new Color(0.92f, 1f, 0.88f, 1f),
            new Color(0.78f, 1f, 0.88f, 1f),
            new Color(0.85f, 1f, 0.92f, 1f),
            Color.white
        };
        mesh.triangles = new[]
        {
            0, 1, 4,
            1, 2, 4,
            2, 3, 4,
            3, 0, 4
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// 葉に緑と淡いシアンの縁を加えるマテリアルを作成する
    /// </summary>
    private static Material CreateLeafMaterial(Shader shader)
    {
        const string path = Root + "/LeafDrift.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        var material = new Material(shader) { name = "LeafDrift" };
        material.SetColor("_BaseColor", new Color(0.48f, 0.82f, 0.3f, 0.9f));
        material.SetColor("_EmissionColor", new Color(0.14f, 0.38f, 0.1f, 1f));
        material.SetFloat("_EmissionStrength", 0.5f);
        material.SetColor("_EdgeColor", new Color(0.3f, 0.92f, 0.7f, 1f));
        material.SetFloat("_EdgeStrength", 0.24f);
        material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// 木漏れ日に馴染む淡い光粒マテリアルを作成する
    /// </summary>
    private static Material CreateGlowMaterial(Shader shader)
    {
        const string path = Root + "/LeafGlow.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/LeafSoftDust.asset");
        if (texture == null)
        {
            texture = CreateSoftDustTexture();
        }

        var material = new Material(shader) { name = "LeafGlow" };
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(0.7f, 0.94f, 0.76f, 0.38f));
        material.SetColor("_EmissionColor", new Color(0.35f, 0.95f, 0.7f, 1f));
        material.SetFloat("_EmissionIntensity", 0.65f);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// 光粒用の柔らかい円形テクスチャを作成する
    /// </summary>
    private static Texture2D CreateSoftDustTexture()
    {
        const string path = Root + "/LeafSoftDust.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
        {
            name = "LeafSoftDust",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear
        };
        var colors = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                colors[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - radius), 2.2f));
            }
        }

        texture.SetPixels(colors);
        texture.Apply(true, false);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    /// <summary>
    /// 風に乗って移動する低密度の葉粒子を作成する
    /// </summary>
    private static ParticleSystem CreateLeafParticles(Transform parent, Mesh mesh, Material material)
    {
        var objectRoot = new GameObject("Leaves");
        objectRoot.transform.SetParent(parent, false);
        objectRoot.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        var particles = objectRoot.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.duration = 12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.035f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.46f, 0.86f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.45f, 0.72f, 0.27f, 0.78f),
            new Color(0.73f, 0.91f, 0.42f, 0.92f));
        main.maxParticles = 52;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.cullingMode = ParticleSystemCullingMode.Automatic;

        var emission = particles.emission;
        emission.rateOverTime = 3.9f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 5f, 12f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0.34f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.027f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.085f);
        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.16f;
        noise.frequency = 0.24f;
        noise.scrollSpeed = 0.16f;
        noise.damping = true;
        var rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.82f, 0.16f),
                new GradientAlphaKey(0.82f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = gradient;

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    /// <summary>
    /// 葉の動きを補助する小さな光粒を作成する
    /// </summary>
    private static ParticleSystem CreateLightParticles(Transform parent, Material material)
    {
        var objectRoot = new GameObject("LightMotes");
        objectRoot.transform.SetParent(parent, false);
        objectRoot.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        var particles = objectRoot.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.56f, 0.88f, 0.63f, 0.28f),
            new Color(0.75f, 1f, 0.87f, 0.62f));
        main.maxParticles = 72;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particles.emission;
        emission.rateOverTime = 5.5f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 5f, 12f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0.27f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.022f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.068f);
        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.28f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.55f, 0.2f),
                new GradientAlphaKey(0.55f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    /// <summary>
    /// EffectTestSceneに木漏れ日と並べてそよ風の確認展示を追加する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Add Stage 1 Leaf Wind Test")]
    public static void AddTestPreview()
    {
        var scene = SceneManager.GetActiveScene();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var existing = Object.FindFirstObjectByType<EffectTestSceneController>();
        if (scene.path != "Assets/Scenes/TestScene/EffectTestScene.unity" || prefab == null || existing == null
            || Object.FindFirstObjectByType<LeafWindTestController>() != null)
        {
            Debug.LogWarning("EffectTestSceneを開き、そよ風Prefab作成済み・未追加の状態で実行してください。");
            return;
        }

        var source = new SerializedObject(existing).FindProperty("goalClearEffectButton").objectReferenceValue as UnityEngine.UI.Button;
        if (source == null)
        {
            Debug.LogError("EffectTestSceneの既存ボタンを取得できませんでした。", existing);
            return;
        }

        var preview = new GameObject("LeafWindTestPreview");
        preview.SetActive(false);
        var dappledPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Effect/Test/DappledLightTestPreview.prefab");
        if (dappledPrefab != null)
        {
            var environment = (GameObject)PrefabUtility.InstantiatePrefab(dappledPrefab, preview.transform);
            environment.name = "DappledLightEnvironment";
            environment.SetActive(true);
        }

        preview.AddComponent<WindSwayTestPreview>();
        AddSimpleWindDemoObjects(preview.transform);

        var effect = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.transform);
        effect.name = "FX-S1-LEAF-01_leaf_particles";
        effect.SetActive(true);
        PrefabUtility.SaveAsPrefabAssetAndConnect(preview, TestPrefabPath, InteractionMode.AutomatedAction);

        var button = Object.Instantiate(source, source.transform.parent);
        button.name = "LeafWindButton";
        ((RectTransform)button.transform).anchoredPosition += new Vector2(0f, -216f);
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        var label = button.GetComponentInChildren<TMPro.TMP_Text>();
        if (label != null)
        {
            label.text = "そよ風の葉\nON / OFF";
        }

        var panel = source.transform.parent as RectTransform;
        panel.sizeDelta += new Vector2(0f, 108f);
        var controller = existing.gameObject.AddComponent<LeafWindTestController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("toggleButton").objectReferenceValue = button;
        serialized.FindProperty("previewRoot").objectReferenceValue = preview;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("EffectTestSceneへそよ風の葉テストを追加しました。", existing);
    }

    /// <summary>
    /// 保存済みのそよ風テストPrefabへ樹冠の揺れ確認用コンポーネントを追加する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Update Stage 1 Leaf Wind Test Preview")]
    public static void UpdateTestPreviewPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(TestPrefabPath);
        if (root == null)
        {
            Debug.LogWarning("LeafWindTestPreview.prefabが見つかりません。先にテスト展示を追加してください。");
            return;
        }

        if (root.GetComponent<WindSwayTestPreview>() == null)
        {
            root.AddComponent<WindSwayTestPreview>();
        }

        AddSimpleWindDemoObjects(root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, TestPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("そよ風テストPrefabへ樹冠の揺れ確認を追加しました: " + TestPrefabPath);
    }

    /// <summary>
    /// 既存のそよ風テストへ保存済みPreviewを再接続する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Repair Stage 1 Leaf Wind Test")]
    public static void RepairTestPreview()
    {
        var scene = SceneManager.GetActiveScene();
        var controller = Object.FindFirstObjectByType<LeafWindTestController>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabPath);
        if (scene.path != "Assets/Scenes/TestScene/EffectTestScene.unity" || controller == null || prefab == null)
        {
            Debug.LogWarning("EffectTestScene、LeafWindTestController、LeafWindTestPreview.prefabを確認してください。");
            return;
        }

        var serialized = new SerializedObject(controller);
        var current = serialized.FindProperty("previewRoot").objectReferenceValue as GameObject;
        if (current != null)
        {
            Debug.Log("そよ風テストのPreviewはすでに接続されています。", controller);
            return;
        }

        var preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        preview.name = "LeafWindTestPreview";
        preview.SetActive(false);
        serialized.FindProperty("previewRoot").objectReferenceValue = preview;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("そよ風テストのPreviewを再接続しました。", controller);
    }

    /// <summary>
    /// Wind Zoneの揺れを見やすくする簡易の木と草をテストPrefabへ追加する
    /// </summary>
    private static void AddSimpleWindDemoObjects(Transform parent)
    {
        if (parent.Find("WindDemoTree") != null)
        {
            return;
        }

        var bark = CreatePreviewMaterial("WindDemoBark", new Color(0.18f, 0.1f, 0.055f));
        var leaf = CreatePreviewMaterial("WindDemoLeaf", new Color(0.16f, 0.42f, 0.1f));
        var grass = CreatePreviewMaterial("WindDemoGrass", new Color(0.22f, 0.52f, 0.14f));
        var tree = new GameObject("WindDemoTree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = new Vector3(3.1f, 0f, -1.1f);
        CreatePreviewPrimitive(tree.transform, "WindDemoTrunk", PrimitiveType.Cylinder,
            new Vector3(0f, 1.55f, 0f), new Vector3(0.28f, 1.55f, 0.28f), bark);
        for (var i = 0; i < 3; i++)
        {
            var canopy = CreatePreviewPrimitive(tree.transform, "Canopy_WindDemo_" + i, PrimitiveType.Sphere,
                new Vector3((i - 1) * 0.55f, 3.15f + (i % 2) * 0.22f, 0f),
                new Vector3(1.35f, 0.72f, 1.1f), leaf);
            canopy.transform.localRotation = Quaternion.Euler(0f, i * 32f, (i - 1) * 8f);
        }

        var grassRoot = new GameObject("WindDemoGrass");
        grassRoot.transform.SetParent(parent, false);
        grassRoot.transform.localPosition = new Vector3(-2.3f, 0f, -1.1f);
        for (var i = 0; i < 9; i++)
        {
            var x = (i % 3) * 0.42f - 0.42f;
            var z = (i / 3) * 0.36f - 0.36f;
            var blade = CreatePreviewPrimitive(grassRoot.transform, "Canopy_GrassBlade_" + i,
                PrimitiveType.Cube, new Vector3(x, 0.42f, z), new Vector3(0.12f, 0.42f, 0.08f), grass);
            blade.transform.localRotation = Quaternion.Euler(0f, i * 17f, (i % 2 == 0 ? -1f : 1f) * 7f);
        }
    }

    /// <summary>
    /// テスト表示用のURP/Litマテリアルを再利用または作成する
    /// </summary>
    private static Material CreatePreviewMaterial(string name, Color color)
    {
        var path = Root + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// Colliderを持たないテスト用プリミティブを配置する
    /// </summary>
    private static GameObject CreatePreviewPrimitive(Transform parent, string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
