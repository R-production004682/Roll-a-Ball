using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;//ゲームマネージャー

    [SerializeField]
    public bool isClear = false;//クリアしているか

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Clear()//クリア処理
    {

        if (isClear==true )
            return;//クリア済みの場合以降を処理しない

        isClear = true;//クリア判定

       

    }



    /*
    public void deductHavetCoins()//コイン使用処理
    { if(DataHolder.instance.hasCoin<useCoin)//所持コインより使用コインが多い場合
    　　　{return　false;}//失敗
      if(DataHolder.instance.hasCoin=>useCoin)
     { DataHolder.instance.hasCoin-=useCoin;//使用コインを所持コインから減算する
    　   return true;}//成功

            }
    */

}
