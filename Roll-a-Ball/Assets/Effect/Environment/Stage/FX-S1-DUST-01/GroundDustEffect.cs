using UnityEngine;

/// <summary>
/// 接地点と地面設定を受け取り、ワールド空間に短寿命の微細な粉塵を残す
/// </summary>
[DisallowMultipleComponent]
public sealed class GroundDustEffect : MonoBehaviour
{
    private const int ParticleRandomSeed = 13648;
    private const int MaximumDustParticlesPerUpdate = 4;
    private const int MaximumDetailParticlesPerUpdate = 8;
    private const float MaximumEmissionDeltaTime = 0.1f;
    private const float MinimumFullEffectSpeedGap = 0.01f;

    [Header("参照（通常はPrefabの設定を維持）")]
    [Tooltip("粉塵ParticleSystem（必須）。発生位置・色・量・寿命は地面Profileと速度から制御します。")]
    [SerializeField] private ParticleSystem dust;
    [Tooltip("少量の光粒ParticleSystem（必須）。割合は地面ProfileのSparkle Ratioで調整します。")]
    [SerializeField] private ParticleSystem sparkles;
    [Tooltip("草片用ParticleSystem。地面ProfileのSurface DetailがGrass Bladeの場合に使用します。")]
    [SerializeField] private ParticleSystem surfaceDetails;
    [Tooltip("小石用ParticleSystem。地面ProfileのSurface DetailがPebbleの場合に使用します。")]
    [SerializeField] private ParticleSystem pebbleParticles;
    [Tooltip("草片の形状Mesh。有効な参照は初期化時にSurface Detailsへ設定します。サイズはProfileで調整します。")]
    [SerializeField] private Mesh grassBladeMesh;
    [Tooltip("小石の形状Mesh。有効な参照は初期化時にPebble Particlesへ設定します。サイズはProfileで調整します。")]
    [SerializeField] private Mesh pebbleMesh;
    [Header("発生速度と位置")]
    [Tooltip("粉塵が出始める接地面方向の移動速度（距離/秒）。大きいほど低速時に出にくくなり、未満では停止します。")]
    [SerializeField, Min(0.01f)] private float minimumSpeed = 0.35f;
    [Tooltip("発生量が最大になる移動速度（距離/秒）。大きいほど最大量に達するまで速く動く必要があります。Minimum Speed＋0.01以上として扱います。")]
    [SerializeField, Min(0.02f)] private float fullEffectSpeed = 5f;
    [Tooltip("接地点から面の法線方向へ浮かせる距離（ワールド座標）。大きいほど地面から離れて出ます。草片・小石の寿命制限にも使います。")]
    [SerializeField, Min(0f)] private float contactOffset = 0.045f;
    [Tooltip("接地点から移動方向の後ろへずらす距離（ワールド座標）。大きいほどプレイヤーの後方に粒子が残ります。")]
    [SerializeField, Min(0f)] private float trailingOffset = 0.4f;

    private readonly System.Random random = new System.Random(ParticleRandomSeed);
    private float dustRemainder;
    private float sparkleRemainder;
    private float detailRemainder;
    private bool ready;
    private MovementSurfaceDetail previousDetail;

    public bool IsEmitting { get; private set; }
    public int ParticleCount => (dust != null ? dust.particleCount : 0)
        + (sparkles != null ? sparkles.particleCount : 0)
        + (surfaceDetails != null ? surfaceDetails.particleCount : 0)
        + (pebbleParticles != null ? pebbleParticles.particleCount : 0);

    /// <summary>
    /// 必須参照を確認し、自動放出を持たないPrefabの初期状態を設定する
    /// </summary>
    private void Awake()
    {
        ready = dust != null && sparkles != null;
        if (surfaceDetails != null && grassBladeMesh != null)
        {
            surfaceDetails.GetComponent<ParticleSystemRenderer>().mesh = grassBladeMesh;
        }

        if (pebbleParticles != null && pebbleMesh != null)
        {
            pebbleParticles.GetComponent<ParticleSystemRenderer>().mesh = pebbleMesh;
        }
        if (!ready)
        {
            Debug.LogError("GroundDustEffectのDustとSparklesを設定してください。", this);
        }
    }

    /// <summary>
    /// 再利用時に前回の粉塵と放出端数を消去する
    /// </summary>
    private void OnEnable()
    {
        Stop(true);
    }

