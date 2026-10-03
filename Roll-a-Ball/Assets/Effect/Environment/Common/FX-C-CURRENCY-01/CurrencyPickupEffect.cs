using UnityEngine;

/// <summary>
/// コイン取得時に再生する、ローポリ風のクリスタルエフェクトです
/// </summary>
public sealed class CurrencyPickupEffect : MonoBehaviour
{
    private const float MinimumDuration = 0.1f;
    private const float CrossSparkAccentRotationDegrees = 45f;
    private const float CrossSparkInitialScale = 0.1f;
    private const float CrossSparkMaximumScale = 0.665f;
    private const float CrossSparkBurstEndProgress = 0.22f;
    private const float CrossSparkSettlingRatio = 0.35f;
    private const float CrossSparkAccentScaleRatio = 0.75f;
    private const float CrossSparkFadeStartProgress = 0.18f;
    private const float CrossSparkOpacity = 0.78f;

    [Header("参照（通常はPrefabの設定を維持）")]
    [Tooltip("通常・大報酬で共通のクリスタル粒子。粒子数・色・サイズ・寿命はこのParticleSystemで調整します。")]
    [SerializeField]
    private ParticleSystem crystalBurst;

    [Tooltip("大報酬時だけ追加するクリスタル粒子。通常報酬では非表示になります。")]
    [SerializeField]
    private ParticleSystem largeRewardBurst;

    [Tooltip("通常・大報酬で共通の中心光ParticleSystem。再生時に指定された閃光の正面を向きます。")]
    [SerializeField]
    private ParticleSystem corePulse;

    [Tooltip("大報酬時だけ再生する強い閃光ParticleSystem。通常報酬では非表示になります。")]
    [SerializeField]
    private ParticleSystem impactFlash;

    [Tooltip("通常・大報酬で共通のキラキラ粒子。量・寿命はこのParticleSystemで調整します。")]
    [SerializeField]
    private ParticleSystem sparkleBurst;

    [Tooltip("大報酬時だけ追加するキラキラ粒子。通常報酬では非表示になります。")]
    [SerializeField]
    private ParticleSystem largeSparkleBurst;

    [Tooltip("大報酬の十字光Transform。向き・サイズ・表示を演出側で制御します。")]
    [SerializeField]
    private Transform crossSpark;

    [Tooltip("十字光のRenderer。演出側から色と透明度を設定する対象です。")]
    [SerializeField]
    private MeshRenderer crossSparkRenderer;

    [Tooltip("大報酬の補助十字光Transform。主十字から45度ずらして表示します。")]
    [SerializeField]
    private Transform crossSparkAccent;

    [Tooltip("補助十字光のRenderer。演出側から色と透明度を設定する対象です。")]
    [SerializeField]
    private MeshRenderer crossSparkAccentRenderer;

    [Tooltip("大報酬時だけ点灯するLight。明るさはPulse Light Intensityで調整します。")]
    [SerializeField]
    private Light pulseLight;

    [Header("再生・終了")]
    [Tooltip("ONなら有効化時にIs Large Rewardの設定で自動再生。OFFならゲーム側からPlayを呼びます。")]
    [SerializeField]
    private bool playOnEnable;

    [Tooltip("ONなら演出と残留粒子の終了後にこのGameObjectを破棄。再利用・プール運用・繰り返し確認ではOFFにします。")]
    [SerializeField]
    private bool destroyOnComplete = true;

    [Header("演出の時間・明るさ")]
    [Tooltip("十字光・ライトなどの本体演出時間（秒、最低0.1）。大きいほどゆっくり消えます。終了後も粒子の自然消滅を待ちます。")]
    [SerializeField]
    private float duration = 1.1f;

    [Tooltip("大報酬時のライト初期強度。大きいほど明るくなります。通常報酬ではライトを点灯しません。")]
    [SerializeField]
    private float pulseLightIntensity = 3.5f;

    [Tooltip("ONなら追加粒子・十字光・ライトを含む大報酬版。引数なしPlayと自動再生の設定で、ゲーム側のPlay(bool)指定が優先されます。")]
    [SerializeField]
    private bool isLargeReward;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private float elapsedTime;
    private ParticleEffectPlaybackState playbackState;
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
        if (playbackState == ParticleEffectPlaybackState.Idle)
        {
            return;
        }

        if (playbackState == ParticleEffectPlaybackState.Playing)
        {
            elapsedTime += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(elapsedTime / Mathf.Max(MinimumDuration, duration));
            UpdateCrossSpark(normalizedTime);
            UpdatePulseLight(normalizedTime);

            if (elapsedTime >= Mathf.Max(MinimumDuration, duration))
            {
                BeginParticleFinish();
            }
        }

        if (playbackState == ParticleEffectPlaybackState.WaitingForParticles && !HasAliveParticles())
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
        Play(largeReward, transform.rotation);
    }

    /// <summary>
    /// 閃光を指定した正面方向へ向け、報酬サイズに応じて再生する
    /// </summary>
    /// <param name="largeReward">大きい報酬では追加の光と粒子を再生する</param>
    /// <param name="flashRotation">閃光の正面を示すワールド回転</param>
    public void Play(bool largeReward, Quaternion flashRotation)
    {
        isLargeReward = largeReward;
        elapsedTime = 0f;
        playbackState = ParticleEffectPlaybackState.Playing;

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
            corePulse.transform.rotation = flashRotation;
            corePulse.Play();
        }

        if (largeReward && largeRewardBurst != null)
        {
            largeRewardBurst.Play();
        }

        if (impactFlash != null)
        {
            impactFlash.transform.rotation = flashRotation;
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
            crossSpark.rotation = flashRotation;
            crossSpark.gameObject.SetActive(largeReward);
            if (largeReward)
            {
                crossSpark.localScale = Vector3.one * 0.1f;
            }
        }

        if (crossSparkAccent != null)
        {
            crossSparkAccent.rotation = flashRotation * Quaternion.Euler(0f, 0f, CrossSparkAccentRotationDegrees);
            crossSparkAccent.gameObject.SetActive(largeReward);
            if (largeReward)
            {
                crossSparkAccent.localScale = Vector3.one * 0.1f;
            }
        }

        SetCrossSparkColor(largeReward ? CrossSparkOpacity : 0f);

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
        playbackState = ParticleEffectPlaybackState.Idle;
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
            crossSpark.localScale = Vector3.one * CrossSparkInitialScale;
            crossSpark.gameObject.SetActive(false);
        }

        if (crossSparkAccent != null)
        {
            crossSparkAccent.localScale = Vector3.one * CrossSparkInitialScale;
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

        var burstProgress = Mathf.Clamp01(normalizedTime / CrossSparkBurstEndProgress);
        var burst = Mathf.SmoothStep(0f, 1f, burstProgress);
        var settle = 1f - Mathf.SmoothStep(CrossSparkBurstEndProgress, 1f, normalizedTime) * CrossSparkSettlingRatio;
        var scale = Mathf.Lerp(CrossSparkInitialScale, CrossSparkMaximumScale, burst) * settle;

        if (crossSpark != null)
        {
            crossSpark.localScale = Vector3.one * scale;
        }

        if (crossSparkAccent != null)
        {
            crossSparkAccent.localScale = Vector3.one * (scale * CrossSparkAccentScaleRatio);
        }

        var alpha = CrossSparkOpacity * (1f - Mathf.SmoothStep(CrossSparkFadeStartProgress, 1f, normalizedTime));
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
        playbackState = ParticleEffectPlaybackState.WaitingForParticles;
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
