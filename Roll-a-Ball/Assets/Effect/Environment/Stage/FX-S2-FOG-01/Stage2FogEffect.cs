using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ステージ2の箱型ローカルフォグを制御し、他シーンの描画設定を変更しない
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public sealed class Stage2FogEffect : MonoBehaviour
{
    public enum FogQuality
    {
        Low = 8,
        Medium = 16,
        High = 24
    }

    [Header("霧の色と距離")]
    [Tooltip("霧の色。濃さはDensity、透明度の上限はMaximum Opacityで調整します。色のAは霧の濃さに使いません。")]
    [SerializeField] private Color fogColor = new Color(0.38f, 0.46f, 0.49f, 1f);
    [Tooltip("霧の濃さ係数（0〜0.5）。大きいほど濃くなり、0で消えます。最大の濃さはMaximum Opacityで制限します。")]
    [SerializeField, Range(0f, 0.5f)] private float density = 0.10f;
    [Tooltip("カメラから霧の濃さが増え始める距離（ワールド座標）。大きいほど手前の視界を確保できます。")]
    [SerializeField, Min(0f)] private float startDistance = 12f;
    [Tooltip("距離による濃さが最大になる距離（ワールド座標）。大きいほど濃くなるまでが緩やか。Start Distance＋0.01以上へ補正します。")]
    [SerializeField, Min(0.01f)] private float endDistance = 36f;
    [Header("高さと模様")]
    [Tooltip("箱の底から上へ霧を薄くする強さ（0〜8）。大きいほど低い場所に霧が集まり、0なら高さで減衰しません。")]
    [SerializeField, Range(0f, 8f)] private float heightFalloff = 2f;
    [Tooltip("霧の模様の細かさ（0.01〜2）。大きいほど細かい模様、小さいほど大きな塊になります。")]
    [SerializeField, Range(0.01f, 2f)] private float noiseScale = 0.3f;
    [Tooltip("霧模様の流れるワールドXYZ速度（距離/秒）。大きいほど速く、ゼロなら模様が静止します。")]
    [SerializeField] private Vector3 noiseSpeed = new Vector3(0.055f, 0.005f, 0.025f);
    [Tooltip("霧の濃淡のばらつき（0〜1）。大きいほどまだらになり、0ならNoiseによる濃淡はありません。")]
    [SerializeField, Range(0f, 1f)] private float noiseStrength = 0.65f;
    [Header("視認性と品質")]
    [Tooltip("霧の不透明度上限（0〜0.75）。大きいほど奥を隠せます。足場が見えにくい場合は下げます。")]
    [SerializeField, Range(0f, 0.75f)] private float maximumOpacity = 0.45f;
    [Tooltip("霧の描画品質。Low=8 / Medium=16 / High=24サンプル。高いほど滑らかですがGPU負荷が増えます。")]
    [SerializeField] private FogQuality quality = FogQuality.Medium;

    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int FogParametersId = Shader.PropertyToID("_FogParameters");
    private static readonly int NoiseParametersId = Shader.PropertyToID("_NoiseParameters");
    private static readonly int NoiseVelocityId = Shader.PropertyToID("_NoiseVelocity");
    private MeshRenderer fogRenderer;
    private MaterialPropertyBlock properties;

    /// <summary>
    /// 有効化時にこのオブジェクトの描画だけを再開する
    /// </summary>
    private void OnEnable()
    {
        ApplySettings();
        fogRenderer.enabled = true;
    }

    /// <summary>
    /// コンポーネントの無効化でもフォグを残さない
    /// </summary>
    private void OnDisable()
    {
        if (fogRenderer != null)
        {
            fogRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Inspectorの変更を安全な範囲に補正して反映する
    /// </summary>
    private void OnValidate()
    {
        density = Mathf.Clamp(density, 0f, 0.5f);
        startDistance = Mathf.Max(0f, startDistance);
        endDistance = Mathf.Max(startDistance + 0.01f, endDistance);
        ApplySettings();
    }

    /// <summary>
    /// 共有マテリアルを変更せずインスタンス固有の値を送る
    /// </summary>
    private void ApplySettings()
    {
        fogRenderer = GetComponent<MeshRenderer>();
        properties ??= new MaterialPropertyBlock();
        fogRenderer.GetPropertyBlock(properties);
        properties.SetColor(FogColorId, fogColor);
        properties.SetVector(FogParametersId, new Vector4(density, startDistance, endDistance, maximumOpacity));
        properties.SetVector(NoiseParametersId, new Vector4(noiseScale, noiseStrength, heightFalloff, (int)quality));
        properties.SetVector(NoiseVelocityId, noiseSpeed);
        fogRenderer.SetPropertyBlock(properties);
        fogRenderer.shadowCastingMode = ShadowCastingMode.Off;
        fogRenderer.receiveShadows = false;
    }

    /// <summary>
    /// ステージやリザルトの切り替えに合わせてフォグを有効化する
    /// </summary>
    public void SetVisible(bool visible)
    {
        enabled = visible;
    }

    /// <summary>
    /// 積分サンプル数を変更して描画負荷を調整する
    /// </summary>
    public void SetQuality(FogQuality value)
    {
        quality = value;
        ApplySettings();
    }
}
