using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultButton : MonoBehaviour
{
    public void GoToStageSelect()//ステージセレクトへ移動する
    {
        SceneManager.LoadScene("StageSelectScene");//ステージセレクトシーンに移動
    }

    public void Retry()//リトライする
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);//現在のシーンを読み込む
    }
}
