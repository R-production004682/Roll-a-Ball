using UnityEngine;

/// <summary>
/// 小さな蝶の群れの飛行とShaderの羽ばたき、落下する鱗粉を制御する
/// </summary>
public sealed class ButterflySwarmEffect : MonoBehaviour
{
    [Header("参照（4匹分の設定を維持）")]
    [Tooltip("蝶4匹分のTransform（各オブジェクトにMeshRendererが必要）。配列は4要素を維持し、表示匹数はButterfly Countで調整します。")]
    [SerializeField] private Transform[] butterflies;
    [Tooltip("蝶の現在位置から落とす鱗粉ParticleSystem。色・サイズ・寿命はSystem側、通常時の量はDust Rate Per Butterflyで調整します。")]
    [SerializeField] private ParticleSystem scaleDust;
    [Header("通常の飛行と鱗粉")]
    [Tooltip("表示する匹数（2〜4匹）。配列の先頭からこの数だけ表示するため、3なら4匹目を非表示にします。")]
    [SerializeField, Range(2, 4)] private int butterflyCount = 3;
    [Tooltip("通常飛行の広がり（ローカル座標）。大きいほど群れの移動と各蝶の周回が広がり、最終位置はRoaming RadiusとFlight Areaで制限します。")]
    [SerializeField, Min(0.1f)] private float flightRadius = 1.15f;
    [Tooltip("群れの基準高度（エフェクト原点からローカルY方向）。大きいほど高く飛びます。Playerとの高低差3以上では接近に反応しません。")]
    [SerializeField, Min(0f)] private float flightHeight = 1.4f;
    [Tooltip("通常飛行の周期を進める速度係数。大きいほど周回・上下動が速くなります。距離/秒の指定ではなく、羽ばたき速度は別です。")]
    [SerializeField, Range(0.1f, 2f)] private float flightSpeed = 0.55f;
    [Tooltip("通常時の羽ばたき回数の基準（回/秒、1〜8）。大きいほど速く、各蝶で少しずれます。逃走中は最大約1.7倍になります。")]
    [SerializeField, Range(1f, 8f)] private float flapFrequency = 3.8f;
    [Tooltip("通常時に蝶1匹から出す鱗粉数（粒/秒、0〜6）。大きいほど増えます。逃走中は最大2倍、驚いた瞬間の追加2粒は0でも出ます。")]
    [SerializeField, Range(0f, 6f)] private float dustRatePerButterfly = 3f;
    [Header("Playerへの反応と帰還")]
    [Tooltip("接近に反応するPlayerのTransform。未指定ならPlayerタグを毎秒探索します。")]
    [SerializeField] private Transform player;
    [Tooltip("蝶からPlayerまでの逃走開始距離（ワールド水平距離）。大きいほど遠くから逃げます。高低差3以上では反応しません。")]
    [SerializeField, Min(0.1f)] private float scareDistance = 2.5f;
    [Tooltip("帰還を許可する安全距離（ワールド水平距離）。蝶と元の群れ位置の両方から離れる必要があります。Scare Distance＋0.2以上へ補正します。")]
    [SerializeField, Min(0.1f)] private float clearDistance = 3.5f;
    [Tooltip("Playerと反対方向へ退避する距離の基準（ワールド座標）。大きいほど遠くへ逃げます。Roaming Radius・Flight Areaで制限します。")]
    [SerializeField, Min(0.1f)] private float escapeDistance = 2.3f;
    [Tooltip("逃走中に追加する高度の基準（ワールド座標）。大きいほど上へ逃げ、各蝶で高さを少しずらします。")]
    [SerializeField, Min(0f)] private float escapeHeight = 0.65f;
    [Tooltip("安全距離の外で連続して待つ時間（秒）。大きいほど戻るまでが長く、Playerが再接近すると待ち時間をリセットします。逃走開始後は最低2.2秒待ちます。")]
    [SerializeField, Min(0f)] private float returnDelay = 3f;
    [Tooltip("元のエフェクト位置から移動できる最大半径（ワールド水平距離）。Escape Distance以上へ補正し、Flight Areaがあればその範囲も適用します。")]
    [SerializeField, Min(0.1f)] private float roamingRadius = 3.5f;
    [Tooltip("任意の飛行範囲BoxCollider。ColliderのローカルXZ内に位置を制限し、Yの高さ・Colliderの有効状態やサイズ自体は変更しません。")]
    [SerializeField]
    private BoxCollider flightArea;

