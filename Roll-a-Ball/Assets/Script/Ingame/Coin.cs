using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField, Tooltip("取得時に増えるコインの金額")]
    private int coinAmount = 10;//コイン1枚の保有する金額

    [SerializeField, Tooltip("取得した場所で再生するエフェクトのPrefab")]
    private CurrencyPickupEffect pickupEffectPrefab;

    [SerializeField, Tooltip("大きいコインではオンにして、光と粒子を増やす")]
    private bool isLargeCoin;

    [SerializeField, Tooltip("モデルの正面に閃光を合わせる角度。通常は0")]
    private Vector3 flashLocalEulerAngles;

    private bool collected;

    /// <summary>
    /// プレイヤーがコインに衝突した際に所持コインを加算し、衝突したコインは消える処理
    /// </summary>
    /// <param name="other">接触したコライダー</param>
    private void OnTriggerEnter(Collider other)//衝突判定
    {
        if (collected || !other.CompareTag("Player"))
        {
            return;
        }

        collected = true;
        PlayerPrefs.SetInt("HasCoin", PlayerPrefs.GetInt("HasCoin", 0) + coinAmount);//所持コインにコインの金額を加算
        PlayerPrefs.Save();//保存確定

        PlayPickupEffect();
        Destroy(gameObject);//コイン消える
    }

    /// <summary>
    /// コインの位置に独立したエフェクトを生成し、大小に応じて再生する
    /// </summary>
    private void PlayPickupEffect()
    {
        if (pickupEffectPrefab == null)
        {
            Debug.LogWarning("Coin: 取得エフェクトのPrefabを設定してください。", this);
            return;
        }

        var effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        effect.Play(isLargeCoin, transform.rotation * Quaternion.Euler(flashLocalEulerAngles));
    }
}
