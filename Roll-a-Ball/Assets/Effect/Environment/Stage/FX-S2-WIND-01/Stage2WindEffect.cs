using UnityEngine;

/// <summary>
/// ステージ2の密集林に配置する、視認性のある風の流れを制御する
/// </summary>
[ExecuteAlways]
public sealed class Stage2WindEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem leafParticles;
    [SerializeField] private ParticleSystem wetLightParticles;
    [SerializeField] private WindZone windZone;
    [SerializeField] private WindSwayTarget[] swayTargets;
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0.16f, 0.32f);
    [SerializeField, Range(0.1f, 2.5f)] private float windStrength = 0.92f;
    [SerializeField, Range(0f, 1f)] private float turbulence = 0.48f;
    [SerializeField] private Vector3 localVolumeSize = new Vector3(8f, 3.8f, 8f);

    private ParticleSystem.Particle[] leafBuffer;
    private ParticleSystem.Particle[] lightBuffer;
    private Vector3 appliedDirection;
    private float appliedStrength;
    private float appliedTurbulence;
    private Vector3 appliedVolumeSize;
    private bool hasAppliedSettings;
    private Vector3 pushedDirection;
    private float pushedStrength;
    private float pushedTurbulence;
    private bool hasPushedWind;
    private bool initialized;

    /// <summary>
    /// Inspectorから風向きを読み取る
    /// </summary>
    public Vector3 WindDirection => windDirection;

    /// <summary>
    /// Inspectorから風の強さを読み取る
    /// </summary>
    public float WindStrength => windStrength;

    /// <summary>
    /// Inspectorから局所エリアの大きさを読み取る
    /// </summary>
    public Vector3 FlowRange => localVolumeSize;

    /// <summary>
    /// 参照を検証して、粒子と樹木へ初期の風を反映する
    /// </summary>
    private void OnEnable()
    {
        if (leafParticles == null || wetLightParticles == null)
        {
            Debug.LogError("FX-S2-WIND-01には葉片粒子と湿った光粒子を指定してください。", this);
            enabled = false;
            return;
        }

        if (swayTargets == null || swayTargets.Length == 0)
        {
            swayTargets = GetComponentsInChildren<WindSwayTarget>(true);
        }

        hasAppliedSettings = false;
        hasPushedWind = false;
        initialized = true;
        ApplyWind();
        PushWindToTargets();
        if (Application.isPlaying)
        {
            Play();
        }
    }

    /// <summary>
    /// Inspectorで変更した範囲を安全な値へ整え、編集時の粒子設定へ反映する
    /// </summary>
    private void OnValidate()
    {
        localVolumeSize.x = Mathf.Max(0.5f, localVolumeSize.x);
        localVolumeSize.y = Mathf.Max(0.5f, localVolumeSize.y);
        localVolumeSize.z = Mathf.Max(0.5f, localVolumeSize.z);
        windStrength = Mathf.Max(0f, windStrength);
        turbulence = Mathf.Clamp01(turbulence);
        hasAppliedSettings = false;
        hasPushedWind = false;
        ApplyWind();
    }

    /// <summary>
    /// 風設定を連続適用し、粒子を局所範囲内で循環させる
    /// </summary>
    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            return;
        }

        WrapParticles(leafParticles, ref leafBuffer);
        WrapParticles(wetLightParticles, ref lightBuffer);
    }

    /// <summary>
    /// 風向きと強さを粒子、Wind Zone、揺れ対象へまとめて反映する
    /// </summary>
    private void ApplyWind()
    {
        if (leafParticles == null || wetLightParticles == null)
        {
            return;
        }

        var direction = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.forward;
        if (hasAppliedSettings
            && direction == appliedDirection
            && Mathf.Approximately(windStrength, appliedStrength)
            && Mathf.Approximately(turbulence, appliedTurbulence)
            && localVolumeSize == appliedVolumeSize)
        {
            return;
        }

        ApplyWindToParticleSystem(leafParticles, direction, windStrength, turbulence, 0.24f);
        ApplyWindToParticleSystem(wetLightParticles, direction, windStrength * 0.82f, turbulence * 0.9f, 0.32f);
        UpdateWindZone(direction);
        appliedDirection = direction;
        appliedStrength = windStrength;
        appliedTurbulence = turbulence;
        appliedVolumeSize = localVolumeSize;
        hasAppliedSettings = true;
    }

    /// <summary>
    /// 粒子システムへ方向性、乱流、局所エリアを設定する
    /// </summary>
    private void ApplyWindToParticleSystem(
        ParticleSystem target,
        Vector3 direction,
        float speed,
        float noiseStrength,
        float noiseFrequency)
    {
        var main = target.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var shape = target.shape;
        shape.scale = localVolumeSize;
        var velocity = target.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(direction.x * speed);
        velocity.y = new ParticleSystem.MinMaxCurve(direction.y * speed);
        velocity.z = new ParticleSystem.MinMaxCurve(direction.z * speed);

        var noise = target.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = noiseFrequency;
        noise.scrollSpeed = 0.42f;
        noise.damping = true;
    }

    /// <summary>
    /// 風の向きと強さをWind Zoneへ反映する
    /// </summary>
    private void UpdateWindZone(Vector3 direction)
    {
        if (windZone == null)
        {
            return;
        }

        windZone.mode = WindZoneMode.Directional;
        windZone.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
        windZone.windMain = Mathf.Clamp(windStrength, 0f, 1f);
        windZone.windTurbulence = turbulence;
        windZone.windPulseMagnitude = 0.16f + turbulence * 0.12f;
        windZone.windPulseFrequency = 0.42f + windStrength * 0.18f;
    }

    /// <summary>
    /// 現在の風パラメータをすべての木や草へ渡す
    /// </summary>
    private void PushWindToTargets()
    {
        if (swayTargets == null)
        {
            return;
        }

        var direction = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.forward;
        if (hasPushedWind
            && direction == pushedDirection
            && Mathf.Approximately(windStrength, pushedStrength)
            && Mathf.Approximately(turbulence, pushedTurbulence))
        {
            return;
        }

        for (var i = 0; i < swayTargets.Length; i++)
        {
            if (swayTargets[i] != null)
            {
                swayTargets[i].SetWind(windDirection, windStrength, turbulence);
            }
        }

        pushedDirection = direction;
        pushedStrength = windStrength;
        pushedTurbulence = turbulence;
        hasPushedWind = true;
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
    /// 設定した風の流れを再生する
    /// </summary>
    public void Play()
    {
        if (leafParticles != null)
        {
            leafParticles.Play(true);
        }

        if (wetLightParticles != null)
        {
            wetLightParticles.Play(true);
        }
    }

    /// <summary>
    /// 表示中の粒子を消して風の流れを停止する
    /// </summary>
    public void Stop()
    {
        if (leafParticles != null)
        {
            leafParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (wetLightParticles != null)
        {
            wetLightParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (swayTargets != null)
        {
            for (var i = 0; i < swayTargets.Length; i++)
            {
                if (swayTargets[i] != null)
                {
                    swayTargets[i].ResetWind();
                }
            }
        }

        hasPushedWind = false;
    }

    /// <summary>
    /// 実行時に風向きと強さを変更する公開API
    /// </summary>
    public void SetWind(Vector3 direction, float strength)
    {
        windDirection = direction;
        windStrength = Mathf.Max(0f, strength);
        hasAppliedSettings = false;
        hasPushedWind = false;
        ApplyWind();
        PushWindToTargets();
    }

    /// <summary>
    /// 実行時に風の乱流を変更する公開API
    /// </summary>
    public void SetTurbulence(float turbulenceAmount)
    {
        turbulence = Mathf.Clamp01(turbulenceAmount);
        hasAppliedSettings = false;
        hasPushedWind = false;
        ApplyWind();
        PushWindToTargets();
    }

    /// <summary>
    /// 実行時に風が流れる局所範囲を変更する公開API
    /// </summary>
    public void SetFlowRange(Vector3 range)
    {
        localVolumeSize = new Vector3(
            Mathf.Max(0.5f, range.x),
            Mathf.Max(0.5f, range.y),
            Mathf.Max(0.5f, range.z));
        hasAppliedSettings = false;
        ApplyWind();
    }

    /// <summary>
    /// エフェクトを表示または非表示にする
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
    /// 無効化時に粒子と揺れを停止する
    /// </summary>
    private void OnDisable()
    {
        initialized = false;
        Stop();
    }
}
