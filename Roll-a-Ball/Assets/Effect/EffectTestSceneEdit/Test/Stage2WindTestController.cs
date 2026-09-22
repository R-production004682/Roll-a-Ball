using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EffectTestSceneでステージ2の風エフェクト展示を表示または非表示にする
/// </summary>
public sealed class Stage2WindTestController : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject previewRoot;
    private bool showing;

    /// <summary>
    /// ステージ2風テストのボタン購読を登録する
    /// </summary>
    private void OnEnable()
    {
        if (toggleButton == null || previewRoot == null)
        {
            Debug.LogError("ステージ2風テストのボタンとPreview参照を設定してください。", this);
            return;
        }

        toggleButton.onClick.AddListener(TogglePreview);
    }

    /// <summary>
    /// ステージ2の風展示を切り替える
    /// </summary>
    public void TogglePreview()
    {
        showing = !showing;
        previewRoot.SetActive(showing);
    }

    /// <summary>
    /// ボタン購読を解除し、テスト表示を非表示へ戻す
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
