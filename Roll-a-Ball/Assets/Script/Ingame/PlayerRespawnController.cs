using System;
using System.Collections;
using Roll_a_Ball.OutGame;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーの死亡受付、暗転中の復帰と入力・物理状態の復元を管理する
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerRespawnController : MonoBehaviour
{
    [SerializeField, Tooltip("リスポーン時に位置の飛びを通知する追従カメラ")]
    private CinemachineVirtualCameraBase followCamera;

    [SerializeField, Tooltip("被弾時に再生する追従カメラの演出")]
    private HitCameraEffect hitCameraEffect;

    [SerializeField, Tooltip("死亡時にプレイヤーの表示を断面付きの破片へ切り替える演出")]
    private PlayerFracturePresentation fracturePresentation;

    private Player player;
    private Rigidbody rb;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isKinematic;
    private Coroutine deathRoutine;
    private FadeTransition respawnFade;
    private IDisposable deathInputLock;

    /// <summary>
    /// 死亡から暗転解除までの復帰処理中かを取得する
    /// </summary>
    public bool IsRespawning { get; private set; }

    /// <summary>
    /// 同一オブジェクトの操作と物理参照および初期復帰位置を保持する
    /// </summary>
    private void Awake()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    /// <summary>
    /// 死亡を即時確定し操作と物理演算を止めて暗転による復帰を一度だけ開始する
    /// </summary>
    /// <param name="effectDelay">接触演出を見せてから暗転を始めるまでの秒数</param>
    /// <param name="playHitFeedback">トゲ接触等の被弾時にカメラ演出も再生するか</param>
    /// <returns>死亡を新たに受け付けた場合だけ true</returns>
    public bool TryDie(float effectDelay = 0f, bool playHitFeedback = false)
    {
        if (!isActiveAndEnabled || player == null || !player.CanReceiveGameplayContact || FadeTransition.IsTransitioning)
        {
            return false;
        }

        IsRespawning = true;
        isKinematic = rb.isKinematic;
        player.ClearMovement();
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        deathInputLock = UiInputScope.BlockInput();
        if (playHitFeedback && hitCameraEffect != null)
        {
            hitCameraEffect.Play();
        }
        if (fracturePresentation != null)
        {
            effectDelay = Mathf.Max(effectDelay, fracturePresentation.Play());
        }
        deathRoutine = StartCoroutine(RespawnRoutine(Mathf.Max(0f, effectDelay)));
        return true;
    }

    /// <summary>
    /// 接触演出の後に完全暗転中の復帰を実行し明転後に操作を戻す
    /// </summary>
    private IEnumerator RespawnRoutine(float effectDelay)
    {
        // 接触側のエフェクト再生が終わってからフェードを準備する
        yield return new WaitForSecondsRealtime(effectDelay);
        if (respawnFade == null)
        {
            respawnFade = FadeTransition.Create(transform);
        }
        respawnFade.gameObject.SetActive(true);
        if (!respawnFade.Play(RespawnCheckPoint, FinishRespawn))
        {
            Debug.LogWarning("死亡時の暗転を開始できないため復帰位置へ直接戻します", this);
            RespawnCheckPoint();
            FinishRespawn();
        }
        deathRoutine = null;
    }

    /// <summary>
    /// 最後の有効な復活地点の位置と向きに戻り追従カメラへ移動量を通知する
    /// </summary>
    private void RespawnCheckPoint()
    {
        if (fracturePresentation != null)
        {
            fracturePresentation.Stop();
        }
        if (hitCameraEffect != null)
        {
            hitCameraEffect.Stop();
        }
        var position = initialPosition;
        var rotation = initialRotation;
        if (player.TryGetCheckpointPose(out var checkpointPosition, out var checkpointRotation))
        {
            position = checkpointPosition;
            rotation = checkpointRotation;
        }

        var positionDelta = position - transform.position;
        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
        if (followCamera != null)
        {
            followCamera.OnTargetObjectWarped(transform, positionDelta);
        }
        player.ClearMovement();//方向をリセット
    }

    /// <summary>
    /// 暗転解除後に死亡状態と入力ロックを解除し元の物理状態に戻す
    /// </summary>
    private void FinishRespawn()
    {
        if (!IsRespawning)
        {
            return;
        }
        rb.isKinematic = isKinematic;
        IsRespawning = false;
        deathInputLock?.Dispose();
        deathInputLock = null;
    }

    /// <summary>
    /// 死亡中の無効化でも復帰と暗転解除を行い操作ロックを残さない
    /// </summary>
    public void CancelRespawn()
    {
        if (!IsRespawning)
        {
            return;
        }
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
        RespawnCheckPoint();
        if (respawnFade != null)
        {
            respawnFade.gameObject.SetActive(false);
        }
        FinishRespawn();
    }

    /// <summary>
    /// コンポーネント無効化時に復帰を完了し入力ロックを残さない
    /// </summary>
    private void OnDisable()
    {
        CancelRespawn();
    }
}
