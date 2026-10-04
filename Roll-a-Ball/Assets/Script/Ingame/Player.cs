using System.Collections.Generic;
using Roll_a_Ball.OutGame;
using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(PlayerRespawnController))]
public class Player : MonoBehaviour
{
    private Rigidbody rb; //Rigidbody

    private Vector3 moveDirection; //現在の移動方向

    private List<CheckPoint> checkPoints = new List<CheckPoint>(); //リスポーン地点

    private bool isJumping = false;//ジャンプ中か

    [Header("移動設定")]

    [SerializeField, Tooltip("通常の最大速度")]
    private float normalMaxSpeed = 20;//通常時の最大速度

    [SerializeField, Tooltip("ジャンプ中の最大速度")]
    private float jumpMaxSpeed = 10;//ジャンプ中の最大速度（水平方向）

    [SerializeField, Tooltip("プレイヤーの加速量")]
    private float acceleration = 50; //加速度

    [Header("地点設定")]

    [SerializeField, Tooltip("落下判定とするy座標")]
    private int fall; //落下地点

    [SerializeField, Tooltip("スタート地点(空オブジェクトで座標を決める)")]
    private CheckPoint startPoint; //スタート地点

    private bool isGoal = false; //ゴールしたか

    [Header("カメラ")]

    [SerializeField, Tooltip("プレイヤーの移動方向を決めるカメラオブジェクト")]
    private Transform cameraTransform; //カメラ（インスペクターから指定）

    private PlayerRespawnController respawnController;

    /// <summary>
    /// 復帰処理中かを取得し接触側が死亡状態を判断できるようにする
    /// </summary>
    public bool IsRespawning => respawnController != null && respawnController.IsRespawning;

    /// <summary>
    /// 生存中でチェックポイントやゴールとの接触を受け付けられるかを取得する
    /// </summary>
    public bool CanReceiveGameplayContact => isActiveAndEnabled && !isGoal && !IsRespawning;

    /// <summary>
    /// 操作と復帰の必須コンポーネントを取得し保存済みシーンにも復帰機能を補う
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        respawnController = GetComponent<PlayerRespawnController>() ?? gameObject.AddComponent<PlayerRespawnController>();
    }

    /// <summary>
    /// 新しい挑戦のチェックポイント登録と表示を初期化する
    /// </summary>
    private void Start()
    {
        ResetCheckpoints();
    }

    /// <summary>
    /// 落下時は死亡処理を開始し生存中かつ操作可能なときだけ移動入力を更新する
    /// </summary>
    private void Update()
    {
        if (CanReceiveGameplayContact && transform.position.y <= fall)
        {
            respawnController.TryDie();
            isJumping = false;//ジャンプ中ではない
        }

        if (UiInputScope.BlocksPlayer || !CanReceiveGameplayContact || cameraTransform == null)
        {
            moveDirection = Vector3.zero;
            return;//UI画面では操作不可
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

    /// <summary>
    /// 生存中の移動加速と最大速度の制限を適用する
    /// </summary>
    private void FixedUpdate()
    {
        if (!CanReceiveGameplayContact || UiInputScope.BlocksPlayer)
        {
            return;
        }
        float currentMaxSpeed = isJumping ? jumpMaxSpeed : normalMaxSpeed;

        if (moveDirection != Vector3.zero)//入力がある
        {
            rb.AddForce(moveDirection * acceleration, ForceMode.Acceleration);//入力方向に加速度分力を加える(質量依存なし)
        }

        var horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);//水平方向の速度取得
        if (horizontalVelocity.magnitude > currentMaxSpeed)//最大速度を超えた
        {
            horizontalVelocity = horizontalVelocity.normalized * currentMaxSpeed;//最大速度にする
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    /// <summary>
    /// 生存中にゴールへ到達した場合だけ移動と物理演算を停止する
    /// </summary>
    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (CanReceiveGameplayContact && other.CompareTag("Goal"))
        {
            isGoal = true;
            Debug.Log("ゴールに触れた");
            rb.isKinematic = true;//物理演算を止める
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;//ジャンプ中ではない
        }
    }

    /// <summary>
    /// 未到達の復活地点を登録し前の地点を通過済みにして到達演出を再生する
    /// </summary>
    /// <param name="point">新たに到達した復活地点</param>
    public void UnlockPoint(CheckPoint point)
    {
        if (!CanReceiveGameplayContact || point == null || checkPoints.Contains(point))//地点番号がないか解放済みだと処理しない
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

    /// <summary>
    /// 最後の有効なチェックポイントの位置と向きを取得する
    /// </summary>
    /// <returns>復帰地点が登録されている場合だけ true</returns>
    public bool TryGetCheckpointPose(out Vector3 position, out Quaternion rotation)
    {
        for (var i = checkPoints.Count - 1; i >= 0; i--)
        {
            if (checkPoints[i] != null)
            {
                position = checkPoints[i].transform.position;
                rotation = checkPoints[i].transform.rotation;
                return true;
            }
        }
        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    /// <summary>
    /// 操作停止時に蓄積した移動入力を消去する
    /// </summary>
    public void ClearMovement()
    {
        moveDirection = Vector3.zero;
    }

    /// <summary>
    /// 操作コンポーネント無効化時も復帰処理を完了する
    /// </summary>
    private void OnDisable()
    {
        if (respawnController != null)
        {
            respawnController.CancelRespawn();
        }
    }

    public void Jump(float jumForce)//ジャンプ台にふれたときの処理
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);//縦方向の移動量をリセット
        rb.AddForce(Vector3.up * jumForce, ForceMode.Impulse);//プレイヤーに上方向の力をのせる
        isJumping = true;//ジャンプ中にする
    }
}
