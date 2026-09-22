using UnityEngine;

/// <summary>
/// ステージ2の風パラメータを受けて木や草の見た目を揺らす
/// </summary>
public sealed class WindSwayTarget : MonoBehaviour
{
    [SerializeField, Range(0f, 12f)] private float swayAngle = 4.8f;
    [SerializeField, Range(0f, 0.5f)] private float positionSway = 0.06f;
    [SerializeField, Range(0.25f, 3f)] private float speedMultiplier = 1.15f;

    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 windDirection = Vector3.forward;
    private float windStrength;
    private float turbulence;
    private float phase;
    private bool initialized;
    private bool hasWind;

    /// <summary>
    /// 初期姿勢とオブジェクトごとの揺れ位相を保存する
    /// </summary>
    private void Awake()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
        phase = Mathf.Abs(GetInstanceID()) * 0.0137f;
        initialized = true;
    }

    /// <summary>
    /// 風の入力を使って、毎フレーム異なる揺れを見た目へ反映する
    /// </summary>
    private void LateUpdate()
    {
        if (!initialized || !hasWind || !Application.isPlaying)
        {
            return;
        }

        var speed = (1.2f + windStrength * 2.2f + turbulence * 0.7f) * speedMultiplier;
        var time = Time.time * speed + phase;
        var primary = Mathf.Sin(time);
        var secondary = Mathf.Sin(time * 0.61f + phase * 1.7f);
        var irregular = Mathf.Sin(time * 1.73f + phase * 0.37f) * turbulence;
        var sway = (primary + secondary * 0.32f + irregular * 0.18f) * swayAngle * Mathf.Clamp01(windStrength);
        var horizontal = new Vector3(windDirection.z, 0f, -windDirection.x);
        var offset = horizontal * (Mathf.Sin(time * 0.77f + phase) * positionSway * windStrength);
        offset += Vector3.up * (Mathf.Sin(time * 0.49f + phase * 0.8f) * positionSway * 0.24f * windStrength);
        transform.localPosition = restPosition + offset;
        transform.localRotation = restRotation * Quaternion.Euler(
            sway * 0.42f + windDirection.z * sway * 0.2f,
            sway * 0.12f,
            sway + windDirection.x * sway * 0.18f);
    }

    /// <summary>
    /// エフェクトと共有する風向き、強さ、乱流を設定する
    /// </summary>
    public void SetWind(Vector3 direction, float strength, float turbulenceAmount)
    {
        windDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        windStrength = Mathf.Max(0f, strength);
        turbulence = Mathf.Clamp01(turbulenceAmount);
        hasWind = true;
    }

    /// <summary>
    /// 揺れを停止して、元の姿勢へ戻す
    /// </summary>
    public void ResetWind()
    {
        hasWind = false;
        if (!initialized)
        {
            return;
        }

        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
    }

    /// <summary>
    /// 無効化時に対象オブジェクトを初期姿勢へ戻す
    /// </summary>
    private void OnDisable()
    {
        ResetWind();
    }
}
