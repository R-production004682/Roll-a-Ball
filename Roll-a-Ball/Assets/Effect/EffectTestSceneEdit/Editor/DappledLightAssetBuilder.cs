using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 木漏れ日の初期アセットと専用の比較展示を生成する
/// </summary>
public static class DappledLightAssetBuilder
{
    private const string Root = "Assets/Effect/Stage1/FX-S1-LIGHT-01";
    private const string PrefabPath = Root + "/FX-S1-LIGHT-01_dappled_light.prefab";

    /// <summary>
    /// 既存Prefabを上書きせず初回だけ木漏れ日の構成を作成する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Create Dappled Light Assets")]
    public static void CreateAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.LogWarning("木漏れ日Prefabは作成済みです。既存のInspector設定を調整してください。");
            return;
        }

        var shader = Shader.Find("Roll-a-Ball/Effects/Dappled Light Volume");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || lit == null)
        {
            Debug.LogError("木漏れ日ShaderまたはURP/Litのインポートを確認してください。");
            return;
        }

        var cookie = CreateCanopyCookie();
        var volumeMaterial = new Material(shader) { name = "DappledLightVolume" };
        AssetDatabase.CreateAsset(volumeMaterial, Root + "/DappledLightVolume.mat");
        var root = new GameObject("FX-S1-LIGHT-01_dappled_light");
        root.SetActive(false);
        var lightObject = new GameObject("CanopySun");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(-12f, 22f, 0f);
        lightObject.transform.localRotation = Quaternion.LookRotation(new Vector3(12f, -22f, 0f));
        var sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Spot;
        sun.range = 40f;
        sun.spotAngle = 30f;
        sun.innerSpotAngle = 25f;
        sun.color = new Color(1f, 0.94f, 0.82f);
        sun.intensity = 1400f;
        sun.cookie = cookie;
        sun.shadows = LightShadows.Soft;
        sun.shadowBias = 0.03f;
        sun.shadowNormalBias = 0.08f;
        sun.shadowNearPlane = 0.2f;
        lightObject.AddComponent<UniversalAdditionalLightData>();
        var volumeObject = Primitive(root.transform, "ScatteringVolume", PrimitiveType.Cube,
            new Vector3(0f, 4.5f, 0f), new Vector3(12f, 9f, 12f), volumeMaterial);
        var renderer = volumeObject.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var effect = root.AddComponent<DappledLightEffect>();
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("sunLight").objectReferenceValue = sun;
        serialized.FindProperty("volumeRenderer").objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        CreateDust(root.transform);
        root.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 樹冠の不規則な隙間と細かい葉を持つ再現可能なCookieを作成する
    /// </summary>
    public static Texture2D CreateCanopyCookie()
    {
        const int size = 512;
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/CanopyCookie.asset");
        var texture = existing != null ? existing : new Texture2D(size, size, TextureFormat.RGBA32, true, true)
        {
            name = "CanopyCookie",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4
        };
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / size;
                var v = (y + 0.5f) / size;
                var broad = Mathf.PerlinNoise(u * 7.3f + 13.7f, v * 7.3f + 42.2f);
                var leaves = Mathf.PerlinNoise(u * 43f + 5f, v * 36f + 17f);
                var detail = Mathf.PerlinNoise(u * 110f, v * 95f + 9f);
                var canopy = broad * 0.64f + leaves * 0.28f + detail * 0.08f;
                var gaps = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.54f, 0.61f, canopy));
                var edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(u, v, 1f - u, 1f - v) / 0.14f));
                var value = gaps * edge;
                pixels[y * size + x] = new Color(value, value, value, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(texture, Root + "/CanopyCookie.asset");
        }

        EditorUtility.SetDirty(texture);
        return texture;
    }

    /// <summary>
    /// 光を受ける微細な浮遊物を低密度で配置する
    /// </summary>
    private static void CreateDust(Transform parent)
    {
        var dustShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        var material = new Material(dustShader) { name = "SunlitDust" };
        material.SetColor("_BaseColor", new Color(0.85f, 0.8f, 0.65f, 0.65f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, true, true) { name = "SoftDust" };
        var colors = new Color[1024];
        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                colors[y * 32 + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - radius), 2f));
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, Root + "/SoftDust.asset");
        material.SetTexture("_BaseMap", texture);
        AssetDatabase.CreateAsset(material, Root + "/SunlitDust.mat");
        var go = new GameObject("SunlitMotes");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        var particles = go.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = particles.emission;
        emission.rateOverTime = 6f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7f, 4f, 7f);
        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.08f;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.2f), new GradientAlphaKey(0.6f, 0.8f), new GradientAlphaKey(0f, 1f) });
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    /// <summary>
    /// テストシーンだけに照明確認用の展示と既存形式のボタンを追加する
    /// </summary>
    [MenuItem("Tools/Roll-a-Ball/Effects/Add Dappled Light Test")]
    public static void AddTestPreview()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (scene.path != "Assets/Scenes/TestScene/EffectTestScene.unity" || prefab == null
            || Object.FindFirstObjectByType<DappledLightTestController>() != null)
        {
            Debug.LogWarning("EffectTestSceneを開き、Prefab作成済み・木漏れ日テスト未追加の状態で実行してください。");
            return;
        }

        var existing = Object.FindFirstObjectByType<EffectTestSceneController>();
        var source = new SerializedObject(existing).FindProperty("goalClearEffectButton").objectReferenceValue as UnityEngine.UI.Button;
        var preview = new GameObject("DappledLightTestPreview");
        preview.SetActive(false);
        var effect = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.transform);
        effect.SetActive(true);
        var ground = CreateLitMaterial("ForestGround", Color.white);
        UpdateTestSurface();
        var bark = CreateLitMaterial("ForestBark", new Color(0.12f, 0.085f, 0.05f));
        var leaf = CreateLitMaterial("ForestCanopy", new Color(0.11f, 0.19f, 0.055f));
        var stone = CreateLitMaterial("ForestStone", new Color(0.24f, 0.27f, 0.23f));
        var ballMaterial = CreateLitMaterial("LightTestBall", new Color(0.7f, 0.74f, 0.73f));
        Primitive(preview.transform, "ForestFloor", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(11f, 0.3f, 11f), ground);
        Primitive(preview.transform, "LightTestBall", PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0f), Vector3.one, ballMaterial);
        var random = new System.Random(47);
        for (var i = 0; i < 11; i++)
        {
            var x = -5f + i;
            var z = 3.6f + (float)random.NextDouble() * 1.4f;
            var height = 3.3f + (float)random.NextDouble() * 2.4f;
            Primitive(preview.transform, "Trunk_" + i, PrimitiveType.Cylinder, new Vector3(x, height * 0.5f, z), new Vector3(0.25f, height * 0.5f, 0.3f), bark);
            for (var j = 0; j < 3; j++)
            {
                var canopy = Primitive(preview.transform, "Canopy_" + i + "_" + j, PrimitiveType.Sphere,
                    new Vector3(x + j * 0.5f - 0.5f, height + 0.2f * j, z), new Vector3(2.1f, 0.8f, 1.7f), leaf);
                canopy.transform.localRotation = Quaternion.Euler(10f * j, 39f * i, 15f * j);
            }
        }

        for (var i = 0; i < 25; i++)
        {
            var x = (float)random.NextDouble() * 9f - 4.5f;
            var z = (float)random.NextDouble() * 8f - 4f;
            var size = 0.15f + (float)random.NextDouble() * 0.5f;
            var rock = Primitive(preview.transform, "Stone_" + i, PrimitiveType.Sphere, new Vector3(x, size * 0.2f, z), new Vector3(size, size * 0.6f, size * 0.8f), stone);
            rock.transform.localRotation = Quaternion.Euler(i * 37f, i * 19f, i * 11f);
        }

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.value = 1.1f;
        bloom.intensity.value = 0.18f;
        bloom.scatter.value = 0.65f;
        var tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.value = TonemappingMode.ACES;
        AssetDatabase.CreateAsset(profile, Root + "/DappledLightTestProfile.asset");
        foreach (var component in profile.components)
        {
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        EditorUtility.SetDirty(profile);
        var volume = preview.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.sharedProfile = profile;
        PrefabUtility.SaveAsPrefabAssetAndConnect(preview, "Assets/Effect/Test/DappledLightTestPreview.prefab", InteractionMode.AutomatedAction);
        var button = Object.Instantiate(source, source.transform.parent);
        button.name = "DappledLightButton";
        ((RectTransform)button.transform).anchoredPosition += new Vector2(0f, -108f);
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        var label = button.GetComponentInChildren<TMPro.TMP_Text>();
        label.text = "木漏れ日\nON / OFF";
        var panel = source.transform.parent as RectTransform;
        panel.sizeDelta += new Vector2(0f, 108f);
        var controller = existing.gameObject.AddComponent<DappledLightTestController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("toggleButton").objectReferenceValue = button;
        serialized.FindProperty("previewRoot").objectReferenceValue = preview;
        serialized.FindProperty("testCamera").objectReferenceValue = Camera.main;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                serialized.FindProperty("sceneLight").objectReferenceValue = light;
                break;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// テスト専用の拡散反射マテリアルを作成する
    /// </summary>
    private static Material CreateLitMaterial(string name, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.23f);
        AssetDatabase.CreateAsset(material, Root + "/" + name + ".mat");
        return material;
    }

    /// <summary>
    /// 地面の凹凸への光の反応を見るためテスト専用の苔と土のマップを生成する
    /// </summary>
    public static void UpdateTestSurface()
    {
        const int size = 256;
        var albedoPath = Root + "/ForestGroundColor.asset";
        var normalPath = Root + "/ForestGroundNormal.asset";
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var create = albedo == null;
        albedo ??= new Texture2D(size, size, TextureFormat.RGBA32, true, false) { name = "ForestGroundColor" };
        normal ??= new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = "ForestGroundNormal" };
        var colors = new Color[size * size];
        var normals = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var u = (float)x / size;
                var v = (float)y / size;
                var fine = Mathf.PerlinNoise(u * 130f + 19f, v * 130f + 42f);
                var patches = Mathf.PerlinNoise(u * 12f, v * 12f + 14f);
                var path = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.2f, Mathf.Abs(u - 0.5f - Mathf.Sin(v * 6f) * 0.12f)));
                var dirt = new Color(0.32f, 0.27f, 0.19f) * (0.7f + fine * 0.6f);
                var moss = Color.Lerp(new Color(0.17f, 0.22f, 0.10f), new Color(0.38f, 0.41f, 0.23f), patches) * (0.6f + fine * 0.7f);
                colors[y * size + x] = Color.Lerp(dirt, moss, path);
                var dx = Mathf.PerlinNoise((u + 1f / size) * 130f + 19f, v * 130f + 42f) - fine;
                var dy = Mathf.PerlinNoise(u * 130f + 19f, (v + 1f / size) * 130f + 42f) - fine;
                var n = new Vector3(-dx * 1.5f, -dy * 1.5f, 1f).normalized;
                normals[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        }

        albedo.SetPixels(colors);
        normal.SetPixels(normals);
        albedo.Apply();
        normal.Apply();
        if (create)
        {
            AssetDatabase.CreateAsset(albedo, albedoPath);
            AssetDatabase.CreateAsset(normal, normalPath);
        }

        EditorUtility.SetDirty(albedo);
        EditorUtility.SetDirty(normal);
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/ForestGround.mat");
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", albedo);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 0.6f);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
    }

    /// <summary>
    /// 描画確認に不要なColliderを外したプリミティブを配置する
    /// </summary>
    private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
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
