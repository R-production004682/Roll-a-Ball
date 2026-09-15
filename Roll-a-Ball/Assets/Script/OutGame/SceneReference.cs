using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// Inspector で Scene アセットを指定し、実行時にはそのパスを使用する参照
    /// </summary>
    [Serializable]
    public sealed class SceneReference
    {
        [SerializeField] private string scenePath;
        [SerializeField, HideInInspector] private string sceneGuid;

        /// <summary>
        /// 現在の Scene アセットパスを返す
        /// </summary>
        public string Path
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(sceneGuid))
                {
                    return UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
                }
#endif
                return scenePath;
            }
        }
        /// <summary>
        /// 有効な Scene パスが設定されているか
        /// </summary>
        public bool IsAssigned => !string.IsNullOrWhiteSpace(Path);

#if UNITY_EDITOR
        /// <summary>
        /// GUID から解決した Scene パスをシリアライズ値へ同期する
        /// </summary>
        public void SynchronizePath()
        {
            scenePath = Path;
        }
#endif
    }
}
