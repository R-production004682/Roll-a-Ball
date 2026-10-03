using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// DOTween の短いシェイクとズームを Cinemachine の最終出力へ加える被弾演出
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class HitCameraEffect : CinemachineExtension
{
    private const float MinimumEffectDuration = 0.01f;
    private const float ShakeRandomnessDegrees = 90f;
    private const float MinimumFieldOfViewDegrees = 1f;
    private const float MaximumFieldOfViewDegrees = 179f;

    [SerializeField, Min(MinimumEffectDuration), Tooltip("被弾直後のシェイクが減衰して消えるまでの秒数")]
    private float duration = 0.28f;

    [SerializeField, Tooltip("カメラのローカル方向でのシェイク幅。Z は奥行き方向です")]
    private Vector3 positionStrength = new Vector3(0.22f, 0.14f, 0.03f);

    [SerializeField, Tooltip("被弾時の傾きの強さ（度）。Z は画面のロールです")]
    private Vector3 rotationStrength = new Vector3(0.6f, 0.8f, 1.6f);

    [SerializeField, Min(1), Tooltip("シェイク中の振動回数")]
    private int vibrato = 14;

    [SerializeField, Range(0f, 8f), Tooltip("被弾直後に狭める視野角（度）。0 ならズームしません")]
    private float zoomKick = 2.5f;

    private Sequence sequence;
    private Vector3 positionOffset;
    private Vector3 rotationOffset;
    private float fieldOfViewOffset;

    /// <summary>
    /// 被弾演出を再生中かを取得する
    /// </summary>
    public bool IsPlaying => sequence != null && sequence.IsActive();

    /// <summary>
    /// 前の演出を消去して短いシェイクと瞬間的なズームを再生する
    /// </summary>
    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Stop();
        var effectDuration = Mathf.Max(MinimumEffectDuration, duration);
        var vibrations = Mathf.Max(1, vibrato);
        fieldOfViewOffset = -zoomKick;
        sequence = DOTween.Sequence();
        sequence.SetUpdate(true).SetTarget(this);
        sequence.Join(DOTween.Shake(
            () => positionOffset, value => positionOffset = value,
            effectDuration, positionStrength, vibrations, ShakeRandomnessDegrees, true, ShakeRandomnessMode.Harmonic));
        sequence.Join(DOTween.Shake(
            () => rotationOffset, value => rotationOffset = value,
            effectDuration, rotationStrength, vibrations, ShakeRandomnessDegrees, true, ShakeRandomnessMode.Harmonic));
        sequence.Join(DOTween.To(
            () => fieldOfViewOffset, value => fieldOfViewOffset = value,
            0f, effectDuration).SetEase(Ease.OutCubic));
        sequence.OnComplete(ResetOffsets);
    }

    /// <summary>
    /// 所有する Tween を停止し位置・傾き・視野角の補正を消す
    /// </summary>
    public void Stop()
    {
        sequence?.Kill();
        ResetOffsets();
    }

    /// <summary>
    /// Tween の完了時に参照とカメラへの補正値を初期化する
    /// </summary>
    private void ResetOffsets()
    {
        sequence = null;
        positionOffset = Vector3.zero;
        rotationOffset = Vector3.zero;
        fieldOfViewOffset = 0f;
    }

    /// <summary>
    /// 追従計算が終わったカメラへローカル方向の揺れと傾きとズームを加える
    /// </summary>
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || !isActiveAndEnabled)
        {
            return;
        }

        state.PositionCorrection += state.GetFinalOrientation() * positionOffset;
        state.OrientationCorrection *= Quaternion.Euler(rotationOffset);
        state.Lens.FieldOfView = Mathf.Clamp(state.Lens.FieldOfView + fieldOfViewOffset, MinimumFieldOfViewDegrees, MaximumFieldOfViewDegrees);
    }

    /// <summary>
    /// リスポーン等の追従対象の瞬間移動では揺れを復帰地点へ持ち越さない
    /// </summary>
    public override void OnTargetObjectWarped(
        CinemachineVirtualCameraBase vcam, Transform target, Vector3 positionDelta)
    {
        Stop();
    }

    /// <summary>
    /// 無効化時に再生を止め補正を残さない
    /// </summary>
    private void OnDisable()
    {
        Stop();
    }

    /// <summary>
    /// 破棄時に Tween を停止し Cinemachine の接続を解除する
    /// </summary>
    protected override void OnDestroy()
    {
        Stop();
        base.OnDestroy();
    }
}
