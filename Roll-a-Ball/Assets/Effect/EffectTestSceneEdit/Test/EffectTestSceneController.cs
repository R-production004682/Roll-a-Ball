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
    private GoalHighlightEffect goalHighlightEffect;

    [SerializeField]
    private GoalClearEffect goalClearEffect;

    [SerializeField]
    private UnityEngine.UI.Button smallRewardButton;

    [SerializeField]
    private UnityEngine.UI.Button largeRewardButton;

    [SerializeField]
    private UnityEngine.UI.Button goalEffectButton;

    [SerializeField]
    private UnityEngine.UI.Button goalClearEffectButton;

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

        if (goalEffectButton != null)
        {
            goalEffectButton.onClick.AddListener(PlayGoalEffect);
        }

        if (goalClearEffectButton != null)
        {
            goalClearEffectButton.onClick.AddListener(PlayGoalClearEffect);
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

        if (goalEffectButton != null)
        {
            goalEffectButton.onClick.RemoveListener(PlayGoalEffect);
        }

        if (goalClearEffectButton != null)
        {
            goalClearEffectButton.onClick.RemoveListener(PlayGoalClearEffect);
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

        smallRewardEffect.gameObject.SetActive(true);
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

        largeRewardEffect.gameObject.SetActive(true);
        largeRewardEffect.Play(true);
    }

    /// <summary>
    /// ゴールエフェクトを有効化して再生
    /// </summary>
    private void PlayGoalEffect()
    {
        if (goalHighlightEffect == null)
        {
            Debug.LogWarning("ゴールエフェクトが設定されていません。", this);
            return;
        }

        goalHighlightEffect.gameObject.SetActive(true);
        goalHighlightEffect.Play();
    }

    /// <summary>
    /// クリアセレブレーションを有効化して再生
    /// </summary>
    private void PlayGoalClearEffect()
    {
        if (goalClearEffect == null)
        {
            Debug.LogWarning("クリアセレブレーションが設定されていません。", this);
            return;
        }

        goalClearEffect.gameObject.SetActive(true);
        goalClearEffect.Play();
    }
}
