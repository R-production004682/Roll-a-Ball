using UnityEngine;

/// <summary>
/// Player が接触したときに即時リスポーンさせるトゲギミックです
/// </summary>
public sealed class SpikeHazard : MonoBehaviour
{
    [SerializeField]
    private SpikeContactEffect contactEffect;

    [SerializeField]
    private Transform effectAnchor;

    [SerializeField]
    private float contactCooldown = 0.35f;

    private float lastContactTime = float.NegativeInfinity;
    private bool isProcessing;

    /// <summary>
    /// コンポーネント追加時に子の接触エフェクトを初期設定
    /// </summary>
    private void Reset()
    {
        contactEffect = GetComponentInChildren<SpikeContactEffect>(true);
    }

    /// <summary>
    /// Player との接触時にエフェクトを再生してリスポーン
    /// </summary>
    /// <param name="other">接触したコライダーです。</param>
    private void OnTriggerEnter(Collider other)
    {
        var player = other == null ? null : other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        var contactPosition = effectAnchor != null
            ? effectAnchor.position
            : other.ClosestPoint(transform.position);
        var contactNormal = contactPosition - transform.position;
        HandlePlayerContact(player, contactPosition, contactNormal);
    }

    /// <summary>
    /// 通常のCollider接触時にもPlayerをリスポーン
    /// </summary>
    /// <param name="collision">接触情報です。</param>
    private void OnCollisionEnter(Collision collision)
    {
        var player = collision == null ? null : collision.collider.GetComponentInParent<Player>();
        if (player == null)
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

        HandlePlayerContact(player, contactPosition, contactNormal);
    }

    /// <summary>
    /// 接触間隔を確認してエフェクト再生とリスポーンを一度だけ実行
    /// </summary>
    /// <param name="player">接触したPlayerです。</param>
    /// <param name="contactPosition">エフェクトを表示する位置です。</param>
    /// <param name="contactNormal">エフェクトの向きです。</param>
    private void HandlePlayerContact(Player player, Vector3 contactPosition, Vector3 contactNormal)
    {
        if (isProcessing || Time.time - lastContactTime < Mathf.Max(0f, contactCooldown))
        {
            return;
        }

        isProcessing = true;
        lastContactTime = Time.time;

        if (contactEffect != null)
        {
            if (contactNormal.sqrMagnitude < 0.001f)
            {
                contactNormal = transform.up;
            }

            contactEffect.PlayAt(contactPosition, contactNormal.normalized);
        }
    }
}
