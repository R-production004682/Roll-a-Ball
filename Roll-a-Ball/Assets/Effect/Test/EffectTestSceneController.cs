using UnityEngine;

/// <summary>
/// EffectTestScene専用のエフェクト再生ボタンを管理します
/// </summary>
public sealed class EffectTestSceneController : MonoBehaviour
{
    [SerializeField]
    private CurrencyPickupEffect smallRewardEffect;

    [SerializeField]
    private CurrencyPickupEffect largeRewardEffect;

    [SerializeField]
    private UnityEngine.UI.Button smallRewardButton;

    [SerializeField]
    private UnityEngine.UI.Button largeRewardButton;

    /// <summary>
    /// テストボタンのクリック処理を登録
    /// </summary>
    private void OnEnable()
    {
        if (smallRewardButton != null)
        {
            smallRewardButton.onClick.AddListener(PlaySmallRewardEffect);
        }

        if (largeRewardButton != null)
        {
            largeRewardButton.onClick.AddListener(PlayLargeRewardEffect);
        }
    }

    /// <summary>
    /// テストボタンのクリック処理を解除
    /// </summary>
    private void OnDisable()
    {
        if (smallRewardButton != null)
        {
            smallRewardButton.onClick.RemoveListener(PlaySmallRewardEffect);
        }

        if (largeRewardButton != null)
        {
            largeRewardButton.onClick.RemoveListener(PlayLargeRewardEffect);
        }
    }

    /// <summary>
    /// 小さい報酬用のエフェクトを再生
    /// </summary>
    private void PlaySmallRewardEffect()
    {
        if (smallRewardEffect == null)
        {
            Debug.LogWarning("小さい報酬用のエフェクトが設定されていません。", this);
            return;
        }

        smallRewardEffect.Play(false);
    }

    /// <summary>
    /// 大きい報酬用のエフェクトを再生
    /// </summary>
    private void PlayLargeRewardEffect()
    {
        if (largeRewardEffect == null)
        {
            Debug.LogWarning("大きい報酬用のエフェクトが設定されていません。", this);
            return;
        }

        largeRewardEffect.Play(true);
    }
}
