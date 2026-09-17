using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem goalEffect;//ゴールエフェクト

    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (other.CompareTag("Player"))//プレイヤーとの衝突
        {
            goalEffect.Play();//ゴールエフェクトを再生
            GameManager.instance.StageCompleted();//クリア処理を行う
        }
    }
}
