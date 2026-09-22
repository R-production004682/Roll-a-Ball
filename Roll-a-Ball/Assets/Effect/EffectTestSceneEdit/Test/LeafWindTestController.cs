using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EffectTestSceneでそよ風の展示を表示または非表示にする
/// </summary>
public sealed class LeafWindTestController : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject previewRoot;
    private bool showing;

    /// <summary>
    /// そよ風テストのボタン購読を登録する
    /// </summary>
    private void OnEnable()
    {
        if (toggleButton == null || previewRoot == null)
        {
            Debug.LogError("そよ風テストのボタンとPreview参照を設定してください。", this);
            return;
        }

        toggleButton.onClick.AddListener(TogglePreview);
    }

    /// <summary>
    /// そよ風の展示を切り替える
    /// </summary>
    public void TogglePreview()
    {
        showing = !showing;
        previewRoot.SetActive(showing);
    }

    /// <summary>
    /// ボタン購読を解除してテスト表示を残さない
    /// </summary>
    private void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePreview);
        }

        showing = false;
        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }
}
