using UnityEngine;

/// <summary>
/// そよ風に運ばれる葉と光粒の連続再生を制御する
/// </summary>
[ExecuteAlways]
public sealed class LeafWindEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem leafParticles;
    [SerializeField] private ParticleSystem lightParticles;
    [SerializeField] private WindZone windZone;
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0.08f, 0.25f);
    [SerializeField, Range(0.05f, 1.5f)] private float windSpeed = 0.34f;
    [SerializeField, Range(0f, 1f)] private float turbulence = 0.16f;
    [SerializeField] private Vector3 localVolumeSize = new Vector3(12f, 5f, 12f);

    private ParticleSystem.Particle[] leafBuffer;
    private ParticleSystem.Particle[] lightBuffer;
    private bool initialized;

    /// <summary>
    /// 参照を検証して風の初期値を各粒子システムへ渡す
    /// </summary>
    private void OnEnable()
    {
        if (leafParticles == null || lightParticles == null)
        {
            Debug.LogError("そよ風の葉エフェクトには葉粒子と光粒子を指定してください。", this);
            enabled = false;
            return;
        }

        initialized = true;
        ApplyWind();
        if (Application.isPlaying)
        {
            Play();
        }
    }

    /// <summary>
    /// Inspectorで変更した風の値を編集時にも粒子へ反映する
    /// </summary>
    private void OnValidate()
    {
        localVolumeSize.x = Mathf.Max(0.5f, localVolumeSize.x);
        localVolumeSize.y = Mathf.Max(0.5f, localVolumeSize.y);
        localVolumeSize.z = Mathf.Max(0.5f, localVolumeSize.z);
        ApplyWind();
    }

    /// <summary>
    /// 粒子の風向きと揺らぎを更新し再生中だけ範囲内へ循環させる
    /// </summary>
    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        ApplyWind();
        if (!Application.isPlaying)
        {
            return;
        }

        WrapParticles(leafParticles, ref leafBuffer);
        WrapParticles(lightParticles, ref lightBuffer);
    }

    /// <summary>
    /// 風の設定を葉と光粒のVelocity over Lifetimeへ反映する
    /// </summary>
    private void ApplyWind()
    {
        if (leafParticles == null || lightParticles == null)
        {
            return;
        }

        var direction = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.forward;
        ApplyWindToParticleSystem(leafParticles, direction, windSpeed, turbulence);
        ApplyWindToParticleSystem(lightParticles, direction, windSpeed * 0.8f, turbulence * 0.8f);
        if (windZone != null)
        {
            windZone.windMain = Mathf.Clamp(windSpeed, 0f, 1f);
            windZone.windTurbulence = turbulence;
        }
    }

    /// <summary>
    /// 指定した粒子システムに一定方向の風と自然な揺らぎを設定する
    /// </summary>
    private static void ApplyWindToParticleSystem(ParticleSystem target, Vector3 direction, float speed, float noiseStrength)
    {
        var velocity = target.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(direction.x * speed);
        velocity.y = new ParticleSystem.MinMaxCurve(direction.y * speed);
        velocity.z = new ParticleSystem.MinMaxCurve(direction.z * speed);

        var noise = target.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = 0.24f;
        noise.scrollSpeed = 0.16f;
        noise.damping = true;
    }

    /// <summary>
    /// 粒子を局所ボリュームの反対側へ循環させて長時間再生を安定させる
    /// </summary>
    private void WrapParticles(ParticleSystem target, ref ParticleSystem.Particle[] buffer)
    {
        var maximum = Mathf.Max(1, target.main.maxParticles);
        if (buffer == null || buffer.Length < maximum)
        {
            buffer = new ParticleSystem.Particle[maximum];
        }

        var count = target.GetParticles(buffer);
        var half = localVolumeSize * 0.5f;
        for (var i = 0; i < count; i++)
        {
            var position = buffer[i].position;
            position.x = Wrap(position.x, -half.x, half.x, localVolumeSize.x);
            position.y = Wrap(position.y, -half.y, half.y, localVolumeSize.y);
            position.z = Wrap(position.z, -half.z, half.z, localVolumeSize.z);
            buffer[i].position = position;
        }

        target.SetParticles(buffer, count);
    }

    /// <summary>
    /// 値を指定した範囲へ循環させる
    /// </summary>
    private static float Wrap(float value, float minimum, float maximum, float size)
    {
        return Mathf.Repeat(value - minimum, size) + minimum;
    }

    /// <summary>
    /// 葉と光粒を再生してそよ風を開始する
    /// </summary>
    public void Play()
    {
        if (leafParticles != null)
        {
            leafParticles.Play(true);
        }

        if (lightParticles != null)
        {
            lightParticles.Play(true);
        }
    }

    /// <summary>
    /// 葉と光粒を停止して表示中の粒子を消す
    /// </summary>
    public void Stop()
    {
        if (leafParticles != null)
        {
            leafParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (lightParticles != null)
        {
            lightParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>
    /// 風向きと速度を実行時に変更する
    /// </summary>
    public void SetWind(Vector3 direction, float speed)
    {
        windDirection = direction;
        windSpeed = Mathf.Max(0f, speed);
        ApplyWind();
    }

    /// <summary>
    /// Prefab全体を表示または非表示にする
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible)
        {
            Play();
        }
    }

    /// <summary>
    /// 無効化時に再生状態を残さず表示を停止する
    /// </summary>
    private void OnDisable()
    {
        initialized = false;
        Stop();
    }
}
