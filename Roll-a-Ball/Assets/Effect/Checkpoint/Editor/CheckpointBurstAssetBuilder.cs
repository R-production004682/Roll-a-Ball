using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Blenderの分割モデルから比較用Prefabと既存テストシーンの配置を構築する
/// </summary>
public static class CheckpointBurstAssetBuilder
{
    private const string Root = "Assets/Effect/Checkpoint";

    public static void FinalizeA()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var preview = roots.FirstOrDefault(item => item.name == "CheckpointBurstTestPreview");
        var controllerObject = roots.FirstOrDefault(item => item.name == "CheckpointBurstTestController");
        var controller = controllerObject != null ? controllerObject.GetComponent<CheckpointBurstTestController>() : null;
        if (scene.path != "Assets/Scenes/TestScene/EffectTestScene.unity" || preview == null || controller == null)
        {
            Debug.LogError("EffectTestSceneのA/B配置を確認できません。");
            return;
        }

        var effects = preview.GetComponentsInChildren<CheckpointBurstEffect>(true);
        var adoptedEffect = effects.First(item => item.name.Contains("snap"));
        foreach (var effect in effects)
        {
            if (effect.name.Contains("crystal"))
            {
                Object.DestroyImmediate(effect.gameObject);
            }
            else
            {
                effect.transform.position = Vector3.zero;
                effect.transform.localScale = Vector3.one * 1.5f;
            }
        }
        var label = preview.GetComponentsInChildren<Transform>(true).First(item => item.name == "A / SNAP");
        label.position = Vector3.up * 3.3f;
        var obsoleteLabel = preview.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "B / CRYSTAL");
        if (obsoleteLabel != null)
        {
            Object.DestroyImmediate(obsoleteLabel.gameObject);
        }

        var buttons = GameObject.Find("EffectTestCanvas").GetComponentsInChildren<Button>(true);
        var show = buttons.First(button => button.name == "Checkpoint0Button");
        var play = buttons.First(button => button.name == "Checkpoint1Button");
        var state = buttons.First(button => button.name == "Checkpoint2Button");
        var reset = buttons.First(button => button.name == "Checkpoint3Button");
        show.GetComponentInChildren<TMP_Text>().text = "チェックポイントA\nON / OFF";
        play.GetComponentInChildren<TMP_Text>().text = "A案を破裂";
        state.GetComponentInChildren<TMP_Text>().text = "現在 / 通過済み 切替";
        reset.GetComponentInChildren<TMP_Text>().text = "リトライ：袋に戻す";

        var serialized = new SerializedObject(controller);
        Reference(serialized, "previewRoot", preview);
        Reference(serialized, "effect", adoptedEffect);
        Reference(serialized, "showButton", show);
        Reference(serialized, "playButton", play);
        Reference(serialized, "stateButton", state);
        Reference(serialized, "resetButton", reset);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        controller.enabled = true;
        AssetDatabase.DeleteAsset(Root + "/Prefab/FX-C-CP-02_crystal_burst.prefab");
        preview.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 初回のみA案と操作ボタンを作成して保存する
    /// </summary>
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TestScene/EffectTestScene.unity" ||
            GameObject.Find("CheckpointBurstTestController") != null)
        {
            Debug.LogError("EffectTestSceneを開き、既存のCheckpointテスト配置がないことを確認してください。");
            return;
        }
        foreach (var folder in new[] { "Mesh", "Material", "Prefab" })
        {
            if (!AssetDatabase.IsValidFolder(Root + "/" + folder))
            {
                AssetDatabase.CreateFolder(Root, folder);
            }
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Model/FX-C-CP-01_pouch_parts.fbx");
        var baked = model.GetComponentsInChildren<MeshFilter>().ToDictionary(m => m.name, Bake);
        var pouch = Material("FX-C-CP-01_pouch", "Universal Render Pipeline/Lit", new Color(.86f, .94f, 1f, .3f));
        ConfigurePouchMaterial(pouch);
        var seal = Material("FX-C-CP-01_seal", "Universal Render Pipeline/Lit", new Color(.86f, .94f, 1f, .6f));
        ConfigurePouchMaterial(seal);
        var arrow = Material("FX-C-CP-01_arrow", "Universal Render Pipeline/Unlit", Color.white);
        var shardMaterial = Material("FX-C-CP-01_shard", "Universal Render Pipeline/Particles/Unlit", Color.white);
        var flashMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Effect/Environment/Common/FX-C-SPIKE-01/FX-C-SPIKE-01_flash.mat"));
        AssetDatabase.CreateAsset(flashMaterial, Root + "/Material/FX-C-CP-01_flash.mat");
        var triangle = new Mesh { name = "CheckpointTriangle" };
        triangle.vertices = new[] { new Vector3(-.5f, -.35f, 0), new Vector3(.5f, -.35f, 0), new Vector3(0, .65f, 0) };
        triangle.triangles = new[] { 0, 1, 2, 2, 1, 0 };
        triangle.RecalculateNormals();
        triangle.RecalculateBounds();
        AssetDatabase.CreateAsset(triangle, Root + "/Mesh/FX-C-CP-01_triangle.asset");
        var preview = new GameObject("CheckpointBurstTestPreview");
        CheckpointBurstEffect effect;
        var camera = Camera.main;
        var id = "FX-C-CP-01_pouch_snap";
        var root = new GameObject(id);
        var left = Child("LeftHalf", root.transform);
        var right = Child("RightHalf", root.transform);
        Transform arrowTransform = null;
        foreach (var entry in baked)
        {
            var parent = entry.Key.Contains("Left") ? left : entry.Key.Contains("Right") ? right : root.transform;
            var part = Child(entry.Key, parent);
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = entry.Value;
            var renderer = part.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = entry.Key == "Arrow" ? arrow : entry.Key.Contains("Seal") ? seal : pouch;
            if (entry.Key == "Arrow")
            {
                arrowTransform = part;
            }
        }
        var burst = Particles("TriangleBurst", root.transform, shardMaterial, triangle, false);
        var flash = Particles("Flash", root.transform, flashMaterial, null, false);
        var component = root.AddComponent<CheckpointBurstEffect>();
        var serialized = new SerializedObject(component);
        Reference(serialized, "leftHalf", left);
        Reference(serialized, "rightHalf", right);
        Reference(serialized, "arrow", arrowTransform);
        Reference(serialized, "arrowRenderer", arrowTransform.GetComponent<Renderer>());
        Reference(serialized, "fragments", burst);
        Reference(serialized, "flash", flash);
        serialized.FindProperty("duration").floatValue = .6f;
        serialized.FindProperty("splitDistance").floatValue = 1.1f;
        serialized.FindProperty("fragmentCount").intValue = 18;
        serialized.FindProperty("currentColor").colorValue = new Color(1f, .42f, .01f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefab/" + id + ".prefab");
        Object.DestroyImmediate(root);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.transform);
        instance.transform.position = Vector3.up * .15f;
        instance.transform.rotation = Quaternion.Euler(0, camera.transform.eulerAngles.y, 0);
        instance.transform.localScale = Vector3.one * 1.5f;
        effect = instance.GetComponent<CheckpointBurstEffect>();
        var label = Child("A / SNAP", preview.transform);
        label.position = instance.transform.position + Vector3.up * 3.3f;
        label.rotation = camera.transform.rotation;
        var text = label.gameObject.AddComponent<TextMeshPro>();
        text.text = "A  /  SNAP";
        text.fontSize = 3f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.14f, .3f, .36f);
        text.rectTransform.sizeDelta = new Vector2(3f, .6f);
        var canvas = GameObject.Find("EffectTestCanvas").transform;
        var template = canvas.GetComponentsInChildren<Button>().First(b => b.name == "MovementDustButton");
        var buttons = new Button[4];
        var labels = new[] { "チェックポイントA\nON / OFF", "A案を破裂", "現在 / 通過済み 切替", "リトライ：袋に戻す" };
        for (var i = 0; i < 4; i++)
        {
            var button = Object.Instantiate(template, canvas);
            button.name = "Checkpoint" + i + "Button";
            button.onClick = new Button.ButtonClickedEvent();
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(-24, -126 - i * 96);
            foreach (var buttonText in button.GetComponentsInChildren<TMP_Text>())
            {
                buttonText.text = labels[i];
            }
            buttons[i] = button;
        }
        var controller = new GameObject("CheckpointBurstTestController").AddComponent<CheckpointBurstTestController>();
        var setup = new SerializedObject(controller);
        Reference(setup, "previewRoot", preview);
        Reference(setup, "effect", effect);
        var fields = new[] { "showButton", "playButton", "stateButton", "resetButton" };
        for (var i = 0; i < 4; i++)
        {
            Reference(setup, fields[i], buttons[i]);
        }
        setup.ApplyModifiedPropertiesWithoutUndo();
        preview.SetActive(false);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// FBXの座標変換を焼き込みUnityの正面に揃える
    /// </summary>
    private static Mesh Bake(MeshFilter source)
    {
        var mesh = Object.Instantiate(source.sharedMesh);
        mesh.name = source.name;
        var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * source.transform.localToWorldMatrix;
        mesh.vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        mesh.normals = mesh.normals.Select(n => matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Root + "/Mesh/FX-C-CP-01_" + source.name + ".asset");
        return mesh;
    }

    /// <summary>
    /// 袋を薄い青の半透明フィルムにして光沢と内部の矢印を両立する
    /// </summary>
    public static void ConfigurePouchMaterial(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_Smoothness", .72f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetFloat("_BlendModePreserveSpecular", 1f);
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetShaderPassEnabled("ShadowCaster", false);
    }

    /// <summary>
    /// 子オブジェクトを単位姿勢で作る
    /// </summary>
    private static Transform Child(string name, Transform parent)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    /// <summary>
    /// 共有マテリアルを作成する
    /// </summary>
    private static Material Material(string name, string shader, Color color)
    {
        var material = new Material(Shader.Find(shader));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .15f);
        AssetDatabase.CreateAsset(material, Root + "/Material/" + name + ".mat");
        return material;
    }

    /// <summary>
    /// 放射状の三角片または短寿命フラッシュを構成する
    /// </summary>
    private static ParticleSystem Particles(string name, Transform parent, Material material, Mesh mesh, bool rising)
    {
        var child = Child(name, parent);
        child.localPosition = new Vector3(0, .8f, -.1f);
        child.localRotation = Quaternion.Euler(-90, 0, 0);
        var particles = child.gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.maxParticles = mesh == null ? 1 : 32;
        main.startLifetime = mesh == null ? .12f : rising ? .85f : .55f;
        main.startSpeed = mesh == null ? 0f : rising ? 2.3f : 3.2f;
        main.startSize = mesh == null ? 1.3f : .24f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startColor = mesh == null ? new Color(1f, .95f, .65f) : rising ? new Color(.18f, .85f, 1f) : new Color(1f, .68f, .01f);
        main.gravityModifier = mesh == null ? 0f : .15f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = mesh != null;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = rising ? 32f : 78f;
        shape.radius = .12f;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        if (mesh != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            main.startRotation3D = true;
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }
        return particles;
    }

    /// <summary>
    /// Inspector参照をシリアライズして保存する
    /// </summary>
    private static void Reference(SerializedObject target, string field, Object value)
    {
        target.FindProperty(field).objectReferenceValue = value;
    }
}
