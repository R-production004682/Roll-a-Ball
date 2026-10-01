using System;
using UnityEngine;
[Serializable]

public class CheckPoint : MonoBehaviour
{
    [SerializeField, Tooltip("到達時に再生するFX-C-CP-01。同じGameObjectにある場合は自動取得します")]
    private CheckpointBurstEffect checkpointEffect;

    /// <summary>
    /// 明示的な参照がない場合は同じGameObjectの到達演出を取得する
    /// </summary>
    private void Awake()
    {
        if (checkpointEffect == null)
        {
            checkpointEffect = GetComponent<CheckpointBurstEffect>();
        }
    }

    /// <summary>
    /// Player本体または子のColliderの接触で復活地点を登録する
    /// </summary>
    /// <param name="other">進入したCollider</param>
    private void OnTriggerEnter(Collider other)//衝突判定
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }
        player.UnlockPoint(this);
    }

    /// <summary>
    /// 初回到達の破裂を再生し終了後もチェックポイントの矢印を残す
    /// </summary>
    public void PlayCheckpointEffect()
    {
        if (checkpointEffect == null)
        {
            return;
        }
        checkpointEffect.gameObject.SetActive(true);
        checkpointEffect.Play();
    }

    /// <summary>
    /// 再生せず通過済みや未到達の表示へ切り替え残留粒子を消去する
    /// </summary>
    /// <param name="state">次に表示するチェックポイント状態</param>
    public void SetCheckpointEffectState(CheckpointBurstEffect.CheckpointState state)
    {
        if (checkpointEffect == null)
        {
            return;
        }
        checkpointEffect.gameObject.SetActive(true);
        checkpointEffect.SetState(state);
    }
}
