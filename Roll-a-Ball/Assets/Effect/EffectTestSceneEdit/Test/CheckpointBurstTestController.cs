using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チェックポイント通過演出と状態表示をテストシーンで操作
/// </summary>
public sealed class CheckpointBurstTestController : MonoBehaviour
{
    [SerializeField] private GameObject previewRoot;
    [SerializeField] private CheckpointBurstEffect effect;
    [SerializeField] private Button showButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button stateButton;
    [SerializeField] private Button resetButton;

    /// <summary>
    /// EditorとDevelopment Buildのみテストボタンを有効にする
    /// </summary>
    private void OnEnable()
    {
        if (previewRoot == null || effect == null || showButton == null ||
            playButton == null || stateButton == null || resetButton == null)
        {
            Debug.LogError("CheckpointBurstTestController: A案と操作ボタンの参照を設定してください。", this);
            enabled = false;
            return;
        }

        var allowed = Application.isEditor || Debug.isDebugBuild;
        showButton.gameObject.SetActive(allowed);
        playButton.gameObject.SetActive(allowed);
        stateButton.gameObject.SetActive(allowed);
        resetButton.gameObject.SetActive(allowed);
        previewRoot.SetActive(false);
        if (!allowed)
        {
            return;
        }

        showButton.onClick.AddListener(TogglePreview);
        playButton.onClick.AddListener(PlayEffect);
        stateButton.onClick.AddListener(ToggleCheckpointState);
        resetButton.onClick.AddListener(ResetCheckpoint);
    }

    /// <summary>
    /// チェックポイントの表示または非表示にする
    /// </summary>
    public void TogglePreview()
    {
        previewRoot.SetActive(!previewRoot.activeSelf);
        if (previewRoot.activeSelf)
        {
            effect.ResetCheckpoint();
        }
    }

    /// <summary>
    /// チェックポイントを未到達状態から再生する
    /// </summary>
    public void PlayEffect()
    {
        previewRoot.SetActive(true);
        effect.ResetCheckpoint();
        effect.Play();
    }

    /// <summary>
    /// 現在地点と通過済みのチェックポイントを矢印表示を切り替える
    /// </summary>
    public void ToggleCheckpointState()
    {
        previewRoot.SetActive(true);
        var state = effect.State == CheckpointBurstEffect.CheckpointState.Current
            ? CheckpointBurstEffect.CheckpointState.Visited
            : CheckpointBurstEffect.CheckpointState.Current;
        effect.SetState(state);
    }

    /// <summary>
    /// 粒子を停止して袋を未到達の初期状態へ戻す
    /// </summary>
    public void ResetCheckpoint()
    {
        previewRoot.SetActive(true);
        effect.ResetCheckpoint();
    }

    /// <summary>
    /// ボタン購読を解除してプレビューの寿命を揃える
    /// </summary>
    private void OnDisable()
    {
        if (showButton != null)
        {
            showButton.onClick.RemoveListener(TogglePreview);
        }
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(PlayEffect);
        }
        if (stateButton != null)
        {
            stateButton.onClick.RemoveListener(ToggleCheckpointState);
        }
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetCheckpoint);
        }
        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }
}
