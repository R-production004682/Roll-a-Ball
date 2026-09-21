using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 木漏れ日の比較表示とテスト中だけの環境光を管理する
/// </summary>
public sealed class DappledLightTestController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button toggleButton;
    [SerializeField] private GameObject previewRoot;
    [SerializeField] private Light sceneLight;
    [SerializeField] private Camera testCamera;
    private Color previousAmbient;
    private AmbientMode previousAmbientMode;
    private float previousIntensity;
    private bool previousPostProcessing;
    private CameraClearFlags previousClearFlags;
    private Color previousBackground;
    private bool showing;

    /// <summary>
    /// 比較ボタンを登録する
    /// </summary>
    private void OnEnable()
    {
        if (toggleButton == null || previewRoot == null || sceneLight == null || testCamera == null)
        {
            Debug.LogError("木漏れ日テストのボタン・Preview・Light・Camera参照を設定してください。", this);
            return;
        }

        toggleButton.onClick.AddListener(TogglePreview);
    }

    /// <summary>
    /// テスト表示を切り替えて元のシーン設定を保存または復元する
    /// </summary>
    public void TogglePreview()
    {
        if (showing)
        {
            RestoreEnvironment();
            return;
        }

        previousAmbient = RenderSettings.ambientLight;
        previousAmbientMode = RenderSettings.ambientMode;
        previousIntensity = sceneLight.intensity;
        var cameraData = testCamera.GetUniversalAdditionalCameraData();
        previousPostProcessing = cameraData.renderPostProcessing;
        previousClearFlags = testCamera.clearFlags;
        previousBackground = testCamera.backgroundColor;
        testCamera.clearFlags = CameraClearFlags.SolidColor;
        testCamera.backgroundColor = new Color(0.11f, 0.15f, 0.18f);
        cameraData.renderPostProcessing = true;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.4f, 0.46f);
        sceneLight.intensity = 0.18f;
        previewRoot.SetActive(true);
        showing = true;
    }

    /// <summary>
    /// 木漏れ日テストだけを停止して通常のエフェクト確認環境へ戻す
    /// </summary>
    private void RestoreEnvironment()
    {
        if (!showing)
        {
            return;
        }

        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
        RenderSettings.ambientLight = previousAmbient;
        RenderSettings.ambientMode = previousAmbientMode;
        if (sceneLight != null)
        {
            sceneLight.intensity = previousIntensity;
        }

        if (testCamera != null)
        {
            testCamera.GetUniversalAdditionalCameraData().renderPostProcessing = previousPostProcessing;
            testCamera.clearFlags = previousClearFlags;
            testCamera.backgroundColor = previousBackground;
        }
        showing = false;
    }

    /// <summary>
    /// ボタン購読を解除してテスト中の環境変更を残さない
    /// </summary>
    private void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePreview);
        }

        RestoreEnvironment();
    }
}
