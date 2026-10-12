using UnityEngine;

public class Birdsong : MonoBehaviour
{
    [SerializeField]
    private Transform cameramain;//メインカメラの位置

    [SerializeField,Tooltip("カメラとの最大距離")]
    private float maxDistance = 30f;//折り返しの最大距離

    [SerializeField]
    private float turnDistance = 30f;//折り返し距離

    [SerializeField]
    private bool movingUp = true;//上昇中か？

    [SerializeField,Tooltip("音の移動速度")]
    private float moveSpeed = 5;//音の移動速度

    // Update is called once per frame
    void Update()
    {
        if (cameramain == null)//カメラがセットされていない場合処理しない
            return;

        Vector3 cameraPosition = cameramain.position;//カメラの座標を取得
        transform.position = new Vector3(cameraPosition.x, transform.position.y, cameraPosition.z);//ｘ座標とｚ座標をカメラとあわせる

        float distance = transform.position.y - cameraPosition.y;//カメラとの距離

        if(distance >= turnDistance)//カメラとの距離が折り返し距離より遠い
        {
            movingUp = false;//上昇中ではない
        }

        if(distance <= 0 && movingUp == false)//カメラとの距離が0以下
        {
            movingUp = true;//上昇中にする
            turnDistance = Random.Range(0f, maxDistance);//次の折り返し地点を決める
        }

        if (movingUp == true)//上昇中
        {
            transform.Translate(0f, +moveSpeed * Time.deltaTime, 0f, Space.World);//上に移動する
        }

        else
        {
            transform.Translate(0f, -moveSpeed * Time.deltaTime, 0f, Space.World);//下に移動する
        }
    }
}
