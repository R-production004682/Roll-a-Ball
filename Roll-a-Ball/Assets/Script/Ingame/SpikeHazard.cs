using UnityEngine;

/// <summary>
/// Collider と同じオブジェクトでトゲ接触を判定し死亡と接触演出を連携させる
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class SpikeHazard : MonoBehaviour
{
    [Header("接触演出の参照")]
    [Tooltip("トゲ接触時に再生するSpikeContactEffect。未設定の場合は接触エフェクトを再生しません。")]
    [SerializeField]
    private SpikeContactEffect contactEffect;

    [Tooltip("接触エフェクトの固定発生位置。未設定なら接触点などを使用します。通常はPrefabの参照を維持します。")]
    [SerializeField]
    private Transform effectAnchor;

    [Header("接触の受付")]
    [Tooltip("接触の連続受付を抑える時間（秒）。死亡中の多重実行はPlayerRespawnController側でも防止します。")]
    [SerializeField]
    private float contactCooldown = 0.35f;

    private float lastContactTime = float.NegativeInfinity;

    [SerializeField, Min(0f), Tooltip("即死を確定して接触演出を見せた後、暗転を始めるまでの秒数")]
    private float effectDelay = 0.2f;

    /// <summary>
    /// 再有効化時に前回の接触間隔をリセットする
    /// </summary>
    private void OnEnable()
    {
        lastContactTime = float.NegativeInfinity;
    }

    /// <summary>
    /// コンポーネント追加時に子の接触エフェクトを初期設定
    /// </summary>
    private void Reset()
    {
        contactEffect = transform.parent != null
            ? transform.parent.GetComponentInChildren<SpikeContactEffect>(true)
            : GetComponentInChildren<SpikeContactEffect>(true);
    }

    /// <summary>
    /// Player 本体または子の Collider の接触位置から死亡処理を開始する
    /// </summary>
    /// <param name="other">接触したコライダーです。</param>
    private void OnTriggerEnter(Collider other)
    {
        var respawnController = other == null ? null : other.GetComponentInParent<PlayerRespawnController>();
        if (respawnController == null)
        {
            return;
        }

        var contactPosition = effectAnchor != null
            ? effectAnchor.position
            : other.ClosestPoint(transform.position);
        var contactNormal = contactPosition - transform.position;
        HandlePlayerContact(respawnController, contactPosition, contactNormal);
    }

    /// <summary>
    /// 接触中に復帰した場合や受付間隔内の進入でも死亡判定を取りこぼさない
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }

    /// <summary>
    /// 通常の Collider の接触点でも死亡処理を開始する
    /// </summary>
    /// <param name="collision">接触情報です。</param>
    private void OnCollisionEnter(Collision collision)
    {
        var respawnController = collision == null ? null : collision.collider.GetComponentInParent<PlayerRespawnController>();
        if (respawnController == null)
        {
            return;
        }

        var contactPosition = effectAnchor != null ? effectAnchor.position : transform.position;
        var contactNormal = transform.up;
        if (collision.contactCount > 0)
        {
            var contact = collision.GetContact(0);
            contactPosition = effectAnchor != null ? effectAnchor.position : contact.point;
            contactNormal = contact.normal;
        }

        HandlePlayerContact(respawnController, contactPosition, contactNormal);
    }

    /// <summary>
    /// 通常の Collider に接触し続けている場合も復帰後の死亡判定を行う
    /// </summary>
    private void OnCollisionStay(Collision collision)
    {
        OnCollisionEnter(collision);
    }

    /// <summary>
    /// 接触間隔と Player の死亡受付を確認し受理された接触だけ演出を再生する
    /// </summary>
    /// <param name="respawnController">接触したプレイヤーの復帰コンポーネント</param>
    /// <param name="contactPosition">エフェクトを表示する位置です。</param>
    /// <param name="contactNormal">エフェクトの向きです。</param>
    private void HandlePlayerContact(PlayerRespawnController respawnController, Vector3 contactPosition, Vector3 contactNormal)
    {
        if (Time.unscaledTime - lastContactTime < Mathf.Max(0f, contactCooldown) ||
            !respawnController.TryDie(effectDelay, playHitFeedback: true))
        {
            return;
        }

        lastContactTime = Time.unscaledTime;

        if (contactEffect != null)
        {
            contactEffect.gameObject.SetActive(true);
            if (contactNormal.sqrMagnitude < 0.001f)
            {
                contactNormal = transform.up;
            }

            contactEffect.PlayAt(contactPosition, contactNormal.normalized);
        }
    }
}
