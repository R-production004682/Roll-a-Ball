using UnityEngine;

/// <summary>
/// コイン取得時に再生する、ローポリ風のクリスタルエフェクトです
/// </summary>
public sealed class CurrencyPickupEffect : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem crystalBurst;

    [SerializeField]
    private ParticleSystem largeRewardBurst;

    [SerializeField]
    private ParticleSystem corePulse;

    [SerializeField]
    private ParticleSystem impactFlash;

    [SerializeField]
    private ParticleSystem sparkleBurst;

    [SerializeField]
    private ParticleSystem largeSparkleBurst;

    [SerializeField]
    private Transform crossSpark;

    [SerializeField]
    private MeshRenderer crossSparkRenderer;

    [SerializeField]
    private Transform crossSparkAccent;

    [SerializeField]
    private MeshRenderer crossSparkAccentRenderer;

    [SerializeField]
    private Light pulseLight;

    [SerializeField]
    private bool playOnEnable;

    [SerializeField]
    private bool destroyOnComplete = true;

    [SerializeField]
    private float duration = 1.1f;

    [SerializeField]
    private float pulseLightIntensity = 3.5f;

    [SerializeField]
    private bool isLargeReward;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private float elapsedTime;
    private bool isPlaying;
    private bool isFinishing;
    private MaterialPropertyBlock crossSparkPropertyBlock;

    /// <summary>
    /// エフェクトを有効化したときの初期再生設定を適用
    /// </summary>
    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play(isLargeReward);
        }
    }

    /// <summary>
    /// 再生中の弾ける光とライトを更新し、指定時間後にエフェクトを終了
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
            var normalizedTime = Mathf.Clamp01(elapsedTime / Mathf.Max(0.1f, duration));
            UpdateCrossSpark(normalizedTime);
            UpdatePulseLight(normalizedTime);

            if (elapsedTime >= Mathf.Max(0.1f, duration))
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
    /// 小さい報酬用のエフェクトを再生
    /// </summary>
    public void Play()
    {
        Play(isLargeReward);
    }

    /// <summary>
    /// 報酬サイズを指定してエフェクトを再生
    /// </summary>
    /// <param name="largeReward">大きい報酬の場合は豪華版の粒子を追加します。</param>
    public void Play(bool largeReward)
    {
        isLargeReward = largeReward;
        elapsedTime = 0f;
        isPlaying = true;
        isFinishing = false;

        StopParticles();

        if (largeRewardBurst != null)
        {
            largeRewardBurst.gameObject.SetActive(largeReward);
        }

        if (crystalBurst != null)
        {
            crystalBurst.Play();
        }

        if (corePulse != null)
        {
            corePulse.Play();
        }

        if (largeReward && largeRewardBurst != null)
        {
            largeRewardBurst.Play();
        }

        if (impactFlash != null)
        {
            impactFlash.gameObject.SetActive(largeReward);
            if (largeReward)
            {
                impactFlash.Play();
            }
        }

        if (sparkleBurst != null)
        {
            sparkleBurst.Play();
        }

        if (largeSparkleBurst != null)
        {
            largeSparkleBurst.gameObject.SetActive(largeReward);
            if (largeReward)
            {
                largeSparkleBurst.Play();
            }
        }

        if (crossSpark != null)
        {
            crossSpark.gameObject.SetActive(largeReward);
            if (largeReward)
            {
                crossSpark.localScale = Vector3.one * 0.1f;
            }
        }

        if (crossSparkAccent != null)
        {
            crossSparkAccent.gameObject.SetActive(largeReward);
            if (largeReward)
            {
                crossSparkAccent.localScale = Vector3.one * 0.1f;
            }
        }

        SetCrossSparkColor(largeReward ? 0.78f : 0f);

        if (pulseLight != null)
        {
            pulseLight.enabled = largeReward;
            pulseLight.intensity = largeReward ? pulseLightIntensity : 0f;
        }
    }

    /// <summary>
    /// 再生中の粒子と光を停止して初期状態へ戻す
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        isFinishing = false;
        StopParticles();

        if (largeRewardBurst != null)
        {
            largeRewardBurst.gameObject.SetActive(false);
        }

        if (largeSparkleBurst != null)
        {
            largeSparkleBurst.gameObject.SetActive(false);
        }

        if (crossSpark != null)
        {
            crossSpark.localScale = Vector3.one * 0.1f;
            crossSpark.gameObject.SetActive(false);
        }

        if (crossSparkAccent != null)
        {
            crossSparkAccent.localScale = Vector3.one * 0.1f;
            crossSparkAccent.gameObject.SetActive(false);
        }

        SetCrossSparkColor(0f);

        if (pulseLight != null)
        {
            pulseLight.intensity = 0f;
            pulseLight.enabled = false;
        }
    }

    /// <summary>
    /// 取得直後に十字の光を弾けさせ、その後に少し縮めながら透明にする
    /// </summary>
    /// <param name="normalizedTime">再生時間の進行度です。</param>
    private void UpdateCrossSpark(float normalizedTime)
    {
        if (!isLargeReward || (crossSpark == null && crossSparkAccent == null))
        {
            return;
        }

        var burstProgress = Mathf.Clamp01(normalizedTime / 0.22f);
        var burst = Mathf.SmoothStep(0f, 1f, burstProgress);
        var maxScale = isLargeReward ? 0.95f : 0.78f;
        var settle = 1f - Mathf.SmoothStep(0.22f, 1f, normalizedTime) * 0.35f;
        var scale = Mathf.Lerp(0.1f, maxScale, burst) * settle;

        if (crossSpark != null)
        {
            crossSpark.localScale = Vector3.one * scale;
        }

        if (crossSparkAccent != null)
        {
            crossSparkAccent.localScale = Vector3.one * (scale * 0.75f);
        }

        var alpha = 0.78f * (1f - Mathf.SmoothStep(0.18f, 1f, normalizedTime));
        SetCrossSparkColor(alpha);
    }

    /// <summary>
    /// 取得直後に強く光り、徐々に消えるライトを更新
    /// </summary>
    /// <param name="normalizedTime">再生時間の進行度です。</param>
    private void UpdatePulseLight(float normalizedTime)
    {
        if (!isLargeReward || pulseLight == null)
        {
            return;
        }

        var fade = 1f - Mathf.SmoothStep(0f, 1f, normalizedTime);
        pulseLight.intensity = pulseLightIntensity * fade;
    }

    /// <summary>
    /// 本体演出を終え、粒子の自然な消滅だけを待つ
    /// </summary>
    private void BeginParticleFinish()
    {
        isPlaying = false;
        isFinishing = true;
        StopParticleEmission();

        if (pulseLight != null)
        {
            pulseLight.intensity = 0f;
            pulseLight.enabled = false;
        }
    }

    /// <summary>
    /// 全粒子が消えた後の後始末を行う
    /// </summary>
    private void FinishPlayback()
    {
        var shouldDestroy = destroyOnComplete;
        Stop();

        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 粒子の新規発生だけを止め、表示中の粒子を残す
    /// </summary>
    private void StopParticleEmission()
    {
        StopEmission(crystalBurst);
        StopEmission(corePulse);
        StopEmission(largeRewardBurst);
        StopEmission(impactFlash);
        StopEmission(sparkleBurst);
        StopEmission(largeSparkleBurst);
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
        return IsAlive(crystalBurst)
            || IsAlive(corePulse)
            || IsAlive(largeRewardBurst)
            || IsAlive(impactFlash)
            || IsAlive(sparkleBurst)
            || IsAlive(largeSparkleBurst);
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
    /// 十字の光マテリアルの透明度を更新
    /// </summary>
    /// <param name="alpha">設定する透明度です。</param>
    private void SetCrossSparkColor(float alpha)
    {
        var hasMainMaterial = crossSparkRenderer != null && crossSparkRenderer.sharedMaterial != null;
        var hasAccentMaterial = crossSparkAccentRenderer != null && crossSparkAccentRenderer.sharedMaterial != null;
        if (!hasMainMaterial && !hasAccentMaterial)
        {
            return;
        }

        if (crossSparkPropertyBlock == null)
        {
            crossSparkPropertyBlock = new MaterialPropertyBlock();
        }
        var color = new Color(0.55f, 1f, 1f, alpha);
        crossSparkPropertyBlock.SetColor(BaseColorProperty, color);
        crossSparkPropertyBlock.SetColor(ColorProperty, color);

        if (hasMainMaterial)
        {
            crossSparkRenderer.SetPropertyBlock(crossSparkPropertyBlock);
        }

        if (hasAccentMaterial)
        {
            crossSparkAccentRenderer.SetPropertyBlock(crossSparkPropertyBlock);
        }
    }

    /// <summary>
    /// 管理している全粒子を消去
    /// </summary>
    private void StopParticles()
    {
        StopParticle(crystalBurst);
        StopParticle(corePulse);
        StopParticle(largeRewardBurst);
        StopParticle(impactFlash);
        StopParticle(sparkleBurst);
        StopParticle(largeSparkleBurst);
    }

    /// <summary>
    /// 指定した粒子を停止して残像を消去
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