    /// <summary>
    /// 接地中の移動状態から粉塵を放出するため、毎フレーム呼び出す
    /// </summary>
    /// <param name="surface">地面設定。未設定の地面では放出を停止する</param>
    /// <param name="velocity">ワールド空間の移動速度</param>
    /// <param name="point">地面上の接地点</param>
    /// <param name="normal">地面の法線</param>
    /// <param name="grounded">ジャンプ・落下中はfalseを渡す</param>
    /// <param name="deltaTime">ゲーム時間の経過秒数</param>
    public void Play(MovementSurfaceProfile surface, Vector3 velocity, Vector3 point,
        Vector3 normal, bool grounded, float deltaTime)
    {
        if (!ready || !isActiveAndEnabled || surface == null || !grounded
            || deltaTime <= 0f || normal.sqrMagnitude < 0.5f)
        {
            Stop();
            return;
        }

        normal.Normalize();
        var tangentVelocity = Vector3.ProjectOnPlane(velocity, normal);
        var speed = tangentVelocity.magnitude;
        if (speed < minimumSpeed)
        {
            Stop();
            return;
        }

        var amount = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(minimumSpeed, Mathf.Max(minimumSpeed + MinimumFullEffectSpeedGap, fullEffectSpeed), speed));
        var rate = amount * Mathf.Max(0f, Mathf.Lerp(surface.ParticlesPerSecond.x, surface.ParticlesPerSecond.y, amount));

        if (!IsEmitting)
        {
            dustRemainder = 0f;
            detailRemainder = 0f;
            dust.Play(false);
            sparkles.Play(false);
            if (surfaceDetails != null)
            {
                surfaceDetails.Play(false);
            }

            if (pebbleParticles != null)
            {
                pebbleParticles.Play(false);
            }
        }

        IsEmitting = true;
        dustRemainder += rate * Mathf.Min(deltaTime, MaximumEmissionDeltaTime);
        var count = Mathf.Min(MaximumDustParticlesPerUpdate, Mathf.FloorToInt(dustRemainder));
        dustRemainder -= Mathf.Floor(dustRemainder);
        var direction = tangentVelocity / speed;
        var side = Vector3.Cross(normal, direction);
        var origin = point + normal * contactOffset - direction * trailingOffset;
        for (var i = 0; i < count; i++)
        {
            var particle = new ParticleSystem.EmitParams
            {
                position = origin + side * Mathf.Lerp(-0.22f, 0.22f, NextValue()),
                velocity = -direction * Mathf.Lerp(0.12f, 0.42f, amount)
                    + side * Mathf.Lerp(-surface.LateralSpeed, surface.LateralSpeed, NextValue())
                    + normal * surface.LiftSpeed * Mathf.Lerp(0.6f, 1f, NextValue()),
                startSize = Mathf.Max(0.01f, Mathf.Lerp(surface.ParticleSize.x, surface.ParticleSize.y, amount))
                    * Mathf.Lerp(0.8f, 1.15f, NextValue()),
                startLifetime = Mathf.Clamp(Mathf.Lerp(surface.Lifetime.x, surface.Lifetime.y, NextValue()), 0.05f, 0.8f),
                startColor = Color.Lerp(surface.DustColor, surface.AccentColor, NextValue()),
                rotation = NextValue() * 360f
            };
            dust.Emit(particle, 1);
            sparkleRemainder += Mathf.Clamp(surface.SparkleRatio, 0f, 0.1f);
            if (sparkleRemainder >= 1f)
            {
                sparkleRemainder -= 1f;
                particle.startSize = 0.055f;
                particle.startLifetime = 0.28f;
                particle.startColor = new Color(0.4f, 0.87f, 0.95f, 0.45f);
                particle.velocity = normal * 0.4f - direction * 0.12f;
                sparkles.Emit(particle, 1);
            }
        }

