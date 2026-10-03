using UnityEngine;

/// <summary>
/// プレイヤーの表示を破砕エフェクトへ切り替え、復帰時に元の表示へ戻す
/// </summary>
public sealed class PlayerFracturePresentation : MonoBehaviour
{
    [SerializeField] private MeshFilter sourceMesh;
    [SerializeField] private MeshRenderer sourceRenderer;
    [SerializeField] private PlayerFractureEffect effectPrefab;

    private PlayerFractureEffect effect;
    private bool hasHiddenSource;
    private bool sourceWasEnabled;

    /// <summary>
    /// 原オブジェクトの Transform と Collider を変えずに割れる表示へ切り替える
    /// </summary>
    /// <returns>再生できた場合は暗転まで演出を見せる秒数、設定不足の場合は 0</returns>
    public float Play()
    {
        Stop();
        if (!isActiveAndEnabled || sourceMesh == null || sourceRenderer == null || effectPrefab == null)
        {
            Debug.LogWarning("PlayerFracturePresentation: プレイヤーの MeshFilter、Renderer、破砕 Prefab を設定してください", this);
            return 0f;
        }
        if (effect == null)
        {
            effect = Instantiate(effectPrefab);
            effect.name = "PlayerFracturePresentation";
        }
        var material = sourceRenderer.sharedMaterial;
        var color = material != null && material.HasProperty("_BaseColor")
            ? material.GetColor("_BaseColor") : Color.gray;
        if (!effect.Play(sourceMesh.sharedMesh, sourceMesh.transform, color, sourceRenderer.bounds.min.y))
        {
            return 0f;
        }
        sourceWasEnabled = sourceRenderer.enabled;
        sourceRenderer.enabled = false;
        hasHiddenSource = true;
        return effect.VisibleDuration;
    }

    /// <summary>
    /// 破片を消して再生前のプレイヤー表示状態を復元する
    /// </summary>
    public void Stop()
    {
        if (effect != null)
        {
            effect.Stop();
            effect.gameObject.SetActive(false);
        }
        if (hasHiddenSource && sourceRenderer != null)
        {
            sourceRenderer.enabled = sourceWasEnabled;
        }
        hasHiddenSource = false;
    }

    /// <summary>
    /// コンポーネント無効化時も表示の差し替えを解除する
    /// </summary>
    private void OnDisable()
    {
        Stop();
    }

    /// <summary>
    /// 独立した演出オブジェクトもプレイヤーと一緒に破棄する
    /// </summary>
    private void OnDestroy()
    {
        if (effect != null)
        {
            Destroy(effect.gameObject);
        }
    }
}
