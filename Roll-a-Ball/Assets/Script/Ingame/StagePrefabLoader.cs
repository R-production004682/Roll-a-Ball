using Roll_a_Ball.OutGame;
using UnityEngine;

/// <summary>
/// 選択したステージ Prefab を読み込み、一件もない場合は MainScene の配置を使う
/// </summary>
[DefaultExecutionOrder(-2000)]
public sealed class StagePrefabLoader : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab がある場合に非表示にする配置済みのステージとゴールです。Player、カメラ、UI は含めません。")]
    private GameObject[] sceneStageObjects;
    [SerializeField, Tooltip("読み込んだステージ Prefab を配置する場所です。未設定ならこのオブジェクトの子に配置します。")]
    private Transform stageRoot;

    /// <summary>
    /// Prefab が存在する場合だけ配置済みステージを置き換える
    /// </summary>
    private void Awake()
    {
        if (!StagePrefabCatalog.HasStagePrefabs)
        {
            return;
        }

        var stagePrefab = StagePrefabCatalog.GetStagePrefab(StageSelectionContext.SelectedStageId);
        if (stagePrefab == null)
        {
            Debug.LogError($"選択されたステージ Prefab が存在しません: {StageSelectionContext.SelectedStageId}", this);
            return;
        }

        if (sceneStageObjects == null || sceneStageObjects.Length == 0)
        {
            Debug.LogError("StagePrefabLoader の Scene Stage Objects が設定されていません。", this);
            return;
        }

        foreach (var stageObject in sceneStageObjects)
        {
            if (stageObject != null)
            {
                stageObject.SetActive(false);
            }
        }

        Instantiate(stagePrefab, stageRoot != null ? stageRoot : transform, true);
    }
}
