using System;
using System.Collections.Generic;
using UnityEngine;
using Roll_a_Ball.OutGame;


public class Player : MonoBehaviour
{
    [SerializeField]
    private float Playerspeed;//プレイヤーの移動速度

    [SerializeField]
    private float mouseSensitivity;//マウス視点操作の感度

    [SerializeField]
    private int fall;//落下地点

    [SerializeField]
    private Vector3 respawnPoint;//リスポーン地点
 


    // Update is called once per frame
    void Update()
    {
        //if (GameManager.instance.isClear == true)
        //return;//クリア状態では操作不可

        if (UiInputScope.BlocksPlayer)
            return;//UI画面では操作不可

        if (Input.GetKey(KeyCode.W))//Wキー入力
            transform.position += Playerspeed * transform.forward * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーの正面方向に進む

        if (Input.GetKey(KeyCode.S))//Sキー入力
            transform.position -= Playerspeed * transform.forward * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーが後方に進む

        if (Input.GetKey(KeyCode.D))//Dキー入力
            transform.position += Playerspeed * transform.right * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーが右に進む

        if (Input.GetKey(KeyCode.A))//Aキー入力
            transform.position -= Playerspeed * transform.right * Time.deltaTime;//1秒ごとにPlayerspeedの値だけプレイヤーが左に進む

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;//マウスの左右移動量を取得
        transform.Rotate(Vector3.up * mouseX);//オブジェクトのy軸を中心に回転

        if (transform.position.y <= fall)
            transform.position = respawnPoint;//落下地点以下にいるとリスポーン地点に戻る

    }
}
