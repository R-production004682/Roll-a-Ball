using System.Collections.Generic;
using Roll_a_Ball.OutGame;
using UnityEngine;


public class Player : MonoBehaviour
{
    private Rigidbody rb;//Rigidbody

    private Vector3 moveDirection;//現在の移動方向

    [SerializeField]
    private float playerSpeed = 20;//プレイヤーの最大移動速度

    [SerializeField]
    private float acceleration = 50;//加速度

    [SerializeField]
    private float mouseSensitivity = 200;//マウス視点操作の感度

    [SerializeField]
    private int fall;//落下地点

    [SerializeField]
    private List<Respawnpoint> respawnPoints = new List<Respawnpoint>();//リスポーン地点

    [SerializeField]
    private Respawnpoint startPoint;//スタート地点

    [SerializeField]
    private bool isGoal = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        respawnPoints.Add(startPoint);
    }

    /// <summary>
    /// プレイヤーとカメラを動かし、クリア状態、UI画面では操作できないようにする。倒れた場合スタート地点か行き解放されたリスポーン位置にワープする。
    /// </summary>
    void Update()
    {
        if (UiInputScope.BlocksPlayer)
            return;//UI画面では操作不可

        moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))//Wキー入力
            moveDirection += transform.forward;//正面方向

        if (Input.GetKey(KeyCode.S))//Sキー入力
            moveDirection -= transform.forward;//後方

        if (Input.GetKey(KeyCode.D))//Dキー入力
            moveDirection += transform.right;//右

        if (Input.GetKey(KeyCode.A))//Aキー入力
            moveDirection -= transform.right;//左

        if (moveDirection != Vector3.zero)//入力がある
        {
            moveDirection.Normalize();//斜め移動が速くならない
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;//マウスの左右移動量を取得
        transform.Rotate(Vector3.up * mouseX);//オブジェクトのy軸を中心に回転

        if (transform.position.y <= fall)//リスポーンポイントがあると最後に解放した地点の位置と向きに戻る、移動を0にする
        {
            transform.position = respawnPoints[respawnPoints.Count - 1].transform.position;
            transform.rotation = respawnPoints[respawnPoints.Count - 1].transform.rotation;
            rb.linearVelocity = Vector3.zero;//移動をリセット
        }
    }

    private void FixedUpdate()
    {
        if (isGoal == true)
            return;//ゴールに触れると操作不可

        if (moveDirection != Vector3.zero)//入力がある
        {
            rb.AddForce(moveDirection * acceleration, ForceMode.Acceleration);//入力方向に加速度分力を加える(質量依存なし)
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);//水平方向の速度取得
        if (horizontalVelocity.magnitude > playerSpeed)//最大速度を超えた
        {
            horizontalVelocity = horizontalVelocity.normalized * playerSpeed;//最大速度にする
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            isGoal = true;
            Debug.Log("ゴールに触れた");
            rb.linearVelocity = Vector3.zero;//移動をリセット
        }
    }
}
