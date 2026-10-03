using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField, Tooltip("プレイヤーに与えるジャンプ力")]
    private float jumpForce = 10;//ジャンプするときに与える力

    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (!other.CompareTag("Player"))//プレイヤー以外との衝突の場合処理しない
            return;
        Player player = other.GetComponent<Player>();//プレイヤー取得

        if(player != null)
        {
            player.Jump(jumpForce);//プレイヤーのジャンプ処理を行う
        }
    }
}