    private static readonly int FlutterTime = Shader.PropertyToID("_FlutterTime");
    private MeshRenderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private float elapsed;
    private Vector3[] flightOffsets;
    private Vector3[] escapeTargets;
    private Vector3[] offsetVelocities;
    private float[] flutterTimes;
    private float[] dustAccumulators;
    private bool[] hasReacted;
    private FlightState flightState;
    private float escapeElapsed;
    private float safeElapsed;
    private float playerSearchElapsed;
    private Vector3 lastThreatPosition;
    private bool isFlying;
    private bool initialized;

    private enum FlightState
    {
        Calm,
        Startled,
        Fleeing,
        Settled,
        Returning
    }

    /// <summary>
    /// Prefabの参照を検証し描画用キャッシュを初期化して再生する
    /// </summary>
    private void OnEnable()
    {
        initialized = false;
        if (butterflies == null || butterflies.Length != 4 || scaleDust == null)
        {
            Debug.LogError("蝶エフェクトには4匹のTransformと鱗粉ParticleSystemを設定してください。", this);
            enabled = false;
            return;
        }

        renderers ??= new MeshRenderer[butterflies.Length];
        propertyBlock ??= new MaterialPropertyBlock();
        flightOffsets ??= new Vector3[butterflies.Length];
        escapeTargets ??= new Vector3[butterflies.Length];
        offsetVelocities ??= new Vector3[butterflies.Length];
        flutterTimes ??= new float[butterflies.Length];
        dustAccumulators ??= new float[butterflies.Length];
        hasReacted ??= new bool[butterflies.Length];
        for (var i = 0; i < butterflies.Length; i++)
        {
            if (butterflies[i] == null || !butterflies[i].TryGetComponent(out renderers[i]))
            {
                Debug.LogError("蝶のTransformにはMeshRendererが必要です。", this);
                enabled = false;
                return;
            }
        }

        initialized = true;
        Play();
    }

    /// <summary>
    /// 再利用時に蝶と残留粒子を停止する
    /// </summary>
    private void OnDisable()
    {
        Stop(true);
    }

    /// <summary>
    /// Inspectorの飛行範囲と匹数を有効な範囲へ収める
    /// </summary>
    private void OnValidate()
    {
        butterflyCount = Mathf.Clamp(butterflyCount, 2, 4);
        flightRadius = Mathf.Max(0.1f, flightRadius);
        flightHeight = Mathf.Max(0f, flightHeight);
        scareDistance = Mathf.Max(0.1f, scareDistance);
        clearDistance = Mathf.Max(scareDistance + 0.2f, clearDistance);
        escapeDistance = Mathf.Max(0.1f, escapeDistance);
        escapeHeight = Mathf.Max(0f, escapeHeight);
        returnDelay = Mathf.Max(0f, returnDelay);
        roamingRadius = Mathf.Max(escapeDistance, roamingRadius);
    }

    /// <summary>
    /// 時間に沿って蝶の位置と羽を更新し移動先から鱗粉を落とす
    /// </summary>
    private void Update()
    {
        AdvanceFlight(Time.deltaTime);
    }

    /// <summary>
    /// ポーズと同期する時間で接近反応と飛行を進める
    /// </summary>
    private void AdvanceFlight(float deltaTime)
    {
        if (!isFlying || deltaTime <= 0f)
        {
            return;
        }

        elapsed += deltaTime;
        ResolvePlayer(deltaTime);
        UpdateReaction(deltaTime);
        UpdateButterflies(deltaTime);
    }

    /// <summary>
    /// Playerの参照をキャッシュし未出現や再生成時だけ毎秒探索する
    /// </summary>
    private void ResolvePlayer(float deltaTime)
    {
        if (player != null)
        {
            return;
        }

        playerSearchElapsed -= deltaTime;
        if (playerSearchElapsed > 0f)
        {
            return;
        }

        playerSearchElapsed = 1f;
        var target = GameObject.FindGameObjectWithTag("Player");
        if (target != null)
        {
            player = target.transform;
        }
    }