        EmitSurfaceDetails(surface, origin, direction, side, normal, amount, deltaTime);
    }

    /// <summary>
    /// 放出を停止し、必要な場合だけ既存粒子も即座に消去する
    /// </summary>
    /// <param name="clear">リスポーン・リトライ・プール返却時はtrueを渡す</param>
    public void Stop(bool clear = false)
    {
        var wasEmitting = IsEmitting;
        IsEmitting = false;
        dustRemainder = 0f;
        sparkleRemainder = 0f;
        detailRemainder = 0f;
        if (!clear && !wasEmitting)
        {
            return;
        }

        var behavior = clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
        if (dust != null)
        {
            dust.Stop(false, behavior);
        }

        if (sparkles != null)
        {
            sparkles.Stop(false, behavior);
        }

        if (surfaceDetails != null)
        {
            surfaceDetails.Stop(false, behavior);
        }

        if (pebbleParticles != null)
        {
            pebbleParticles.Stop(false, behavior);
        }
    }

    /// <summary>
    /// 地面設定に対応する短い草片または小石を速度に合わせて接地点から散らす
    /// </summary>
    private void EmitSurfaceDetails(MovementSurfaceProfile surface, Vector3 origin, Vector3 direction,
        Vector3 side, Vector3 normal, float speedAmount, float deltaTime)
    {
        var isPebble = surface.SurfaceDetail == MovementSurfaceDetail.Pebble;
        var target = isPebble ? pebbleParticles : surfaceDetails;
        if (target == null || surface.SurfaceDetail == MovementSurfaceDetail.None)
        {
            detailRemainder = 0f;
            return;
        }

        // 草と小石を別Systemに保持し、境界を越えても既存の草粒子を小石へ変形させない
        if (previousDetail != surface.SurfaceDetail)
        {
            detailRemainder = 0f;
            previousDetail = surface.SurfaceDetail;
        }

        var detailMain = target.main;
        if (!Mathf.Approximately(detailMain.gravityModifier.constant, surface.DetailGravity))
        {
            detailMain.gravityModifier = surface.DetailGravity;
        }

        var rate = speedAmount * Mathf.Max(0f, Mathf.Lerp(surface.DetailParticlesPerSecond.x,
            surface.DetailParticlesPerSecond.y, speedAmount));
        detailRemainder += rate * Mathf.Min(deltaTime, MaximumEmissionDeltaTime);
        var count = Mathf.Min(MaximumDetailParticlesPerUpdate, Mathf.FloorToInt(detailRemainder));
        // 低FPSでも通常の放出量を保ち、停止後の追い出しが起きないよう整数の余剰は捨てる
        detailRemainder -= Mathf.Floor(detailRemainder);
        var lift = Mathf.Max(0f, surface.DetailLiftSpeed);
        var lateral = Mathf.Max(0f, surface.DetailLateralSpeed);

        for (var i = 0; i < count; i++)
        {
            var lifetime = Mathf.Max(0.05f, Mathf.Lerp(surface.DetailLifetime.x,
                surface.DetailLifetime.y, NextValue()));
            var upwardSpeed = lift * Mathf.Lerp(0.85f, 1.2f, NextValue());
            var gravity = Mathf.Max(0f, -Vector3.Dot(Physics.gravity, normal) * surface.DetailGravity);
            if (gravity > 0.01f)
            {
                // 地面を突き抜ける前に消えるよう、接地点の平面へ戻る飛行時間で寿命を制限する
                var flightTime = (upwardSpeed + Mathf.Sqrt(upwardSpeed * upwardSpeed
                    + 2f * gravity * contactOffset)) / gravity;
                lifetime = Mathf.Min(lifetime, flightTime);
            }

            var particle = new ParticleSystem.EmitParams
            {
                position = origin + side * Mathf.Lerp(-0.16f, 0.16f, NextValue()),
                velocity = -direction * Mathf.Lerp(0.12f, isPebble ? 0.48f : 0.65f, speedAmount)
                    + side * Mathf.Lerp(-lateral, lateral, NextValue())
                    + normal * upwardSpeed,
                startSize = Mathf.Max(0.01f, Mathf.Lerp(surface.DetailSize.x,
                    surface.DetailSize.y, NextValue())),
                startLifetime = lifetime,
                startColor = surface.DetailColor * Mathf.Lerp(0.85f, 1.1f, NextValue()),
                // EmitParamsは度単位。Moduleのラジアン値と混同しない
                rotation3D = new Vector3(NextValue() * 140f - 70f, NextValue() * 360f,
                    NextValue() * 360f)
            };
            target.Emit(particle, 1);
        }
    }

    /// <summary>
    /// Gameplay側の乱数列を変更せず粒子ごとのばらつきを生成する
    /// </summary>
    private float NextValue()
    {
        return (float)random.NextDouble();
    }

    /// <summary>
    /// 無効化時に残留粒子を消去する
    /// </summary>
    private void OnDisable()
    {
        Stop(true);
    }
}
