using UnityEngine;

public class Respawnpoint : MonoBehaviour
{
    [SerializeField]
    [Min(0)]
    private int pointNumber;//リスポーン地点の番号

    /// <summary>
    /// 中間地点で復活するための処理
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (other.CompareTag("Player"))//プレイヤーとの衝突
        {
            Player player = other.GetComponent<Player>();
            player.UnlockPoint(this);//地点番号、座標、向きをプレイヤーに渡す
        }
    }
}
