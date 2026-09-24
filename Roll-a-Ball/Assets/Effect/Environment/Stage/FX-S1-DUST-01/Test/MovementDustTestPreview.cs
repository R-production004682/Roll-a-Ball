using UnityEngine;

/// <summary>
/// EffectTestScene専用のボールを動かし、停止・速度差・空中・リスポーンを巡回確認する
/// </summary>
public sealed class MovementDustTestPreview : MonoBehaviour
{
    [SerializeField] private Transform testBall;
    [SerializeField] private Transform ballVisual;
    [SerializeField] private PlayerMovementEffect movementEffect;
    [SerializeField] private TMPro.TMP_Text statusText;
    [SerializeField] private bool automatic = true;
    [SerializeField, Range(0.25f, 2f)] private float playbackSpeed = 1f;

    private float elapsed;
    private int previousPhase = -1;

    public float Elapsed => elapsed;

    /// <summary>
    /// テスト開始時に専用ボールを開始地点へ戻す
    /// </summary>
    private void OnEnable()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        gameObject.SetActive(false);
        return;
#else
        if (testBall == null || ballVisual == null || movementEffect == null || statusText == null)
        {
            Debug.LogError("MovementDustTestPreviewのボール、表示モデル、移動FX、状態表示を設定してください。", this);
            enabled = false;
            return;
        }

        Restart();
#endif
    }

    /// <summary>
    /// 自動巡回を先頭へ戻し、リスポーン時と同じ粒子消去APIを呼び出す
    /// </summary>
    public void Restart()
    {
        if (testBall == null || movementEffect == null)
        {
            return;
        }

        elapsed = 0f;
        previousPhase = -1;
        testBall.localPosition = new Vector3(-3.4f, 0.69f, 0f);
        movementEffect.ResetEffect();
    }

    /// <summary>
    /// テスト専用のTransformを更新し、実際の接地判定に渡す
    /// </summary>
    private void Update()
    {
        if (!automatic)
        {
            return;
        }

        elapsed += Time.deltaTime * playbackSpeed;
        if (elapsed >= 15f)
        {
            Restart();
        }

        var before = testBall.localPosition;
        var position = new Vector3(-3.4f, 0.69f, 0f);
        var phase = 0;
        var label = "停止：粒子なし";
        if (elapsed >= 1f && elapsed < 5f)
        {
            phase = 1;
            position.x = Mathf.Lerp(-3.4f, -0.6f, (elapsed - 1f) / 4f);
            label = "草地 / 低速：短い草片と控えめな粉塵";
        }
        else if (elapsed >= 5f && elapsed < 6f)
        {
            phase = 2;
            position.x = Mathf.Lerp(-0.6f, 3.4f, elapsed - 5f);
            label = "草 → 土 / 加速：粉塵と草片から、砂埃と小石へ切替";
        }
        else if (elapsed >= 6f && elapsed < 8f)
        {
            phase = 3;
            position.x = 3.4f;
            label = "土 / 停止：小石と粉塵が短い余韻で消滅";
        }
        else if (elapsed >= 8f && elapsed < 10f)
        {
            phase = 4;
            var progress = (elapsed - 8f) / 2f;
            position.x = Mathf.Lerp(3.4f, 0.8f, progress);
            position.y += 1.8f * 4f * progress * (1f - progress);
            label = "ジャンプ → 落下：新しい粉塵なし";
        }
        else if (elapsed >= 10f && elapsed < 12f)
        {
            phase = 5;
            position.x = Mathf.Lerp(0.8f, -2.8f, (elapsed - 10f) / 2f);
            label = "着地 / 逆方向：進行方向の後方へ放出";
        }
        else if (elapsed >= 12f && elapsed < 13f)
        {
            phase = 6;
            position.x = -2.8f;
            label = "停止：残留チェック";
        }
        else if (elapsed >= 13f)
        {
            phase = 7;
            label = "リスポーン：粒子と移動履歴をリセット";
        }

        testBall.localPosition = position;
        var distance = position - before;
        var horizontal = Vector3.ProjectOnPlane(distance, Vector3.up);
        if (horizontal.sqrMagnitude > 0.00001f && horizontal.magnitude < 1f)
        {
            ballVisual.Rotate(Vector3.Cross(Vector3.up, horizontal).normalized,
                horizontal.magnitude / 0.5f * Mathf.Rad2Deg, Space.World);
        }

        if (phase == previousPhase)
        {
            return;
        }

        previousPhase = phase;
        statusText.text = "移動粉塵  /  FX-S1-DUST-01\n" + label
            + "\n左：Grass（草片）    右：Dirt / Dart（土・小石）\n15秒で自動巡回 / 再クリックで終了";
        if (phase == 7)
        {
            movementEffect.ResetEffect();
        }
    }
}
