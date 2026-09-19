using UnityEngine;

public class Respawnpoint : MonoBehaviour
{
    [SerializeField]
    private int pointNumber;//リスポーン地点の番号

    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if(other.CompareTag("Player"))//プレイヤーとの衝突
        {
            Player player =other.GetComponent<Player>();
            player.SetRespawnPointNumber(pointNumber, transform.position, transform.rotation);//地点番号、座標、向きをプレイヤーに渡す
        }
    }
}
