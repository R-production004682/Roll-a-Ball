using Roll_a_Ball.OutGame;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;//ゲームマネージャー

    [SerializeField]
    public bool isStageCompleted = false;//ステージ完了フラグ

    [SerializeField]
    private GameObject result;//リザルト画面

    [SerializeField]
    private OutGameClearController outGameClearController;

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
    /// ゲーム開始時リザルト画面(クリアやゲームオーバー画面)を隠す処理
    /// </summary>
    void Start()
    {
        result.SetActive(false);//リザルトオフ
    }

    /// <summary>
    /// ステージをクリア済みにし、リザルト画面を表示する処理
    /// </summary>
    public void StageCompleted()//ステージ完了処理
    {
        if (isStageCompleted)//すでにクリアしている場合
            return;//処理しない

        isStageCompleted = true;//ステージ完了

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