    /// <summary>
    /// 水平距離と安全待ち時間で驚き、逃走、滞空、帰還を切り替える
    /// </summary>
    private void UpdateReaction(float deltaTime)
    {
        escapeElapsed += deltaTime;
        var nearButterflies = IsPlayerNearButterflies(scareDistance);
        var threatMoved = player != null
            && HorizontalDistanceSquared(player.position, lastThreatPosition) > 0.1225f;
        if (nearButterflies && (flightState == FlightState.Calm || flightState == FlightState.Returning
            || (escapeElapsed >= 1.2f && threatMoved)))
        {
            BeginEscape();
        }

        if (flightState == FlightState.Startled && escapeElapsed >= 0.35f)
        {
            flightState = FlightState.Fleeing;
        }
        else if (flightState == FlightState.Fleeing && escapeElapsed >= 2.2f)
        {
            flightState = FlightState.Settled;
        }

        if (flightState == FlightState.Calm || flightState == FlightState.Returning)
        {
            return;
        }

        var home = transform.TransformPoint(Vector3.up * flightHeight);
        var unsafeHome = player != null && player.gameObject.activeInHierarchy
            && Mathf.Abs(player.position.y - home.y) < 3f
            && HorizontalDistanceSquared(player.position, home) < clearDistance * clearDistance;
        safeElapsed = unsafeHome || IsPlayerNearButterflies(clearDistance) ? 0f : safeElapsed + deltaTime;
        if (safeElapsed >= returnDelay && escapeElapsed >= 2.2f)
        {
            flightState = FlightState.Returning;
        }
    }

