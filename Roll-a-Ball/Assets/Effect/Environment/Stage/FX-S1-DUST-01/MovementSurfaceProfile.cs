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
    [Header("粉塵の色・量・動き")]
    [Tooltip("粉塵の基本色と透明度（A）。Accent Colorとの間で各粒子の色をばらつかせます。")]
    [SerializeField] private Color dustColor = new Color(0.62f, 0.67f, 0.43f, 0.6f);
    [Tooltip("粉塵に混ぜるもう一方の色と透明度（A）。Dust Colorとの差を広げるほど色のばらつきが増えます。")]
    [SerializeField] private Color accentColor = new Color(0.84f, 0.8f, 0.57f, 0.5f);
    [Tooltip("粉塵の基準発生数（粒/秒）。Xは低速側、Yは高速側で、さらに速度に応じた量を乗算します。大きいほど密度が増えます。")]
    [SerializeField] private Vector2 particlesPerSecond = new Vector2(5f, 18f);
    [Tooltip("粉塵の基準サイズ（ワールド座標）。Xは低速側、Yは高速側で、各粒子に約0.8〜1.15倍のばらつきを加えます。")]
    [SerializeField] private Vector2 particleSize = new Vector2(0.1f, 0.18f);
    [Tooltip("粉塵の寿命の範囲（秒、X=最小、Y=最大）。大きいほど長く残ります。実際には0.05〜0.8秒に制限します。")]
    [SerializeField] private Vector2 lifetime = new Vector2(0.22f, 0.42f);
    [Tooltip("粉塵が進行方向の左右に散る最大速度（距離/秒）。大きいほど横へ広がり、0で横方向のばらつきがなくなります。")]
    [SerializeField, Min(0f)] private float lateralSpeed = 0.35f;
    [Tooltip("粉塵が接地面から浮く速度の基準（距離/秒）。大きいほど上へ舞い、各粒子に約0.6〜1倍のばらつきを加えます。")]
    [SerializeField, Min(0f)] private float liftSpeed = 0.25f;
    [Tooltip("粉塵1粒あたりに混ぜる光粒の割合（0〜0.1）。0.1で粉塵約10粒に光粒1粒、0なら光粒を出しません。")]
    [SerializeField, Range(0f, 0.1f)] private float sparkleRatio = 0.075f;
    [Header("草片・小石の調整")]
    [Tooltip("追加の表面粒子。None=なし、Grass Blade=草片、Pebble=小石。対応するParticleSystem・Meshの参照が必要です。")]
    [SerializeField] private MovementSurfaceDetail surfaceDetail;
    [Tooltip("草片・小石の色と透明度（A）。各粒子に約0.85〜1.1倍の明るさのばらつきを加えます。")]
    [SerializeField] private Color detailColor = Color.white;
    [Tooltip("草片・小石の基準発生数（粒/秒）。Xは低速側、Yは高速側で、さらに速度に応じた量を乗算します。大きいほど増えます。")]
    [SerializeField] private Vector2 detailParticlesPerSecond = new Vector2(1f, 4f);
    [Tooltip("草片・小石のメッシュサイズ倍率の範囲（X=最小、Y=最大）。大きいほど大きくなります。元のメッシュ寸法にも依存します。")]
    [SerializeField] private Vector2 detailSize = new Vector2(0.6f, 1f);
    [Tooltip("草片・小石の寿命の範囲（秒、X=最小、Y=最大、最低0.05）。大きいほど長く残しますが、地面へ戻る飛行時間で上限を制限します。")]
    [SerializeField] private Vector2 detailLifetime = new Vector2(0.2f, 0.35f);
    [Tooltip("草片・小石が左右に散る最大速度（距離/秒）。大きいほど横へ広がります。")]
    [SerializeField, Min(0f)] private float detailLateralSpeed = 0.35f;
    [Tooltip("草片・小石が浮く速度の基準（距離/秒）。大きいほど高く跳ね、各粒子に約0.85〜1.2倍のばらつきを加えます。")]
    [SerializeField, Min(0f)] private float detailLiftSpeed = 0.25f;
    [Tooltip("草片・小石に掛かるPhysics.gravityの倍率。大きいほど速く落ち、0なら重力なし。地面に戻る時間による寿命制限にも影響します。")]
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
