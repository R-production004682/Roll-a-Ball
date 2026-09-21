using System.Collections;
using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem goalEffect;//旧ゴールエフェクト

    [SerializeField]
    private GoalClearEffect clearEffect;

    private bool isCompleting;

    /// <summary>
    /// プレイヤーがゴールエリアに入ったときのクリア開始処理
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isCompleting)
        {
            return;
        }

        isCompleting = true;
        if (clearEffect == null)
        {
            PlayLegacyGoalEffect();
            CompleteStage();
            return;
        }

        clearEffect.gameObject.SetActive(true);
        clearEffect.Play();
        StartCoroutine(CompleteStageAfterEffect());
    }

    /// <summary>
    /// クリア演出の再生時間後にステージクリアを確定
    /// </summary>
    private IEnumerator CompleteStageAfterEffect()
    {
        yield return new WaitForSeconds(clearEffect.Duration);
        CompleteStage();
    }

    /// <summary>
    /// 既存のParticleSystemを使ってクリア演出を再生
    /// </summary>
    private void PlayLegacyGoalEffect()
    {
        if (goalEffect != null)
        {
            goalEffect.Play();
        }
    }

    /// <summary>
    /// ステージクリア処理を実行
    /// </summary>
    private void CompleteStage()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.StageCompleted();
        }

        Debug.Log("ゲームクリア");
    }
}
