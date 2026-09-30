using UnityEngine;

/// <summary>
/// 袋の分割とチェックポイントの三状態を表示する再利用可能な演出
/// </summary>
public sealed class CheckpointBurstEffect : MonoBehaviour
{
    public enum CheckpointState { Unvisited, Visited, Current }

    [SerializeField] private Transform leftHalf;
    [SerializeField] private Transform rightHalf;
    [SerializeField] private Transform arrow;
    [SerializeField] private Renderer arrowRenderer;
    [SerializeField] private ParticleSystem fragments;
    [SerializeField] private ParticleSystem flash;
    [SerializeField] private Color currentColor = new Color(1f, .65f, .02f);
    [SerializeField] private Color visitedColor = new Color(.2f, .4f, .45f);
    [SerializeField, Min(.1f)] private float brightness = 1.15f;
    [SerializeField, Min(.1f)] private float playbackSpeed = 1f;
    [SerializeField, Min(.1f)] private float splitDistance = 1.2f;
    [SerializeField, Min(.1f)] private float duration = .75f;
    [SerializeField, Range(1, 40)] private int fragmentCount = 18;
    private MaterialPropertyBlock properties;
    private Vector3 arrowPosition;
    private Vector3 arrowScale;
    private float elapsed;
    private bool ready;
    private bool playing;
    public CheckpointState State { get; private set; }
    public bool IsPlaying => playing;

    /// <summary>
    /// 必須参照と矢印の基準姿勢を初期化する
    /// </summary>
    private void Awake()
    {
        ready = leftHalf != null && rightHalf != null && arrow != null &&
            arrowRenderer != null && fragments != null && flash != null;
        if (!ready)
        {
            Debug.LogError("CheckpointBurstEffect: 袋・矢印・粒子の参照を設定してください。", this);
            enabled = false;
            return;
        }
        properties = new MaterialPropertyBlock();
        arrowPosition = arrow.localPosition;
        arrowScale = arrow.localScale;
        ResetCheckpoint();
    }

    /// <summary>
    /// 未到達のときだけ袋を破裂させて現在地点へ切り替える
    /// </summary>
    public void Play()
    {
        if (!ready || !isActiveAndEnabled || State != CheckpointState.Unvisited)
        {
            return;
        }
        State = CheckpointState.Current;
        elapsed = 0f;
        playing = true;
        SetArrowColor(currentColor * brightness);
        var main = fragments.main;
        main.simulationSpeed = playbackSpeed;
        var flashMain = flash.main;
        flashMain.simulationSpeed = playbackSpeed;
        fragments.Play();
        fragments.Emit(fragmentCount);
        flash.Play();
        flash.Emit(1);
    }

    /// <summary>
    /// リスポーンや次地点への移行時に破裂を再生せず状態を復元する
    /// </summary>
    public void SetState(CheckpointState state)
    {
        if (!ready)
        {
            return;
        }
        StopTransient();
        State = state;
        var unopened = state == CheckpointState.Unvisited;
        leftHalf.gameObject.SetActive(unopened);
        rightHalf.gameObject.SetActive(unopened);
        leftHalf.localPosition = rightHalf.localPosition = Vector3.zero;
        leftHalf.localRotation = rightHalf.localRotation = Quaternion.identity;
        leftHalf.localScale = rightHalf.localScale = Vector3.one;
        arrow.localPosition = arrowPosition + (unopened ? Vector3.zero : Vector3.up * .28f);
        arrow.localScale = arrowScale * (state == CheckpointState.Visited ? .8f : 1f);
        SetArrowColor(state == CheckpointState.Visited ? visitedColor : currentColor * brightness);
    }

    /// <summary>
    /// リトライ用に袋・矢印・粒子を未到達へ戻す
    /// </summary>
    public void ResetCheckpoint()
    {
        SetState(CheckpointState.Unvisited);
    }

    /// <summary>
    /// 左右分割・縮小・矢印の出現を時間に合わせて更新する
    /// </summary>
    private void Update()
    {
        if (!playing)
        {
            return;
        }
        elapsed += Time.deltaTime * Mathf.Max(.1f, playbackSpeed);
        var t = Mathf.Clamp01(elapsed / duration);
        var outward = 1f - Mathf.Pow(1f - t, 3f);
        var size = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, 1f, t));
        var lift = Mathf.Sin(t * Mathf.PI) * .38f;
        leftHalf.localPosition = new Vector3(-splitDistance * outward, lift, 0f);
        rightHalf.localPosition = new Vector3(splitDistance * outward, lift, 0f);
        leftHalf.localRotation = Quaternion.Euler(0f, 0f, 38f * outward);
        rightHalf.localRotation = Quaternion.Euler(0f, 0f, -38f * outward);
        leftHalf.localScale = rightHalf.localScale = Vector3.one * size;
        arrow.localPosition = arrowPosition + Vector3.up * (.28f * outward);
        arrow.localScale = arrowScale * (1f + .15f * Mathf.Sin(t * Mathf.PI));
        if (t >= 1f)
        {
            playing = false;
            leftHalf.gameObject.SetActive(false);
            rightHalf.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 共有マテリアルを複製せず矢印の状態色を反映する
    /// </summary>
    private void SetArrowColor(Color color)
    {
        properties ??= new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        arrowRenderer.SetPropertyBlock(properties);
    }

    /// <summary>
    /// 一時粒子を停止して再利用時の残留を防ぐ
    /// </summary>
    private void StopTransient()
    {
        playing = false;
        fragments.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>
    /// 無効化中に中途半端な破片姿勢を残さず現在の状態へ収束する
    /// </summary>
    private void OnDisable()
    {
        SetState(State);
    }
}
