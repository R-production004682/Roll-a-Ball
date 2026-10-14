using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField, Tooltip("プレイヤーに与えるジャンプ力")]
    private float jumpForce = 10;//ジャンプするときに与える力

    [SerializeField, Tooltip("ジャンプ台のクールタイム")]
    private float jumpcool = 0.03f;

    // 経過した時間
    private float elapsedTime;

    /// <summary>
    /// ジャンプ台を再使用できるまでの経過時間を更新する
    /// </summary>
    private void Update()
    {
        if (elapsedTime < jumpcool)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// クールタイムを満たした接触でプレイヤーを跳ね上げる
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (elapsedTime < jumpcool)
        {
            return;
        }
        elapsedTime = 0f;

        if (!other.CompareTag("Player"))//プレイヤー以外との衝突の場合処理しない
        {
            return;
        }
        var player = other.GetComponent<Player>();//プレイヤー取得

        if (player != null)
        {
            player.Jump(jumpForce);//プレイヤーのジャンプ処理を行う
        }
        else
        {
            Debug.LogWarning($"JumpPad: Player タグの接触オブジェクト {other.name} に Player コンポーネントがないためジャンプできません。", other);
        }
    }
}
