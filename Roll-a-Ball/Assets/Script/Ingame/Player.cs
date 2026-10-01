using System.Collections.Generic;
using Roll_a_Ball.OutGame;
using UnityEngine;


public class Player : MonoBehaviour
{
    private Rigidbody rb; //Rigidbody

    private Vector3 moveDirection; //現在の移動方向

    private List<CheckPoint> checkPoints = new List<CheckPoint>(); //リスポーン地点

    [Header("移動設定")]
    [SerializeField, Tooltip("プレイヤーの最大移動速度")]
    private float playerSpeed = 20; //プレイヤーの最大移動速度

    [SerializeField, Tooltip("プレイヤーの加速量")]
    private float acceleration = 50; //加速度

    [SerializeField, Tooltip("落下判定とするy座標")]
    private int fall; //落下地点

    [SerializeField, Tooltip("スタート地点(空オブジェクトで座標を決める)")]
    private CheckPoint startPoint; //スタート地点

    private bool isGoal = false; //ゴールしたか

    [Header("カメラ")]

    [SerializeField, Tooltip("プレイヤーの移動方向を決めるカメラオブジェクト")]
    private Transform cameraTransform; //カメラ（インスペクターから指定）

    /// <summary>
    /// Rigidbodyを取得し新しい挑戦のチェックポイント登録と表示を初期化する
    /// </summary>
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        ResetCheckpoints();
    }

    /// <summary>
    /// 落下時は最後の復活地点へ戻しプレイ中だけ移動入力を更新する
    /// </summary>
    void Update()
    {
        if (transform.position.y <= fall && checkPoints.Count > 0)//解放した地点の位置と向きに戻る
        {
            transform.position = checkPoints[checkPoints.Count - 1].transform.position;
            transform.rotation = checkPoints[checkPoints.Count - 1].transform.rotation;
            rb.linearVelocity = Vector3.zero;//速度をリセット
            moveDirection = Vector3.zero;//方向をリセット
            rb.angularVelocity = Vector3.zero;//回転をリセット
        }

        if (UiInputScope.BlocksPlayer)
        {
            return;//UI画面では操作不可
        }

        if (isGoal == true)
        {
            return;//ゴールに触れると操作不可
        }

        var cameraForward = cameraTransform.forward;//カメラの前方向を取得
        var cameraRight = cameraTransform.right;//カメラの右方向を取得

        cameraForward.y = 0f;//上下を無視
        cameraRight.y = 0f;

        moveDirection = Vector3.zero;//移動リセット

        if (Input.GetKey(KeyCode.W))//Wキー入力
        {
            moveDirection += cameraForward;//正面方向
        }

        if (Input.GetKey(KeyCode.S))//Sキー入力
        {
            moveDirection -= cameraForward;//後方
        }

        if (Input.GetKey(KeyCode.D))//Dキー入力
        {
            moveDirection += cameraRight;//右
        }

        if (Input.GetKey(KeyCode.A))//Aキー入力
        {
            moveDirection -= cameraRight;//左
        }

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
    /// 未到達の復活地点を登録し前の地点を通過済みにして到達演出を再生する
    /// </summary>
    /// <param name="point">新たに到達した復活地点</param>
    public void UnlockPoint(CheckPoint point)
    {
        if (point == null || checkPoints.Contains(point))//地点番号がないか解放済みだと処理しない
        {
            return;
        }

        if (checkPoints.Count > 0)
        {
            var previous = checkPoints[checkPoints.Count - 1];
            if (previous != null)
            {
                previous.SetCheckpointEffectState(CheckpointBurstEffect.CheckpointState.Visited);
            }
        }
        checkPoints.Add(point);//チェックポイントを追加する
        point.PlayCheckpointEffect();
    }

    /// <summary>
    /// 新しい挑戦用に到達済みの表示と登録を消去してスタート地点だけを残す
    /// </summary>
    public void ResetCheckpoints()
    {
        foreach (var point in checkPoints)
        {
            if (point != null)
            {
                point.SetCheckpointEffectState(CheckpointBurstEffect.CheckpointState.Unvisited);
            }
        }
        checkPoints.Clear();
        if (startPoint != null)
        {
            checkPoints.Add(startPoint);
        }
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
