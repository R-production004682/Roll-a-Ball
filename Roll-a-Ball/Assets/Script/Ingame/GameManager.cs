using System;
using Roll_a_Ball.OutGame;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;//ゲームマネージャー

    public bool isStageCompleted = false;//ステージ完了フラグ

    [SerializeField, Tooltip("リザルト画面のCanvasを入れる")]
    private GameObject result;//リザルト画面

    [SerializeField]
    private OutGameClearController outGameClearController;

    /// <summary>
    /// このプレイで取得したコインの金額を返す
    /// </summary>
    public int CollectedCoins { get; private set; }

    /// <summary>
    /// このプレイの取得金額が変わったときに通知する
    /// </summary>
    public event Action CollectedCoinsChanged;

    /// <summary>
    /// ゲーム開始時に呼び出され、GameManagerが重複しないようにチェックした上で、instanceに登録
    /// </summary>
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;//このGameManagerをinstanceに登録
    }

    /// <summary>
    /// コインを所持金へ保存し、成功した金額をこのプレイの取得金額に加える
    /// </summary>
    /// <param name="amount">取得する正の金額</param>
    /// <returns>所持金への保存と取得金額の更新に成功した場合は true</returns>
    public bool TryCollectCoin(int amount)
    {
        if (isStageCompleted || amount <= 0 || amount > int.MaxValue - CollectedCoins)
        {
            return false;
        }

        if (!GameDataManager.TryAddCurrency(amount, out _))
        {
            return false;
        }

        CollectedCoins += amount;
        CollectedCoinsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// リザルト画面の参照を検証してゲーム開始時に非表示にする
    /// </summary>
    private void Start()
    {
        if (result == null)
        {
            Debug.LogError("GameManager にリザルト画面が設定されていません。", this);
            return;
        }

        result.SetActive(false);//リザルトオフ
    }

    /// <summary>
    /// シーン破棄時に自身の共有参照を解除する
    /// </summary>
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// ステージをクリア済みにし、リザルト画面を表示する処理
    /// </summary>
    public void StageCompleted()//ステージ完了処理
    {
        if (isStageCompleted)//すでにクリアしている場合
        {
            return;//処理しない
        }

        if (result == null)
        {
            Debug.LogError("GameManager にリザルト画面が設定されていません。", this);
            return;
        }

        isStageCompleted = true;//ステージ完了
        OutGameStateController.Enter(GameFlowState.Cleared);

        if (outGameClearController == null)
        {
            Debug.LogError("GameManager に OutGameClearController が設定されていません。", this);
        }
        else
        {
            outGameClearController.MarkStageCleared();
        }

        result.SetActive(true);//リザルト出す
    }
}
