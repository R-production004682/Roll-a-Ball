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
    private int fall;//落下地点

    [SerializeField]
    private List<Respawnpoint> respawnPoints = new List<Respawnpoint>();//リスポーン地点

    [SerializeField]
    private Respawnpoint startPoint;//スタート地点

    [SerializeField]
    private bool isGoal = false;//ゴールしたか

    [SerializeField]
    private Transform cameraTransform;//カメラ（インスペクターから指定）

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
        if (transform.position.y <= fall)//リスポーンポイントがあると最後に解放した地点の位置と向きに戻る、移動を0にする
        {
            transform.position = respawnPoints[respawnPoints.Count - 1].transform.position;
            transform.rotation = respawnPoints[respawnPoints.Count - 1].transform.rotation;
            rb.linearVelocity = Vector3.zero;//速度をリセット
            moveDirection = Vector3.zero;//方向をリセット
            rb.angularVelocity = Vector3.zero;//回転をリセット
        }

        if (UiInputScope.BlocksPlayer)
            return;//UI画面では操作不可

        if (isGoal == true)
            return;//ゴールに触れると操作不可

        Vector3 cameraForward = cameraTransform.forward;//カメラの前方向を取得
        Vector3 cameraRight = cameraTransform.right;//カメラの右方向を取得

        cameraForward.y = 0f;//上下を無視
        cameraRight.y = 0f;

        moveDirection = Vector3.zero;//移動リセット

        if (Input.GetKey(KeyCode.W))//Wキー入力
            moveDirection += cameraForward;//正面方向

        if (Input.GetKey(KeyCode.S))//Sキー入力
            moveDirection -= cameraForward;//後方

        if (Input.GetKey(KeyCode.D))//Dキー入力
            moveDirection += cameraRight;//右

        if (Input.GetKey(KeyCode.A))//Aキー入力
            moveDirection -= cameraRight;//左

        if (moveDirection != Vector3.zero)//入力がある
        {
            moveDirection.Normalize();//斜め移動が速くならない
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

        respawnPoints.Add(point);//リスポーンポイントを追加する

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            isGoal = true;
            Debug.Log("ゴールに触れた");
            rb.isKinematic = true;//物理演算を止める
        }
    }
}
