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
    private List<Respawnpoint> respawnPoints = new List<Respawnpoint>();//リスポーン地点

    [SerializeField]
    private Vector3 startPoint;//スタート地点

    /// <summary>
    /// プレイヤーとカメラを動かし、クリア状態、UI画面では操作できないようにする。倒れた場合スタート地点か行き解放されたリスポーン位置にワープする。
    /// </summary>
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
            Respawn();
        }
    }

    /// <summary>
    /// プレイヤーが中間地点に触れたときに、復活位置を中間地点に登録する
    /// </summary>
    /// <param name="point"></param>
    public void UnlockPoint(Respawnpoint point)
    {
        if (point == null || respawnPoints.Contains(point))//地点番号がないか解放済みだと処理しない
            return;

        respawnPoints.Add(point);

    }
}
