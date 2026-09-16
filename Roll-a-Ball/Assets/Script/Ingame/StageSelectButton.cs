using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectButton : MonoBehaviour
{
    public void GoToStageSelect()//ステージセレクトへ移動する
    {
        SceneManager.LoadScene("StageSelectScene");//ステージセレクトシーンに移動
    }
}
