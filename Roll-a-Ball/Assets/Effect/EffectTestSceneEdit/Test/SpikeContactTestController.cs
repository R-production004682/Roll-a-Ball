using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EffectTestSceneでトゲ接触ギミックを繰り返し確認するための制御です
/// </summary>
public sealed class SpikeContactTestController : MonoBehaviour
{
    [SerializeField]
    private Button testButton;

    [SerializeField]
    private GameObject previewRoot;

    [SerializeField]
    private SpikeContactTestPlayerDriver testPlayer;

    /// <summary>
    /// テストボタンのクリック処理を登録
    /// </summary>
    private void OnEnable()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(PlayTest);
        }
    }

    /// <summary>
    /// テストボタンのクリック処理を解除してプレビューを非表示化
    /// </summary>
    private void OnDisable()
    {
        if (testButton != null)
        {
            testButton.onClick.RemoveListener(PlayTest);
        }

        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }

    /// <summary>
    /// トゲのプレビューを有効化してPlayerを接触位置へ移動
    /// </summary>
    private void PlayTest()
    {
        if (previewRoot == null || testPlayer == null)
        {
            Debug.LogWarning("トゲ接触テストの参照が設定されていません。", this);
            return;
        }

        previewRoot.SetActive(true);
        testPlayer.BeginTest();
    }
}

/// <summary>
/// トゲ接触テスト用のPlayer移動を管理します
/// </summary>
public sealed class SpikeContactTestPlayerDriver : MonoBehaviour
{
    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private Vector3 startPosition;

    [SerializeField]
    private Vector3 targetPosition;

    [SerializeField]
    private float moveSpeed = 3f;

    private bool isMoving;

    /// <summary>
    /// テスト開始位置へ戻してトゲへ移動開始
    /// </summary>
    public void BeginTest()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (body == null)
        {
            return;
        }

        body.position = startPosition;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        isMoving = true;
    }

    /// <summary>
    /// テスト用Playerをトゲ方向へ移動
    /// </summary>
    private void FixedUpdate()
    {
        if (!isMoving || body == null)
        {
            return;
        }

        var nextPosition = Vector3.MoveTowards(
            body.position,
            targetPosition,
            Mathf.Max(0f, moveSpeed) * Time.fixedDeltaTime);
        body.MovePosition(nextPosition);

        if (nextPosition == targetPosition)
        {
            isMoving = false;
        }
    }

    /// <summary>
    /// トゲに接触したらテスト移動を停止
    /// </summary>
    /// <param name="other">接触したコライダーです。</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<SpikeHazard>() == null)
        {
            return;
        }

        isMoving = false;
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
