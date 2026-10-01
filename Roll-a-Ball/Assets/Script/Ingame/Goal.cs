using System.Collections;
using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField,Tooltip("クリア時のゴールエフェクトを入れる")]
    private GoalClearEffect clearEffect;

    private bool isCompleting;

    /// <summary>
    /// プレイヤーがゴールエリアに入ったときのクリア開始処理
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isCompleting)//プレイヤー以外の衝突、または既にステージをクリアしている場合は処理しない
        {
            return;
        }

        isCompleting = true;

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
