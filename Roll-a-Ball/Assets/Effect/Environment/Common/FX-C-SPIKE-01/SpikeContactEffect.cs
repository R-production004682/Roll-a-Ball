using UnityEngine;

/// <summary>
/// トゲ接触時の危険表示を一度だけ再生する共通エフェクトです
/// </summary>
public sealed class SpikeContactEffect : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem flashBurst;

    [SerializeField]
    private ParticleSystem shardBurst;

    [SerializeField]
    private ParticleSystem sparkBurst;

    [SerializeField]
    private Light flashLight;

    [SerializeField]
    private Color flashColor = new Color(1f, 0.12f, 0.08f, 1f);

    [SerializeField]
    private float duration = 0.42f;

    [SerializeField]
    private float flashLightIntensity = 6f;

    [SerializeField]
    private float lightFadeDuration = 0.18f;

    private float elapsedTime;
    private bool isPlaying;

    /// <summary>
    /// エフェクトの再生時間を取得
    /// </summary>
    public float Duration => Mathf.Max(0.1f, duration);

    /// <summary>
    /// エフェクトが再生中かどうかを取得
    /// </summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// 初期状態で粒子とライトを停止
    /// </summary>
    private void Awake()
    {
        StopParticles();
        ResetLight();
    }

    /// <summary>
    /// 再生中のライトを更新して時間切れで停止
    /// </summary>
    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateFlashLight();

        if (elapsedTime >= Duration)
        {
            Stop();
        }
    }

    /// <summary>
    /// 指定位置と向きでトゲ接触エフェクトを再生
    /// </summary>
    /// <param name="position">接触位置です。</param>
    /// <param name="normal">接触面から外向きの方向です。</param>
    public void PlayAt(Vector3 position, Vector3 normal)
    {
        transform.position = position;

        if (normal.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(normal.normalized);
        }

        Play();
    }

    /// <summary>
    /// トゲ接触エフェクトを最初から再生
    /// </summary>
    public void Play()
    {
        StopParticles();
        elapsedTime = 0f;
        isPlaying = true;

        if (flashLight != null)
        {
            flashLight.color = flashColor;
            flashLight.enabled = true;
        }

        UpdateFlashLight();
        PlayParticle(flashBurst);
        PlayParticle(shardBurst);
        PlayParticle(sparkBurst);
    }

    /// <summary>
    /// トゲ接触エフェクトを停止して残像を消去
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        StopParticles();
        ResetLight();
    }

    /// <summary>
    /// 接触直後の赤いフラッシュを徐々に弱める
    /// </summary>
    private void UpdateFlashLight()
    {
        if (flashLight == null)
        {
            return;
        }

        var fadeDuration = Mathf.Max(0.01f, lightFadeDuration);
        var fade = 1f - Mathf.SmoothStep(0f, 1f, elapsedTime / fadeDuration);
        flashLight.intensity = flashLightIntensity * Mathf.Clamp01(fade);
    }

    /// <summary>
    /// 管理しているライトを初期状態に戻す
    /// </summary>
    private void ResetLight()
    {
        if (flashLight == null)
        {
            return;
        }

        flashLight.color = flashColor;
        flashLight.intensity = 0f;
        flashLight.enabled = false;
    }

    /// <summary>
    /// 管理している全粒子を停止して消去
    /// </summary>
    private void StopParticles()
    {
        StopParticle(flashBurst);
        StopParticle(shardBurst);
        StopParticle(sparkBurst);
    }

    /// <summary>
    /// 指定した粒子システムを再生
    /// </summary>
    /// <param name="particleSystem">再生する粒子システムです。</param>
    private static void PlayParticle(ParticleSystem particleSystem)
    {
        if (particleSystem != null)
        {
            particleSystem.Play(true);
        }
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
