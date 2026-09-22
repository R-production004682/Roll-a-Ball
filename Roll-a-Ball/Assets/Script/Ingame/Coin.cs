using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField]
    private int coinAmount = 10;//コイン1枚の保有する金額

    /// <summary>
    /// プレイヤーがコインに衝突した際に所持コインを加算し、衝突したコインは消える処理
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (other.CompareTag("Player"))//プレイヤーと衝突した場合
        {
            PlayerPrefs.SetInt("HasCoin", PlayerPrefs.GetInt("HasCoin", 0) + coinAmount);//所持コインにコインの金額を加算
            coinAmount = 0;//コインの保有する金額を0にする
            PlayerPrefs.Save();//保存確定

            Debug.Log("保存されたコイン：" + PlayerPrefs.GetInt("HasCoin", 0));//保存されているコインを確認

            Destroy(gameObject);//コイン消える
        }
    }
}
