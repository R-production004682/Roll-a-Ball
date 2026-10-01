using UnityEngine;

/// <summary>
/// スポットライトのCookieと影に一致する局所的な木漏れ日を制御する
/// </summary>
[ExecuteAlways]
public sealed class DappledLightEffect : MonoBehaviour
{
    [Header("参照（通常はPrefabの設定を維持）")]
    [Tooltip("木漏れ日のSpot Light（必須）。色・Intensity・Range・Spot Angle・CookieはLight側で調整し、方向と影に光の筋を合わせます。")]
    [SerializeField] private Light sunLight;
    [Tooltip("光の筋を描く箱型ボリュームのMeshRenderer（必須）。描画範囲はこのTransformのScaleで調整します。")]
    [SerializeField] private MeshRenderer volumeRenderer;
    [Header("光の筋と品質")]
    [Tooltip("光を散乱させる媒質の濃さ（0.001〜0.2）。大きいほど光の筋が濃くなりますが、透過が減るため単純な明るさ倍率ではありません。")]
    [SerializeField, Range(0.001f, 0.2f)] private float density = 0.035f;
    [Tooltip("散乱光の明るさ倍率（0〜2）。大きいほど光の筋が明るく、0で散乱光が消えます。Lightの明るさとは別です。")]
    [SerializeField, Range(0f, 2f)] private float scattering = 0.16f;
    [Tooltip("散乱光の方向性（0〜0.8）。0は方向差が小さく、大きいほど光方向と視線が近い場合を強調し、見る角度で明るさが変わります。")]
    [SerializeField, Range(0f, 0.8f)] private float anisotropy = 0.2f;
    [Tooltip("光の筋を計算するサンプル数（24〜96）。大きいほど筋が滑らかになりますがGPU負荷が増えます。低品質設定では小さくします。")]
    [SerializeField, Range(24, 96)] private int sampleCount = 96;
    [Tooltip("再生中にSpot Lightを揺らす角度幅（度、0〜1）。大きいほどCookieと影が揺れ、0なら揺れません。")]
    [SerializeField, Range(0f, 1f)] private float foliageSway = 0.25f;

    private static readonly int SunPosition = Shader.PropertyToID("_SunPositionWS");
    private static readonly int Medium = Shader.PropertyToID("_Medium");
    private MaterialPropertyBlock properties;
    private Quaternion restRotation;
    private bool initialized;

    /// <summary>
    /// 参照を検証してインスタンス固有の描画設定を準備する
    /// </summary>
    private void OnEnable()
    {
        if (sunLight == null || volumeRenderer == null || sunLight.type != LightType.Spot)
        {
            Debug.LogError("木漏れ日にはSpot LightとVolume Rendererを指定してください。", this);
            enabled = false;
            return;
        }

        properties ??= new MaterialPropertyBlock();
        restRotation = sunLight.transform.localRotation;
        initialized = true;
        UpdateVolume();
    }

    /// <summary>
    /// 葉の影をゆっくり揺らしてライトと散乱の設定を同期する
    /// </summary>
    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        if (sunLight == null || volumeRenderer == null)
        {
            Debug.LogError("木漏れ日の参照が外れました。Spot LightとVolume Rendererを再設定してください。", this);
            enabled = false;
            return;
        }

        if (Application.isPlaying)
        {
            var sway = Mathf.Sin(Time.time * 0.43f) * foliageSway;
            sunLight.transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, sway);
        }

        UpdateVolume();
    }

    /// <summary>
    /// ライトが無効の場合は光の筋も消し共有マテリアルを変更せず値を渡す
    /// </summary>
    private void UpdateVolume()
    {
        var isLit = sunLight.isActiveAndEnabled && sunLight.intensity > 0f;
        properties.SetVector(SunPosition, new Vector4(sunLight.transform.position.x,
            sunLight.transform.position.y, sunLight.transform.position.z, isLit ? 1f : 0f));
        properties.SetVector(Medium, new Vector4(density, scattering, anisotropy, sampleCount));
        volumeRenderer.SetPropertyBlock(properties);
    }

    /// <summary>
    /// 再生中に変更したライトの向きを戻し光の筋を停止する
    /// </summary>
    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }

        if (sunLight != null)
        {
            sunLight.transform.localRotation = restRotation;
        }

        if (volumeRenderer != null)
        {
            properties.SetVector(SunPosition, Vector4.zero);
            volumeRenderer.SetPropertyBlock(properties);
        }

        initialized = false;
    }

    /// <summary>
    /// Prefab全体のライトと浮遊粒子を含めて表示を切り替える
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
