using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Play Modeで実際のColliderとParticleSystemを用いて移動粉塵の回帰確認を行う
/// </summary>
public static class MovementDustValidation
{
    private const string Root = "Assets/Effect/Environment/Stage/FX-S1-DUST-01";

    /// <summary>
    /// テスト用オブジェクトだけを生成・破棄し、失敗項目と粒子数・割当量を返す
    /// </summary>
    public static string Validate()
    {
        if (!Application.isPlaying)
        {
            return "Play Modeで実行してください";
        }

        var failures = new List<string>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefab/FX-S1-DUST-01_ground_dust.prefab");
        var grass = AssetDatabase.LoadAssetAtPath<MovementSurfaceProfile>(Root + "/Profile/GrassMovementSurface.asset");
        var dirt = AssetDatabase.LoadAssetAtPath<MovementSurfaceProfile>(Root + "/Profile/DirtMovementSurface.asset");
        if (prefab == null || grass == null || dirt == null)
        {
            return "PrefabとGrass/Dirt Profileが必要です";
        }

        var root = new GameObject("MovementDustValidation_Temporary");
        root.SetActive(false);
        CreateGround(root.transform, new Vector3(98f, -0.1f, 100f), grass);
        CreateGround(root.transform, new Vector3(102f, -0.1f, 100f), dirt);
        var ball = new GameObject("ValidationBall", typeof(SphereCollider), typeof(Rigidbody));
        ball.transform.SetParent(root.transform, false);
        ball.transform.position = new Vector3(98f, 0.51f, 100f);
        var body = ball.GetComponent<Rigidbody>();
        body.isKinematic = true;
        var instance = Object.Instantiate(prefab, root.transform);
        var effect = instance.GetComponent<GroundDustEffect>();
        var movement = ball.AddComponent<PlayerMovementEffect>();
        SetReference(movement, "playerCollider", ball.GetComponent<Collider>());
        SetReference(movement, "effect", effect);
        root.SetActive(true);
        var method = typeof(PlayerMovementEffect).GetMethod("SampleMovement", BindingFlags.NonPublic | BindingFlags.Instance);
        var sample = (Action<float>)method.CreateDelegate(typeof(Action<float>), movement);
        var systems = instance.GetComponentsInChildren<ParticleSystem>();
        var particles = new ParticleSystem.Particle[28];

        Physics.SyncTransforms();
        sample(1f / 60f);
        Check(movement.IsGrounded && !effect.IsEmitting && effect.ParticleCount == 0, "停止中に放出", failures);
        Step(ball.transform, new Vector3(0.03f, 0f, 0f), sample);
        Check(movement.IsGrounded && movement.Surface == grass && effect.IsEmitting && effect.ParticleCount > 0,
            "草地移動を検出できない", failures);
        var before = ball.transform.position;
        var rotation = ball.transform.rotation;
        sample(1f / 60f);
        Check(ball.transform.position == before && ball.transform.rotation == rotation && body.isKinematic,
            "PlayerのTransform/物理設定を変更", failures);
        Check(!effect.IsEmitting, "移動停止後に放出継続", failures);
        Simulate(systems, 0.9f);
        Check(effect.ParticleCount == 0, "停止後に粒子が残留", failures);

        Step(ball.transform, new Vector3(0.03f, 0.015f, 0f), sample);
        Check(!movement.IsGrounded && !effect.IsEmitting, "離陸直後に放出", failures);
        ball.transform.position += Vector3.up;
        movement.ResetEffect();
        Step(ball.transform, new Vector3(0.03f, -0.1f, 0f), sample);
        Check(!movement.IsGrounded && !effect.IsEmitting, "落下中に放出", failures);

        ball.transform.position = new Vector3(102f, 0.51f, 100f);
        movement.ResetEffect();
        Step(ball.transform, new Vector3(0.03f, 0f, 0f), sample);
        Check(movement.Surface == dirt && effect.IsEmitting, "土への地面切替に失敗", failures);
        effect.Stop(true);
        movement.ResetEffect();
        Step(ball.transform, new Vector3(-0.03f, 0f, 0f), sample);
        systems[0].GetParticles(particles);
        Check(effect.IsEmitting && particles[0].velocity.x > 0f, "逆方向の後方へ放出されない", failures);

        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.transform.SetParent(root.transform, false);
        blocker.transform.position = new Vector3(102f, 0.01f, 100f);
        blocker.transform.localScale = new Vector3(3f, 0.02f, 3f);
        Step(ball.transform, new Vector3(0.03f, 0f, 0f), sample);
        Check(movement.Surface == null && !effect.IsEmitting, "未指定地面の下から粉塵を拾う", failures);
        blocker.GetComponent<Collider>().isTrigger = true;
        Step(ball.transform, new Vector3(0.03f, 0f, 0f), sample);
        Check(movement.Surface == dirt && effect.IsEmitting, "Triggerが足元判定を妨害", failures);
        Object.DestroyImmediate(blocker);

        Step(ball.transform, new Vector3(-4f, 0f, 0f), sample);
        Check(!effect.IsEmitting && effect.ParticleCount == 0, "ワープ後に粒子が残留", failures);
        Step(ball.transform, new Vector3(0.03f, 0f, 0f), sample);
        movement.ResetEffect();
        Check(effect.ParticleCount == 0 && movement.Speed == 0f, "明示的リスポーンAPIが消去しない", failures);

        for (var i = 0; i < 10; i++)
        {
            Step(ball.transform, new Vector3(0.02f, 0f, 0f), sample);
            Check(effect.IsEmitting && effect.ParticleCount > 0, "再利用時の放出失敗 " + i, failures);
            movement.enabled = false;
            Check(effect.ParticleCount == 0, "無効化後の残留 " + i, failures);
            movement.enabled = true;
        }

        var slowPeak = MeasurePeak(effect, systems, grass, 0.7f);
        var fastPeak = MeasurePeak(effect, systems, grass, 6f);
        Check(fastPeak > slowPeak && fastPeak <= 40, "速度連動または粒子上限が不正", failures);
        effect.Play(grass, Vector3.right * 6f, Vector3.zero, Vector3.up, false, 0.1f);
        Check(!effect.IsEmitting, "公開APIの空中フラグが無効", failures);
        effect.Stop(true);

        var grassSystem = instance.transform.Find("SurfaceDetails").GetComponent<ParticleSystem>();
        var pebbleSystem = instance.transform.Find("Pebbles").GetComponent<ParticleSystem>();
        var grassMesh = grassSystem.GetComponent<ParticleSystemRenderer>().mesh;
        var pebbleMesh = pebbleSystem.GetComponent<ParticleSystemRenderer>().mesh;
        Check(grassMesh.bounds.size.y <= 1.01f && pebbleMesh.bounds.size.x <= 1.01f,
            "粒子用Meshに100倍のFBXスケールが残っている", failures);
        Check(instance.transform.Find("Dust").GetComponent<ParticleSystemRenderer>().renderMode
            == ParticleSystemRenderMode.Billboard, "粉塵が巨大な立体Meshを使用", failures);
        MeasurePeak(effect, systems, grass, 6f);
        var grassCount = grassSystem.particleCount;
        Check(grassCount > 0, "草片が発生しない", failures);
        effect.Play(dirt, Vector3.right * 6f, Vector3.zero, Vector3.up, true, 0.1f);
        Check(grassSystem.particleCount == grassCount
            && grassSystem.GetComponent<ParticleSystemRenderer>().mesh == grassMesh
            && pebbleSystem.particleCount > 0 && pebbleMesh != grassMesh,
            "土への切替で既存の草が消える、変形する、または小石が発生しない", failures);
        effect.Stop();
        Simulate(systems, 0.9f);
        Check(effect.ParticleCount == 0, "草・小石の独立レイヤーが停止後に残留", failures);

        var second = Object.Instantiate(prefab, root.transform).GetComponent<GroundDustEffect>();
        effect.Play(grass, Vector3.right * 3f, Vector3.zero, Vector3.up, true, 0.02f);
        second.Play(dirt, Vector3.left * 3f, Vector3.one, Vector3.up, true, 0.02f);
        Check(effect.ParticleCount > 0 && second.ParticleCount > 0, "複数同時再生に失敗", failures);
        second.Stop(true);
        Check(effect.ParticleCount > 0 && second.ParticleCount == 0, "別個体の停止が干渉", failures);

        movement.ResetEffect();
        for (var i = 0; i < 40; i++)
        {
            Step(ball.transform, new Vector3(0.001f, 0f, 0f), sample);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 120; i++)
        {
            Step(ball.transform, new Vector3(i % 2 == 0 ? 0.03f : -0.03f, 0f, 0f), sample);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Check(allocated == 0, "ウォームアップ後120回の移動処理でGC割当 " + allocated, failures);
        Object.DestroyImmediate(root);
        Physics.SyncTransforms();
        return "Checks: " + (failures.Count == 0 ? "PASS" : "FAIL") + "; slow peak=" + slowPeak
            + "; fast peak=" + fastPeak + "; max budget=40; GC/120 samples=" + allocated
            + "; failures=" + string.Join(" / ", failures);
    }

    /// <summary>
    /// テスト中だけTransformを同期し、一定時間の実移動をサンプリングする
    /// </summary>
    private static void Step(Transform ball, Vector3 displacement, Action<float> sample)
    {
        ball.position += displacement;
        Physics.SyncTransforms();
        sample(1f / 60f);
    }

    /// <summary>
    /// 一定速度で2秒再生し同時生存粒子のピークを測る
    /// </summary>
    private static int MeasurePeak(GroundDustEffect effect, ParticleSystem[] systems, MovementSurfaceProfile profile, float speed)
    {
        effect.Stop(true);
        var peak = 0;
        for (var i = 0; i < 120; i++)
        {
            effect.Play(profile, Vector3.right * speed, Vector3.zero, Vector3.up, true, 1f / 60f);
            Simulate(systems, 1f / 60f);
            peak = Mathf.Max(peak, effect.ParticleCount);
        }

        return peak;
    }

    /// <summary>
    /// 粒子の自然消滅をEditor一時停止中にも検証する
    /// </summary>
    private static void Simulate(ParticleSystem[] systems, float duration)
    {
        foreach (var system in systems)
        {
            system.Simulate(duration, false, false, false);
        }
    }

    /// <summary>
    /// 検証失敗を収集してテストオブジェクトを最後まで片付けられるようにする
    /// </summary>
    private static void Check(bool passed, string message, List<string> failures)
    {
        if (!passed)
        {
            failures.Add(message);
        }
    }

    /// <summary>
    /// 検証専用の地面と地面種類の参照を構成する
    /// </summary>
    private static void CreateGround(Transform parent, Vector3 position, MovementSurfaceProfile profile)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetParent(parent, false);
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(4f, 0.2f, 4f);
        SetReference(ground.AddComponent<MovementSurface>(), "profile", profile);
    }

    /// <summary>
    /// Awake前にテスト対象のSerializeFieldへ参照を設定する
    /// </summary>
    private static void SetReference(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
