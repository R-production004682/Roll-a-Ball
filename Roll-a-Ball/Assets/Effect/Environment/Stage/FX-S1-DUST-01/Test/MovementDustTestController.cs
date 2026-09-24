using UnityEngine;

/// <summary>
/// 既存エフェクト展示と併用できる移動粉塵テストの表示を切り替える
/// </summary>
public sealed class MovementDustTestController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button toggleButton;
    [SerializeField] private GameObject previewRoot;
    [SerializeField] private GameObject statusRoot;

    /// <summary>
    /// テストボタンのイベントを登録する
    /// </summary>
    private void OnEnable()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (toggleButton != null)
        {
            toggleButton.gameObject.SetActive(false);
        }
        return;
#else
        if (toggleButton == null || previewRoot == null || statusRoot == null)
        {
            Debug.LogError("MovementDustTestControllerのボタン、展示、状態表示を設定してください。", this);
            return;
        }

        toggleButton.onClick.AddListener(TogglePreview);
#endif
    }

    /// <summary>
    /// 移動粉塵の専用ボールと草地・土の展示を表示または非表示にする
    /// </summary>
    public void TogglePreview()
    {
        if (previewRoot == null || statusRoot == null)
        {
            return;
        }

        var showing = !previewRoot.activeSelf;
        statusRoot.SetActive(showing);
        previewRoot.SetActive(showing);
    }

    /// <summary>
    /// イベントを解除し、展示終了時に粒子も消去する
    /// </summary>
    private void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePreview);
        }

        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }

        if (statusRoot != null)
        {
            statusRoot.SetActive(false);
        }
    }
}
