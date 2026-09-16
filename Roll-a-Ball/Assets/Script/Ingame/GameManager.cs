using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;//ゲームマネージャー

    [SerializeField]
    public bool isStageCompleted = false;//ステージ完了フラグ

    [SerializeField]
    private GameObject result;//リザルト画面

    [SerializeField]//確認用
    private float playTime = 0;//プレイタイム

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;//このGameManagerをinstanceに登録
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        result.SetActive(false);//リザルトオフ
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.isStageCompleted)//クリアしている場合
            return;//タイマーを止める

        playTime += Time.deltaTime;//プレイタイム→１秒に１のペースで増える
    }

    public void StageCompleted()//ステージ完了処理
    {
        if (isStageCompleted)//すでにクリアしている場合
            return;//処理しない

        isStageCompleted = true;//ステージ完了

        Debug.Log("クリアタイム：" + playTime.ToString("F2"));//クリアタイムを小数点以下2桁で表示

        PlayerPrefs.SetFloat("ThisPlayTime", playTime);//今回のタイムを記録
        PlayerPrefs.Save();//保存
        result.SetActive(true);//リザルト出す
    }
}
