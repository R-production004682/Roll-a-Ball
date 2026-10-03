using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 球を不揃いの閉じた破片へ置き換え、その場で割れて欠片がこぼれる演出を再生する
/// </summary>
public sealed class PlayerFractureEffect : MonoBehaviour
{
    [SerializeField] private Material fractureMaterial;
    [SerializeField] private ParticleSystem crumbs;
    [SerializeField] private Transform crumbFloor;
    [SerializeField, Range(2, 12)] private int minimumPieces = 2;
    [SerializeField, Range(2, 12)] private int maximumPieces = 8;
    [SerializeField, Min(0.1f)] private float duration = 0.65f;
    [SerializeField, Min(0f), Tooltip("ぱっくり開いて欠片が落ちるまで暗転を待つ秒数")]
    private float visibleDuration = 0.52f;
    [SerializeField, Range(0.01f, 0.2f), Tooltip("球の直径に対する破片の開き幅")]
    private float openingDistance = 0.09f;
    [SerializeField, Range(0f, 0.4f)] private float settlingDistance = 0.2f;

    private sealed class Shard
    {
        public Transform transform;
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Vector3 center;
        public Vector3 direction;
        public Quaternion tilt;
        public float delay;
        public float settling;
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int CrackGlowId = Shader.PropertyToID("_CrackGlow");
    private readonly List<Shard> shards = new List<Shard>();
    private MaterialPropertyBlock propertyBlock;
    private Tween tween;
    private float phase;
    private float diameter;
    private float floorHeight;
    private int activeCount;
    private Color surfaceColor;

    /// <summary>
    /// 表示を隠して元のプレイヤーへ戻せる状態かを取得する
    /// </summary>
    public bool IsPlaying => gameObject.activeSelf && activeCount > 0;

    /// <summary>
    /// 暗転開始まで演出を見せる推奨秒数を取得する
    /// </summary>
    public float VisibleDuration => Mathf.Min(Mathf.Max(0f, visibleDuration), Mathf.Max(0.1f, duration));

    /// <summary>
    /// 元メッシュの姿勢と色を引き継ぎ、毎回違う個数と切断面で割れ始める
    /// </summary>
    /// <returns>表示用の断面付き破片を準備できた場合だけ true</returns>
    public bool Play(Mesh source, Transform sourceTransform, Color color, float groundHeight)
    {
        Stop();
        if (source == null || !source.isReadable || fractureMaterial == null || crumbs == null)
        {
            Debug.LogWarning("PlayerFractureEffect: 読み取り可能な凸 Mesh、断面 Material、欠片 ParticleSystem を設定してください", this);
            return false;
        }
        var low = Mathf.Clamp(minimumPieces, 2, 12);
        var high = Mathf.Clamp(maximumPieces, low, 12);
        var meshes = ConvexFractureMesh.Generate(source, Random.Range(low, high + 1), Random.Range(0, int.MaxValue));
        if (meshes.Count < 2)
        {
            foreach (var mesh in meshes)
            {
                Destroy(mesh);
            }
            Debug.LogWarning("PlayerFractureEffect: 凸 Mesh を断面付きの破片へ分割できませんでした", this);
            return false;
        }
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        transform.localScale = sourceTransform.lossyScale;
        diameter = source.bounds.size.magnitude / Mathf.Sqrt(3f);
        floorHeight = groundHeight;
        if (crumbFloor != null)
        {
            crumbFloor.SetPositionAndRotation(new Vector3(transform.position.x, groundHeight, transform.position.z), Quaternion.identity);
        }
        surfaceColor = color;
        propertyBlock ??= new MaterialPropertyBlock();
        activeCount = meshes.Count;
        EnsureShards(activeCount);
        for (var i = 0; i < activeCount; i++)
        {
            var shard = shards[i];
            var mesh = meshes[i];
            shard.center = mesh.bounds.center;
            var vertices = mesh.vertices;
            for (var j = 0; j < vertices.Length; j++)
            {
                vertices[j] -= shard.center;
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            shard.filter.sharedMesh = mesh;
            shard.direction = (shard.center - source.bounds.center).normalized;
            shard.tilt = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-8f, 8f), Random.Range(-12f, 12f));
            shard.delay = Random.Range(0f, 0.08f);
            shard.settling = Random.Range(0.55f, 1f);
            shard.transform.gameObject.SetActive(true);
        }
        phase = 0f;
        ApplyPose();
        crumbs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var shape = crumbs.shape;
        shape.radius = source.bounds.extents.magnitude * 0.42f;
        crumbs.Play(true);
        tween = DOTween.To(() => phase, value => phase = value, 1f, Mathf.Max(0.1f, duration))
            .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this).OnUpdate(ApplyPose);
        return true;
    }

    /// <summary>
    /// 使う破片スロットだけを追加し、Collider と Rigidbody を持たない表示を再利用する
    /// </summary>
    private void EnsureShards(int count)
    {
        while (shards.Count < count)
        {
            var item = new GameObject("FractureShard");
            item.transform.SetParent(transform, false);
            var filter = item.AddComponent<MeshFilter>();
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = fractureMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shards.Add(new Shard { transform = item.transform, filter = filter, renderer = renderer });
        }
    }

    /// <summary>
    /// 短い予兆から小さく開き、破片ごとの遅れで足元へ沈む姿勢と断面光を更新する
    /// </summary>
    private void ApplyPose()
    {
        propertyBlock.SetColor(BaseColorId, surfaceColor);
        propertyBlock.SetFloat(CrackGlowId, Mathf.Lerp(1.1f, 0.08f, Mathf.SmoothStep(0f, 1f, phase * 2f)));
        for (var i = 0; i < activeCount; i++)
        {
            var shard = shards[i];
            var open = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f + shard.delay, 0.48f, phase));
            var settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f + shard.delay, 1f, phase));
            shard.transform.localPosition = shard.center + shard.direction * (diameter * openingDistance * open);
            shard.transform.localRotation = Quaternion.Slerp(Quaternion.identity, shard.tilt, settle);
            shard.transform.position += Vector3.down * (diameter * settlingDistance * shard.settling * settle);
            // 床へ潜り込ませず、外へ飛ばす速度や物理用オブジェクトを追加しない
            var bottom = shard.renderer.bounds.min.y;
            if (bottom < floorHeight)
            {
                shard.transform.position += Vector3.up * (floorHeight - bottom);
            }
            shard.renderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>
    /// 再生と粒子を停止し、生成 Mesh を解放して表示スロットを隠す
    /// </summary>
    public void Stop()
    {
        tween?.Kill();
        tween = null;
        activeCount = 0;
        if (crumbs != null)
        {
            crumbs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        foreach (var shard in shards)
        {
            if (shard.filter.sharedMesh != null)
            {
                Destroy(shard.filter.sharedMesh);
                shard.filter.sharedMesh = null;
            }
            shard.transform.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 演出の無効化でも Tween と生成 Mesh を残さない
    /// </summary>
    private void OnDisable()
    {
        Stop();
    }
}
