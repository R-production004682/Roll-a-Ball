using UnityEngine;

/// <summary>
/// 移動時の地面に合わせて粉塵と一緒に散らす小さな表面粒子の種類を表す
/// </summary>
public enum MovementSurfaceDetail
{
    None,
    GrassBlade,
    Pebble
}

/// <summary>
/// 地面ごとの移動粉塵と表面粒子の色、量、大きさ、寿命を保持する
/// </summary>
[CreateAssetMenu(menuName = "Roll-a-Ball/Effects/Movement Surface")]
public sealed class MovementSurfaceProfile : ScriptableObject
{
    [SerializeField] private Color dustColor = new Color(0.62f, 0.67f, 0.43f, 0.6f);
    [SerializeField] private Color accentColor = new Color(0.84f, 0.8f, 0.57f, 0.5f);
    [SerializeField] private Vector2 particlesPerSecond = new Vector2(5f, 18f);
    [SerializeField] private Vector2 particleSize = new Vector2(0.1f, 0.18f);
    [SerializeField] private Vector2 lifetime = new Vector2(0.22f, 0.42f);
    [SerializeField, Min(0f)] private float lateralSpeed = 0.35f;
    [SerializeField, Min(0f)] private float liftSpeed = 0.25f;
    [SerializeField, Range(0f, 0.1f)] private float sparkleRatio = 0.075f;
    [SerializeField] private MovementSurfaceDetail surfaceDetail;
    [SerializeField] private Color detailColor = Color.white;
    [SerializeField] private Vector2 detailParticlesPerSecond = new Vector2(1f, 4f);
    [SerializeField] private Vector2 detailSize = new Vector2(0.6f, 1f);
    [SerializeField] private Vector2 detailLifetime = new Vector2(0.2f, 0.35f);
    [SerializeField, Min(0f)] private float detailLateralSpeed = 0.35f;
    [SerializeField, Min(0f)] private float detailLiftSpeed = 0.25f;
    [SerializeField, Min(0f)] private float detailGravity = 0.2f;

    public Color DustColor => dustColor;
    public Color AccentColor => accentColor;
    public Vector2 ParticlesPerSecond => particlesPerSecond;
    public Vector2 ParticleSize => particleSize;
    public Vector2 Lifetime => lifetime;
    public float LateralSpeed => lateralSpeed;
    public float LiftSpeed => liftSpeed;
    public float SparkleRatio => sparkleRatio;
    public MovementSurfaceDetail SurfaceDetail => surfaceDetail;
    public Color DetailColor => detailColor;
    public Vector2 DetailParticlesPerSecond => detailParticlesPerSecond;
    public Vector2 DetailSize => detailSize;
    public Vector2 DetailLifetime => detailLifetime;
    public float DetailLateralSpeed => detailLateralSpeed;
    public float DetailLiftSpeed => detailLiftSpeed;
    public float DetailGravity => detailGravity;
}
