using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryButton : MonoBehaviour
{

    public void Retry()//リトライする
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);//現在のシーンを読み込む
    }

}
