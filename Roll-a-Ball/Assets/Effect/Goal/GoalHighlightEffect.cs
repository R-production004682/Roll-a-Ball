using UnityEngine;

/// <summary>
/// ゴール地点で四角形の光が下から上へ流れ続ける常時演出です
/// </summary>
public sealed class GoalHighlightEffect : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer[] squareRenderers;

    [SerializeField]
    private Light glowLight;

    [SerializeField]
    private Color[] squareColors;

    [SerializeField]
    private bool playOnEnable = true;

    [SerializeField]
    private float loopDuration = 2.4f;

    [SerializeField]
    private float loopHeight = 2.2f;

    [SerializeField]
    private float stagger = 0.36f;

    [SerializeField]
    private float rotationSpeed = 45f;

    [SerializeField]
    private float glowLightIntensity = 1.6f;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3[] basePositions;
    private Vector3[] baseScales;
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
            return;
        }

        basePositions = new Vector3[squareRenderers.Length];
        baseScales = new Vector3[squareRenderers.Length];
        for (var i = 0; i < squareRenderers.Length; i++)
        {
            var renderer = squareRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            basePositions[i] = renderer.transform.localPosition;
            baseScales[i] = renderer.transform.localScale;
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

            // 回転しながら上がってってほしい場合は以下のコメントアウトを外す
            // transform.localRotation = Quaternion.Euler(0f, rotationSpeed * elapsedTime + i * 18f, 0f);

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
}