    /// <summary>
    /// 最も近い蝶への接近を調べ、別の高さのPlayerには反応しない
    /// </summary>
    private bool IsPlayerNearButterflies(float distance)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            return false;
        }

        for (var i = 0; i < butterflyCount; i++)
        {
            var position = butterflies[i].position;
            if (Mathf.Abs(player.position.y - position.y) < 3f
                && HorizontalDistanceSquared(player.position, position) < distance * distance)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 高さを除いた距離の二乗を返す
    /// </summary>
    private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
    {
        var difference = a - b;
        return difference.x * difference.x + difference.z * difference.z;
    }

    /// <summary>
    /// Playerから離れる方向へ蝶ごとの角度と高さをずらした退避先を決める
    /// </summary>
    private void BeginEscape()
    {
        var center = Vector3.zero;
        for (var i = 0; i < butterflyCount; i++)
        {
            center += butterflies[i].position;
        }

        center /= butterflyCount;
        var away = center - player.position;
        away.y = 0f;
        away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;
        lastThreatPosition = player.position;
        escapeElapsed = 0f;
        safeElapsed = 0f;
        flightState = FlightState.Startled;
        for (var i = 0; i < butterflies.Length; i++)
        {
            var angle = (i - (butterflyCount - 1) * 0.5f) * 12f;
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * away;
            var offset = transform.TransformVector(flightOffsets[i]);
            offset.y = 0f;
            offset = Vector3.ClampMagnitude(offset + direction * escapeDistance, escapeDistance * 1.2f);
            offset.y = escapeHeight + i * 0.07f;
            escapeTargets[i] = transform.InverseTransformVector(offset);
            hasReacted[i] = false;
        }
    }

    /// <summary>
    /// 群れの中心を共有しつつ各蝶の周期と向きをずらす
    /// </summary>
    private void UpdateButterflies(float deltaTime)
    {
        var time = elapsed * flightSpeed;
        var center = new Vector3(Mathf.Sin(time) * flightRadius * 0.6f, flightHeight,
            Mathf.Sin(time * 0.73f) * flightRadius * 0.5f);
        var allReturned = true;
        for (var i = 0; i < butterflies.Length; i++)
        {
            var active = i < butterflyCount;
            butterflies[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            var phase = i * 2.39996f;
            var delay = 0.12f + i * 0.07f;
            var reacting = flightState != FlightState.Calm && flightState != FlightState.Returning;
            var reactionStarted = reacting && escapeElapsed >= delay;
            var firstReaction = reactionStarted && !hasReacted[i];
            if (reactionStarted)
            {
                hasReacted[i] = true;
            }

            var target = reacting ? escapeTargets[i] : Vector3.zero;
            if (!reacting || reactionStarted)
            {
                flightOffsets[i] = Vector3.SmoothDamp(flightOffsets[i], target, ref offsetVelocities[i],
                    reacting ? 0.6f : 1.1f, Mathf.Infinity, deltaTime);
            }

            allReturned &= flightOffsets[i].sqrMagnitude < 0.0004f;
            var alertness = reactionStarted ? 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(1.2f, 2.2f, escapeElapsed)) : 0f;
            var orbit = time * 1.2f + phase + 0.14f * Mathf.Sin(time * 0.8f + phase);
            var orbitSpeed = 1.2f + 0.112f * Mathf.Cos(time * 0.8f + phase);
            var radius = flightRadius * (0.5f + i * 0.04f);
            var position = center + flightOffsets[i] + new Vector3(Mathf.Cos(orbit) * radius,
                Mathf.Sin(time * 2.3f + phase) * 0.22f + Mathf.Sin(elapsed * 4f + phase) * 0.055f,
                Mathf.Sin(orbit) * radius);
            var direction = new Vector3(Mathf.Cos(time) * flightRadius * 0.6f
                - Mathf.Sin(orbit) * radius * orbitSpeed, 0f,
                Mathf.Cos(time * 0.73f) * flightRadius * 0.365f
                + Mathf.Cos(orbit) * radius * orbitSpeed);
            position = ConstrainPosition(position);
            if (deltaTime > 0f)
            {
                var movement = position - butterflies[i].localPosition;
                if (movement.sqrMagnitude > 0.000001f)
                {
                    direction = movement;
                }
            }

            butterflies[i].localPosition = position;
            var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up)
                * Quaternion.Euler(8f * Mathf.Sin(orbit), 0f, 12f * Mathf.Sin(time * 1.7f + phase));
            butterflies[i].localRotation = deltaTime > 0f
                ? Quaternion.Slerp(butterflies[i].localRotation, rotation, 1f - Mathf.Exp(-6f * deltaTime))
                : rotation;
            flutterTimes[i] += deltaTime * flapFrequency * (1f + i * 0.06f) * (1f + 0.7f * alertness);
            propertyBlock.SetFloat(FlutterTime, flutterTimes[i] + phase);
            renderers[i].SetPropertyBlock(propertyBlock);
            dustAccumulators[i] += deltaTime * dustRatePerButterfly * (1f + alertness);
            var dustCount = Mathf.FloorToInt(dustAccumulators[i]);
            dustAccumulators[i] -= dustCount;
            dustCount += firstReaction ? 2 : 0;
            if (dustCount > 0)
            {
                var emission = new ParticleSystem.EmitParams
                {
                    position = butterflies[i].position,
                    velocity = new Vector3(Mathf.Sin(orbit) * 0.07f, -0.1f, Mathf.Cos(orbit) * 0.07f)
                };
                scaleDust.Emit(emission, dustCount);
            }
        }

        if (flightState == FlightState.Returning && allReturned)
        {
            flightState = FlightState.Calm;
        }
    }

    /// <summary>
    /// 元の配置位置からの半径と任意の床範囲で飛行位置を制限する
    /// </summary>
    private Vector3 ConstrainPosition(Vector3 localPosition)
    {
        var position = transform.TransformPoint(localPosition);
        var home = transform.position;
        var horizontal = position - home;
        horizontal.y = 0f;
        horizontal = Vector3.ClampMagnitude(horizontal, roamingRadius);
        position.x = home.x + horizontal.x;
        position.z = home.z + horizontal.z;
        if (flightArea != null)
        {
            var areaPosition = flightArea.transform.InverseTransformPoint(position) - flightArea.center;
            var halfSize = flightArea.size * 0.5f;
            areaPosition.x = Mathf.Clamp(areaPosition.x, -halfSize.x, halfSize.x);
            areaPosition.z = Mathf.Clamp(areaPosition.z, -halfSize.z, halfSize.z);
            position = flightArea.transform.TransformPoint(areaPosition + flightArea.center);
        }

        return transform.InverseTransformPoint(position);
    }

    /// <summary>
    /// 蝶と鱗粉を初期状態から再生する
    /// </summary>
    public void Play()
    {
        if (!initialized || !isActiveAndEnabled)
        {
            return;
        }

        elapsed = 0f;
        escapeElapsed = 0f;
        safeElapsed = 0f;
        playerSearchElapsed = 0f;
        flightState = FlightState.Calm;
        for (var i = 0; i < butterflies.Length; i++)
        {
            flightOffsets[i] = Vector3.zero;
            escapeTargets[i] = Vector3.zero;
            offsetVelocities[i] = Vector3.zero;
            flutterTimes[i] = 0f;
            dustAccumulators[i] = 0f;
            hasReacted[i] = false;
        }

        isFlying = true;
        ResolvePlayer(0f);
        scaleDust.Clear(true);
        scaleDust.Play(true);
        UpdateButterflies(0);
    }

    /// <summary>
    /// 蝶と鱗粉の発生を止め、指定時は残留粒子も消す
    /// </summary>
    public void Stop(bool clearParticles = false)
    {
        isFlying = false;
        if (scaleDust != null)
        {
            scaleDust.Stop(true, clearParticles ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
        }

        if (butterflies == null)
        {
            return;
        }

        foreach (var butterfly in butterflies)
        {
            if (butterfly != null)
            {
                butterfly.gameObject.SetActive(false);
            }
        }
    }
}
