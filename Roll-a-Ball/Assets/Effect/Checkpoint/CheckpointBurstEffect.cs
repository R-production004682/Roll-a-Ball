using UnityEngine;

/// <summary>
/// 袋の分割とチェックポイントの三状態を表示する再利用可能な演出
/// </summary>
public sealed class CheckpointBurstEffect : MonoBehaviour
{
    public enum CheckpointState { Unvisited, Visited, Current }

    [Header("参照（通常はPrefabの設定を維持）")]
    [Tooltip("袋の左半分のTransform。破裂時に移動・回転・縮小する対象。通常はPrefabの参照を維持します。")]
    [SerializeField] private Transform leftHalf;
    [Tooltip("袋の右半分のTransform。左半分と反対方向へ開きます。通常はPrefabの参照を維持します。")]
    [SerializeField] private Transform rightHalf;
    [Tooltip("到達状態を示す矢印のTransform。位置と大きさを演出側で制御します。")]
    [SerializeField] private Transform arrow;
    [Tooltip("矢印の色を変えるRenderer。Current Color / Visited Colorの適用先です。")]
    [SerializeField] private Renderer arrowRenderer;
    [Tooltip("破裂時の破片ParticleSystem。量はFragment Count、粒子の形・サイズ・寿命はこのSystemで調整します。")]
    [SerializeField] private ParticleSystem fragments;
    [Tooltip("破裂直後の閃光ParticleSystem。1粒放出します。色・サイズ・寿命はこのSystemで調整します。")]
    [SerializeField] private ParticleSystem flash;
    [Header("状態の色")]
    [Tooltip("未到達・現在地点の矢印色。Brightnessを乗算します。変更は次の状態変更・再生で反映します。")]
    [SerializeField] private Color currentColor = new Color(1f, .65f, .02f);
    [Tooltip("通過済みの矢印色。Brightnessは乗算しません。変更は次の状態変更で反映します。")]
    [SerializeField] private Color visitedColor = new Color(.2f, .4f, .45f);
    [Tooltip("未到達・現在地点の矢印色の倍率。大きいほど明るく、通過済みの色や破片の色は変えません。")]
    [SerializeField, Min(.1f)] private float brightness = 1.15f;
    [Header("破裂の動き・量")]
    [Tooltip("破裂の再生速度倍率。1が標準、2なら袋の動きと粒子が約2倍速。袋の実時間はDuration / この値です。")]
    [SerializeField, Min(.1f)] private float playbackSpeed = 1f;
    [Tooltip("袋の片側が左右へ開く距離（ローカル座標）。大きいほど左右へ広がり、両側の間隔は約2倍になります。")]
    [SerializeField, Min(.1f)] private float splitDistance = 1.2f;
    [Tooltip("速度倍率1での袋の破裂時間（秒）。大きいほどゆっくり開きます。粒子の寿命は各ParticleSystemで別に設定します。")]
    [SerializeField, Min(.1f)] private float duration = .75f;
    [Tooltip("破裂時に追加放出する破片数（1〜40粒）。大きいほど密度が増えます。ParticleSystemのBurst設定は別です。")]
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
