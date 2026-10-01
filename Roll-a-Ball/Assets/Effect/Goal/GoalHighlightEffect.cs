using UnityEngine;

/// <summary>
/// ゴール地点で四角形の光が下から上へ流れ続ける常時演出です
/// </summary>
public sealed class GoalHighlightEffect : MonoBehaviour
{
    [Header("参照（通常はPrefabの設定を維持）")]
    [Tooltip("常時上昇させる四角形のRenderer配列。配列順がSquare ColorsとStaggerに対応します。参照変更は再生前に行います。")]
    [SerializeField]
    private MeshRenderer[] squareRenderers;

    [Tooltip("ゴール周辺を明滅させるLight。明るさはGlow Light Intensityで制御します。未設定ならライト演出を省略します。")]
    [SerializeField]
    private Light glowLight;

    [Header("色と再生")]
    [Tooltip("四角形ごとの色と透明度（A）。Square Renderersの配列順に対応し、不足分はシアンになります。")]
    [SerializeField]
    private Color[] squareColors;

    [Tooltip("ONならGameObject・コンポーネントの有効化時に自動再生。OFFならゲーム側からPlayを呼んで開始します。")]
    [SerializeField]
    private bool playOnEnable = true;

    [Header("上昇と揺れ")]
    [Tooltip("四角形が上昇して一周する時間（秒、最低0.1）。大きいほど上昇・明滅がゆっくりになります。")]
    [SerializeField]
    private float loopDuration = 2.4f;

    [Tooltip("初期位置を中心に上昇する全高（ローカル座標）。下側−半分から上側＋半分まで移動します。")]
    [SerializeField]
    private float loopHeight = 2.2f;

    [Tooltip("四角形同士のループ位相のずれ（秒）。0なら同期し、大きいほど隣の四角形とのタイミングが離れます。")]
    [SerializeField]
    private float stagger = 0.36f;

    [Tooltip("四角形の揺れ角の強さ。大きいほど傾きが増えます。名前はSpeedですが回転速度ではなく、主な角度はこの値×0.08度です。")]
    [SerializeField]
    private float rotationSpeed = 45f;

    [Tooltip("ライトの基準強度。大きいほど明るくなり、再生中はこの値の約64〜100%で明滅します。")]
    [SerializeField]
    private float glowLightIntensity = 1.6f;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3[] basePositions;
    private Vector3[] baseScales;
    private Quaternion[] baseRotations;
    private float elapsedTime;
    private bool isPlaying;

    /// <summary>
    /// 子オブジェクトの初期Transformを保存し、配置時の再生設定を適用
    /// </summary>
    private void Awake()
    {
        CacheBaseTransforms();
    }

    /// <summary>
    /// Prefabを有効化したときに常時演出を開始
    /// </summary>
    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    /// <summary>
    /// 四角形の上昇、透明度、発光を毎フレーム更新
    /// </summary>
    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateSquares();
        UpdateGlowLight();
    }

    /// <summary>
    /// ゴール演出を最初から再生
    /// </summary>
    public void Play()
    {
        elapsedTime = 0f;
        isPlaying = true;
        SetRenderersEnabled(true);

        if (glowLight != null)
        {
            glowLight.enabled = true;
        }

        UpdateSquares();
        UpdateGlowLight();
    }

    /// <summary>
    /// ゴール演出を停止して非表示にする
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        SetRenderersEnabled(false);
        ResetSquareTransforms();

        if (glowLight != null)
        {
            glowLight.intensity = 0f;
            glowLight.enabled = false;
        }
    }

    /// <summary>
    /// 四角形の初期位置と初期サイズを保存
    /// </summary>
    private void CacheBaseTransforms()
    {
        if (squareRenderers == null)
        {
            basePositions = new Vector3[0];
            baseScales = new Vector3[0];
            baseRotations = new Quaternion[0];
            return;
        }

        basePositions = new Vector3[squareRenderers.Length];
        baseScales = new Vector3[squareRenderers.Length];
        baseRotations = new Quaternion[squareRenderers.Length];
        for (var i = 0; i < squareRenderers.Length; i++)
        {
            var renderer = squareRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            basePositions[i] = renderer.transform.localPosition;
            baseScales[i] = renderer.transform.localScale;
            baseRotations[i] = renderer.transform.localRotation;
        }
    }

    /// <summary>
    /// 四角形を下から上へ移動させ、色と透明度を更新
    /// </summary>
    private void UpdateSquares()
    {
        if (squareRenderers == null || squareRenderers.Length == 0)
        {
            return;
        }

        var duration = Mathf.Max(0.1f, loopDuration);
        var height = Mathf.Max(0f, loopHeight);
        for (var i = 0; i < squareRenderers.Length; i++)
        {
            var renderer = squareRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            var phase = Mathf.Repeat((elapsedTime + stagger * i) / duration, 1f);
            var progress = Mathf.SmoothStep(0f, 1f, phase);
            var visibility = Mathf.Sin(phase * Mathf.PI);
            var transform = renderer.transform;
            transform.localPosition = basePositions[i] + Vector3.up * Mathf.Lerp(-height * 0.5f, height * 0.5f, progress);
            transform.localScale = baseScales[i] * Mathf.Lerp(0.86f, 1.08f, visibility);
            var rotationAmount = Mathf.Sin(phase * Mathf.PI * 2f + i * 0.7f) * rotationSpeed * 0.08f;
            transform.localRotation = baseRotations[i] * Quaternion.Euler(0f, rotationAmount, rotationAmount * 0.15f);

            var color = GetSquareColor(i);
            color.a *= visibility * 0.92f;
            SetRendererColor(renderer, color);
        }
    }

    /// <summary>
    /// ゴール周辺のライトをゆっくり明滅させる
    /// </summary>
    private void UpdateGlowLight()
    {
        if (glowLight == null)
        {
            return;
        }

        var pulse = 0.82f + Mathf.Sin(elapsedTime * Mathf.PI * 2f) * 0.18f;
        glowLight.intensity = glowLightIntensity * pulse;
    }

    /// <summary>
    /// 四角形の色配列から対象の色を取得
    /// </summary>
    /// <param name="index">取得する四角形の番号です。</param>
    /// <returns>表示する色です。</returns>
    private Color GetSquareColor(int index)
    {
        if (squareColors != null && index < squareColors.Length)
        {
            return squareColors[index];
        }

        return Color.cyan;
    }

    /// <summary>
    /// 指定した四角形のマテリアル色を更新
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
    /// 管理している四角形Rendererの表示状態を切り替え
    /// </summary>
    /// <param name="enabled">表示する場合はtrueです。</param>
    private void SetRenderersEnabled(bool enabled)
    {
        if (squareRenderers == null)
        {
            return;
        }

        foreach (var renderer in squareRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    /// <summary>
    /// 四角形を保存した初期Transformへ戻す
    /// </summary>
    private void ResetSquareTransforms()
    {
        if (squareRenderers == null)
        {
            return;
        }

        for (var i = 0; i < squareRenderers.Length; i++)
        {
            var renderer = squareRenderers[i];
            if (renderer == null || basePositions == null || i >= basePositions.Length)
            {
                continue;
            }

            renderer.transform.localPosition = basePositions[i];
            renderer.transform.localScale = baseScales[i];
            renderer.transform.localRotation = baseRotations[i];
        }
    }
}
