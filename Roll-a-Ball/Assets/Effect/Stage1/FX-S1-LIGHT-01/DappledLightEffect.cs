using UnityEngine;

/// <summary>
/// スポットライトのCookieと影に一致する局所的な木漏れ日を制御する
/// </summary>
[ExecuteAlways]
public sealed class DappledLightEffect : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private MeshRenderer volumeRenderer;
    [SerializeField, Range(0.001f, 0.2f)] private float density = 0.035f;
    [SerializeField, Range(0f, 2f)] private float scattering = 0.16f;
    [SerializeField, Range(0f, 0.8f)] private float anisotropy = 0.2f;
    [SerializeField, Range(24, 96)] private int sampleCount = 96;
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
