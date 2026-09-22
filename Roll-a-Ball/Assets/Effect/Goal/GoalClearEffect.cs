using UnityEngine;

/// <summary>
/// ゴールエリア到達時に一度だけ再生するクリアセレブレーションです
/// </summary>
public sealed class GoalClearEffect : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer areaPulseRenderer;

    [SerializeField]
    private ParticleSystem crystalBurst;

    [SerializeField]
    private ParticleSystem sparkleBurst;

    [SerializeField]
    private Light glowLight;

    [SerializeField]
    private Color areaPulseColor = new Color(0.35f, 0.9f, 1f, 1f);

    [SerializeField]
    private float duration = 1.2f;

    [SerializeField]
    private float pulseScale = 2.8f;

    [SerializeField]
    private float pulseExpandDuration = 0.12f;

    [SerializeField]
    private float glowLightIntensity = 8f;

    [SerializeField]
    private bool playOnEnable;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseAreaScale;
    private float elapsedTime;
    private bool isPlaying;
    private bool isFinishing;

    /// <summary>
    /// クリア演出の再生時間を取得
    /// </summary>
    public float Duration => Mathf.Max(0.1f, duration);

    /// <summary>
    /// エリア演出の初期状態を保存して非表示化
    /// </summary>
    private void Awake()
    {
        if (areaPulseRenderer != null)
        {
            baseAreaScale = areaPulseRenderer.transform.localScale;
        }

        Stop();
    }

    /// <summary>
    /// 有効化時に設定されている場合だけ演出を再生
    /// </summary>
    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    /// <summary>
    /// クリア演出の発光と粒子を更新
    /// </summary>
    private void Update()
    {
        if (!isPlaying && !isFinishing)
        {
            return;
        }

        if (isPlaying)
        {
            elapsedTime += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(elapsedTime / Duration);
            UpdateAreaPulse(normalizedTime);
            UpdateGlowLight(normalizedTime);

            if (elapsedTime >= Duration)
            {
                BeginParticleFinish();
            }
        }

        if (isFinishing && !HasAliveParticles())
        {
            FinishPlayback();
        }
    }

    /// <summary>
    /// クリアセレブレーションを最初から再生
    /// </summary>
    public void Play()
    {
        StopParticles();
        elapsedTime = 0f;
        isPlaying = true;
        isFinishing = false;

        if (areaPulseRenderer != null)
        {
            areaPulseRenderer.enabled = true;
        }

        if (glowLight != null)
        {
            glowLight.enabled = true;
        }

        UpdateAreaPulse(0f);
        UpdateGlowLight(0f);

        if (crystalBurst != null)
        {
            crystalBurst.Play();
        }

        if (sparkleBurst != null)
        {
            sparkleBurst.Play();
        }
    }

    /// <summary>
    /// クリアセレブレーションを停止して非表示化
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        isFinishing = false;
        StopParticles();

        if (areaPulseRenderer != null)
        {
            areaPulseRenderer.enabled = false;
            areaPulseRenderer.transform.localScale = baseAreaScale;
        }

        if (glowLight != null)
        {
            glowLight.intensity = 0f;
            glowLight.enabled = false;
        }
    }

    /// <summary>
    /// ゴールエリアの発光範囲と透明度を更新
    /// </summary>
    /// <param name="normalizedTime">再生時間の進行度です。</param>
    private void UpdateAreaPulse(float normalizedTime)
    {
        if (areaPulseRenderer == null)
        {
            return;
        }

        var expandDuration = Mathf.Max(0.01f, pulseExpandDuration / Duration);
        var fadeStart = Mathf.Max(expandDuration * 0.75f, 0.08f / Duration);
        var fadeDuration = Mathf.Max(0.01f, 0.32f / Duration);
        var expandProgress = Mathf.Clamp01(normalizedTime / expandDuration);
        var fadeProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normalizedTime - fadeStart) / fadeDuration));
        areaPulseRenderer.transform.localScale = baseAreaScale * Mathf.Lerp(0.72f, pulseScale, expandProgress);

        var color = areaPulseColor;
        color.a *= (1f - fadeProgress) * 0.86f;
        SetRendererColor(areaPulseRenderer, color);
    }

    /// <summary>
    /// ゴール到達の瞬間からライトを徐々に消す
    /// </summary>
    /// <param name="normalizedTime">再生時間の進行度です。</param>
    private void UpdateGlowLight(float normalizedTime)
    {
        if (glowLight == null)
        {
            return;
        }

        var fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime / 0.82f));
        glowLight.intensity = glowLightIntensity * fade;
    }

    /// <summary>
    /// エリア演出を終え、粒子の自然な消滅だけを待つ
    /// </summary>
    private void BeginParticleFinish()
    {
        isPlaying = false;
        isFinishing = true;
        StopParticleEmission();

        if (areaPulseRenderer != null)
        {
            areaPulseRenderer.enabled = false;
        }

        if (glowLight != null)
        {
            glowLight.intensity = 0f;
            glowLight.enabled = false;
        }
    }

    /// <summary>
    /// 全粒子が消えた後に演出を初期状態へ戻す
    /// </summary>
    private void FinishPlayback()
    {
        isFinishing = false;
        StopParticles();

        if (areaPulseRenderer != null)
        {
            areaPulseRenderer.transform.localScale = baseAreaScale;
        }
    }

    /// <summary>
    /// 粒子の新規発生だけを止め、表示中の粒子を残す
    /// </summary>
    private void StopParticleEmission()
    {
        StopEmission(crystalBurst);
        StopEmission(sparkleBurst);
    }

    /// <summary>
    /// 指定した粒子システムの新規発生を止める
    /// </summary>
    /// <param name="particleSystem">発生を止める粒子システムです。</param>
    private static void StopEmission(ParticleSystem particleSystem)
    {
        if (particleSystem != null)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// 管理している粒子がまだ表示中か確認する
    /// </summary>
    /// <returns>粒子が残っている場合はtrueです。</returns>
    private bool HasAliveParticles()
    {
        return IsAlive(crystalBurst) || IsAlive(sparkleBurst);
    }

    /// <summary>
    /// 指定した粒子システムに表示中の粒子があるか確認する
    /// </summary>
    /// <param name="particleSystem">確認する粒子システムです。</param>
    /// <returns>粒子が残っている場合はtrueです。</returns>
    private static bool IsAlive(ParticleSystem particleSystem)
    {
        return particleSystem != null && particleSystem.IsAlive(true);
    }

    /// <summary>
    /// 指定したエリアRendererの色と透明度を更新
    /// </summary>
    /// <param name="renderer">色を更新するRendererです。</param>
    /// <param name="color">設定する色です。</param>
    private void SetRendererColor(MeshRenderer renderer, Color color)
    {
        if (renderer.sharedMaterial == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        propertyBlock.SetColor(BaseColorProperty, color);
        propertyBlock.SetColor(ColorProperty, color);
        renderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// 管理している全粒子を停止して残像を消去
    /// </summary>
    private void StopParticles()
    {
        StopParticle(crystalBurst);
        StopParticle(sparkleBurst);
    }

    /// <summary>
    /// 指定した粒子システムを停止して消去
    /// </summary>
    /// <param name="particleSystem">停止する粒子システムです。</param>
    private static void StopParticle(ParticleSystem particleSystem)
    {
        if (particleSystem != null)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
