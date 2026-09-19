using System.Collections.Generic;
using Roll_a_Ball.OutGame;
using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField]
    private float playerSpeed;//プレイヤーの移動速度

    [SerializeField]
    private float mouseSensitivity;//マウス視点操作の感度

    [SerializeField]
    private int fall;//落下地点

    [SerializeField]
    private List<Vector3> respawnPoints = new List<Vector3>();//リスポーン地点

    [SerializeField]
    private List<Quaternion> respawnRotations = new List<Quaternion>();//リスポーンの向き

    [SerializeField]
    private Vector3 startPoint= Vector3.zero;//(スタート地点)

    private void Start()
    {
        respawnPoints.Add(startPoint);//スタート地点をリスポーン地点に追加
        respawnRotations .Add(transform.rotation );//スタート時の向きをリスポーンの向きに追加
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.isStageCompleted == true)
            return;//クリア状態では操作不可

        if (UiInputScope.BlocksPlayer)
            return;//UI画面では操作不可

        if (Input.GetKey(KeyCode.W))//Wキー入力
            transform.position += playerSpeed * transform.forward * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーの正面方向に進む

        if (Input.GetKey(KeyCode.S))//Sキー入力
            transform.position -= playerSpeed * transform.forward * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーが後方に進む

        if (Input.GetKey(KeyCode.D))//Dキー入力
            transform.position += playerSpeed * transform.right * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーが右に進む

        if (Input.GetKey(KeyCode.A))//Aキー入力
            transform.position -= playerSpeed * transform.right * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーが左に進む

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;//マウスの左右移動量を取得
        transform.Rotate(Vector3.up * mouseX);//オブジェクトのy軸を中心に回転

        if (transform.position.y <= fall)
        {
            if (respawnPoints.Count > 0)
            {
                transform.position = respawnPoints [respawnPoints .Count -1];
                transform .rotation = respawnRotations [respawnPoints .Count - 1 ];
            }
        }
    }

    public void SetRespawnPointNumber(int pointNumber, Vector3 position, Quaternion rotation)
    {
        while (respawnPoints.Count <= pointNumber)//地点番号の箱がまだない
        {
            respawnPoints.Add(startPoint);//スタート地点を入れとく
            respawnRotations.Add(transform.rotation);//現在の向きを入れとく
        }

        respawnPoints[pointNumber] = position;//指定した番号に地点座標を登録
        respawnRotations[pointNumber] = rotation;//指定した番号に向きを登録
    }
}
