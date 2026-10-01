using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 蝶の低ポリMesh、共有Material、再利用Prefabと確認シーンを初回生成する
/// </summary>
public static class ButterflySwarmAssetBuilder
{
    public const string Root = "Assets/Effect/Environment/Stage/FX-S1-BUTTERFLY-01";
    public const string PrefabPath = Root + "/Prefab/FX-S1-BUTTERFLY-01_butterfly_swarm.prefab";
    public const string ScenePath = "Assets/Scenes/TestScene/ButterflySwarmTestScene.unity";

    /// <summary>
    /// 既存資産を上書きせず蝶エフェクト一式を生成する
    /// </summary>
    public static void CreateAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.LogWarning("蝶Prefabは作成済みです。既存のInspector設定を調整してください。");
            return;
        }

        var shader = Shader.Find("Roll-a-Ball/Effects/Leaf Drift");
        var dustSource = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Effect/Environment/Common/FX-C-SPIKE-01/FX-C-SPIKE-01_spark.mat");
        if (shader == null || dustSource == null)
        {
            Debug.LogError("蝶の作成にはLeaf Drift ShaderとFX-C-SPIKE-01_spark Materialが必要です。");
            return;
        }

        CreateFolder(Root + "/Mesh");
        CreateFolder(Root + "/Material");
        CreateFolder(Root + "/Prefab");
        var mesh = CreateButterflyMesh();
        AssetDatabase.CreateAsset(mesh, Root + "/Mesh/CrystalButterfly.asset");
        var material = new Material(shader) { name = "CrystalButterfly" };
        material.SetColor("_BaseColor", new Color(0.2f, 0.78f, 1f, 0.92f));
        material.SetColor("_EmissionColor", new Color(0.03f, 0.22f, 0.38f, 1f));
        material.SetFloat("_EmissionStrength", 0.65f);
        material.SetColor("_EdgeColor", new Color(0.45f, 1f, 1f, 1f));
        material.SetFloat("_EdgeStrength", 0.45f);
        material.SetFloat("_WingFlap", 1f);
        AssetDatabase.CreateAsset(material, Root + "/Material/CrystalButterfly.mat");
        var dustMaterial = new Material(dustSource) { name = "ButterflyScaleDust" };
        dustMaterial.SetColor("_BaseColor", new Color(0.5f, 0.92f, 1f, 0.8f));
        AssetDatabase.CreateAsset(dustMaterial, Root + "/Material/ButterflyScaleDust.mat");

        var root = new GameObject("FX-S1-BUTTERFLY-01_butterfly_swarm");
        root.SetActive(false);
        var butterflies = new Transform[4];
        for (var i = 0; i < butterflies.Length; i++)
        {
            var butterfly = new GameObject("Butterfly_" + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
            butterfly.transform.SetParent(root.transform, false);
            butterfly.transform.localScale = Vector3.one * (0.85f + i * 0.05f);
            butterfly.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = butterfly.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            butterflies[i] = butterfly.transform;
        }

        var dust = CreateDust(root.transform, dustMaterial);
        var effect = root.AddComponent<ButterflySwarmEffect>();
        var serialized = new SerializedObject(effect);
        var array = serialized.FindProperty("butterflies");
        array.arraySize = butterflies.Length;
        for (var i = 0; i < butterflies.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = butterflies[i];
        }

        serialized.FindProperty("scaleDust").objectReferenceValue = dust;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        butterflies[3].gameObject.SetActive(false);
        for (var i = 0; i < 3; i++)
        {
            butterflies[i].localPosition = new Vector3((i - 1) * 0.5f, 1.4f + i * 0.12f, 0f);
        }

        root.SetActive(true);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        CreateTestScene(prefab);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// AssetDatabaseで親フォルダーを含めて作成する
    /// </summary>
    private static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var split = path.LastIndexOf('/');
        var parent = path.Substring(0, split);
        CreateFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }

    /// <summary>
    /// 前翅と後翅、細い胴体と触角を1つの両面低ポリMeshへまとめる
    /// </summary>
    private static Mesh CreateButterflyMesh()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        var upper = new[] { new Vector2(0.025f, 0.13f), new Vector2(0.15f, 0.34f),
            new Vector2(0.32f, 0.29f), new Vector2(0.38f, 0.13f),
            new Vector2(0.28f, 0.01f), new Vector2(0.025f, -0.035f) };
        var lower = new[] { new Vector2(0.025f, 0.015f), new Vector2(0.28f, 0.01f),
            new Vector2(0.29f, -0.13f), new Vector2(0.18f, -0.25f),
            new Vector2(0.07f, -0.21f), new Vector2(0.025f, -0.065f) };
        for (var side = -1; side <= 1; side += 2)
        {
            AddWing(upper, new Vector2(0.13f, 0.11f), side, vertices, colors, uv, triangles);
            AddWing(lower, new Vector2(0.12f, -0.09f), side, vertices, colors, uv, triangles);
            AddTriangle(new Vector3(0f, 0.015f, 0.2f), new Vector3(side * 0.07f, 0.015f, 0.32f),
                new Vector3(side * 0.008f, 0.015f, 0.2f), new Color(0.25f, 0.65f, 0.9f),
                0f, vertices, colors, uv, triangles);
        }

        AddTriangle(new Vector3(-0.026f, 0.02f, 0.18f), new Vector3(0.026f, 0.02f, 0.18f),
            new Vector3(0f, 0.02f, -0.23f), new Color(0.12f, 0.22f, 0.42f),
            0f, vertices, colors, uv, triangles);
        var mesh = new Mesh { name = "CrystalButterfly" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        // Include the GPU-folded wings so culling remains correct at every flap phase.
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(0.9f, 0.9f, 0.8f));
        return mesh;
    }

    /// <summary>
    /// 放射状の色分けでクリスタルの翅脈と輪郭を描く
    /// </summary>
    private static void AddWing(Vector2[] outline, Vector2 center, int side, List<Vector3> vertices,
        List<Color> colors, List<Vector2> uv, List<int> triangles)
    {
        for (var i = 0; i < outline.Length; i++)
        {
            var next = (i + 1) % outline.Length;
            var start = vertices.Count;
            vertices.Add(new Vector3(center.x * side, 0f, center.y));
            vertices.Add(new Vector3(outline[i].x * side, 0f, outline[i].y));
            vertices.Add(new Vector3(outline[next].x * side, 0f, outline[next].y));
            colors.Add(i % 2 == 0 ? Color.white : new Color(0.4f, 0.8f, 1f));
            colors.Add(new Color(0.15f, 0.35f, 0.7f));
            colors.Add(new Color(0.28f, 0.65f, 0.9f));
            for (var j = 0; j < 3; j++)
            {
                uv.Add(new Vector2(1f, 0f));
                triangles.Add(start + (side == 1 ? j : 2 - j));
            }
        }
    }

    /// <summary>
    /// 羽ばたき対象外の胴体と触角の三角形を追加する
    /// </summary>
    private static void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, float wingMask,
        List<Vector3> vertices, List<Color> colors, List<Vector2> uv, List<int> triangles)
    {
        var start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        for (var i = 0; i < 3; i++)
        {
            colors.Add(color);
            uv.Add(new Vector2(wingMask, 0f));
            triangles.Add(start + i);
        }
    }

    /// <summary>
    /// ワールド空間でゆっくり落下し明滅して消える上限40粒の鱗粉を設定する
    /// </summary>
    private static ParticleSystem CreateDust(Transform parent, Material material)
    {
        var dustObject = new GameObject("FallingScaleDust");
        dustObject.transform.SetParent(parent, false);
        var dust = dustObject.AddComponent<ParticleSystem>();
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = dust.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.1f);
        main.startSpeed = 0f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.85f, 1f), Color.white);
        main.gravityModifier = 0.015f;
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = dust.emission;
        emission.enabled = false;
        var shape = dust.shape;
        shape.enabled = false;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.2f, 0.7f, 1f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.8f, 0.65f), new GradientAlphaKey(0f, 1f) });
        var color = dust.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
        var size = dust.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f),
            new Keyframe(0.15f, 1f), new Keyframe(0.4f, 0.3f), new Keyframe(0.65f, 0.9f),
            new Keyframe(1f, 0f)));
        var noise = dust.noise;
        noise.enabled = true;
        noise.strength = 0.045f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.2f;
        var renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return dust;
    }

    /// <summary>
    /// 実ゲームに近いクォータービューで2匹、3匹、4匹を比較できるシーンを保存する
    /// </summary>
    private static void CreateTestScene(GameObject prefab)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            return;
        }

        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(5f, 7.5f, -9f);
        cameraObject.transform.LookAt(new Vector3(0f, 1f, 0f));
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.055f, 0.08f);
        var lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        lightObject.GetComponent<Light>().type = LightType.Directional;
        lightObject.GetComponent<Light>().intensity = 1.2f;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ForestGround";
        floor.transform.localScale = Vector3.one * 1.3f;
        floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Effect/Environment/Stage/FX-S1-LIGHT-01/Material/ForestGround.mat");
        for (var i = 0; i < 3; i++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "ButterflySwarm_" + (i + 2);
            instance.transform.position = new Vector3((i - 1) * 3.2f, 0f, 0f);
            var serialized = new SerializedObject(instance.GetComponent<ButterflySwarmEffect>());
            serialized.FindProperty("butterflyCount").intValue = i + 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scene, true);
    }
}
