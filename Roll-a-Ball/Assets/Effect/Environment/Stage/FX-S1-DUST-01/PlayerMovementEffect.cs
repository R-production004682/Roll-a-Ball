using UnityEngine;

/// <summary>
/// Playerの実移動と足元を読み取り、操作や物理状態を変更せず移動粉塵を制御する
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class PlayerMovementEffect : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("足元と移動速度を調べるPlayerのCollider（必須）。Player本体の物理Colliderを指定します。")]
    [SerializeField] private Collider playerCollider;
    [Tooltip("接地中に再生するGroundDustEffect（必須）。停止・落下・リスポーン時は放出を止めます。")]
    [SerializeField] private GroundDustEffect effect;
    [Header("接地と移動の判定")]
    [Tooltip("足元のRaycastで地面として調べるLayer。対象を絞ると不要な物体を地面と誤認しにくくなります。Triggerは対象外です。")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [Tooltip("Colliderの底から下へ地面を探す追加距離（ワールド座標）。大きいほど隙間を許容しますが、宙に浮いた状態も接地と判定しやすくなります。")]
    [SerializeField, Min(0f)] private float groundTolerance = 0.055f;
    [Tooltip("接地と認める面の上向き成分（0〜1）。大きいほど平らな床だけに限定し、小さいほど急な斜面でも粉塵を出します。")]
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormal = 0.55f;
    [Tooltip("1フレームの移動をテレポートと見なす距離（ワールド座標）。超えると粒子・履歴を消去します。高速移動で消える場合は大きくします。")]
    [SerializeField, Min(0.1f)] private float teleportDistance = 2f;
    [Tooltip("地面から離れる法線方向の許容速度（距離/秒）。大きいほど小さな跳ねでも接地扱いになり、小さいほど離れた直後に粉塵を止めます。")]
    [SerializeField, Min(0f)] private float separationSpeed = 0.2f;

    private readonly RaycastHit[] hits = new RaycastHit[16];
    private Vector3 previousPosition;
    private Collider previousGround;
    private MovementSurface currentSurface;
    private bool ready;

    public bool IsGrounded { get; private set; }
    public float Speed { get; private set; }
    public MovementSurfaceProfile Surface => currentSurface != null ? currentSurface.Profile : null;

    /// <summary>
    /// Playerとエフェクトの必須参照を検証する
    /// </summary>
    private void Awake()
    {
        ready = playerCollider != null && effect != null;
        if (!ready)
        {
            Debug.LogError("PlayerMovementEffectのPlayer ColliderとEffectを設定してください。", this);
        }
    }

    /// <summary>
    /// 再開時の位置を基準にし、前回の粒子を消す
    /// </summary>
    private void OnEnable()
    {
        ResetEffect();
    }

    /// <summary>
    /// Player移動後の位置差と最も近い足元の地面から再生状態を更新する
    /// </summary>
    private void LateUpdate()
    {
        SampleMovement(Time.deltaTime);
    }

    /// <summary>
    /// 経過時間と実際の接地状態をもとにエフェクトだけを更新する
    /// </summary>
    private void SampleMovement(float deltaTime)
    {
        if (!ready || playerCollider == null || effect == null)
        {
            return;
        }

        var position = playerCollider.transform.position;
        var displacement = position - previousPosition;
        previousPosition = position;
        if (!playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy
            || displacement.sqrMagnitude > teleportDistance * teleportDistance)
        {
            ResetEffect();
            return;
        }

        if (deltaTime <= 0f)
        {
            effect.Stop();
            return;
        }

        // 物理更新のない描画フレームを停止と誤判定しないよう、動的Bodyは物理速度を使う
        var body = playerCollider.attachedRigidbody;
        var velocity = body != null && !body.isKinematic ? body.linearVelocity : displacement / deltaTime;
        var bounds = playerCollider.bounds;
        var count = Physics.RaycastNonAlloc(bounds.center, Vector3.down, hits,
            bounds.extents.y + groundTolerance, groundLayers, QueryTriggerInteraction.Ignore);
        var nearestDistance = float.PositiveInfinity;
        var ground = default(RaycastHit);
        for (var i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.collider == playerCollider || hit.transform.IsChildOf(playerCollider.transform)
                || hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            ground = hit;
        }

        IsGrounded = ground.collider != null && ground.normal.y >= minimumGroundNormal
            && Vector3.Dot(velocity, ground.normal) <= separationSpeed;
        if (ground.collider != previousGround)
        {
            previousGround = ground.collider;
            currentSurface = previousGround != null ? previousGround.GetComponentInParent<MovementSurface>() : null;
        }

        Speed = Vector3.ProjectOnPlane(velocity, IsGrounded ? ground.normal : Vector3.up).magnitude;
        effect.Play(Surface, velocity, ground.point, ground.normal, IsGrounded, deltaTime);
    }

    /// <summary>
    /// リスポーンやリトライ直後に呼び出して、粒子・地面キャッシュ・移動履歴を消去する
    /// </summary>
    public void ResetEffect()
    {
        previousPosition = playerCollider != null ? playerCollider.transform.position : transform.position;
        previousGround = null;
        currentSurface = null;
        IsGrounded = false;
        Speed = 0f;
        if (effect != null)
        {
            effect.Stop(true);
        }
    }

    /// <summary>
    /// Playerまたはプレビューの無効化時に粉塵を消す
    /// </summary>
    private void OnDisable()
    {
        ResetEffect();
    }
}
