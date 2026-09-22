using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem goalEffect;//ゴールエフェクト

    /// <summary>
    /// ゴールに衝突した際にゴールエフェクトが再生しゲームをクリアをする
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (other.CompareTag("Player"))//プレイヤーとの衝突
        {
            goalEffect.Play();//ゴールエフェクトを再生
            GameManager.instance.StageCompleted();//クリア処理を行う

            Debug.Log("ゲームクリア");
        }
    }
}
