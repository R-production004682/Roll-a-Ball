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

        public string Path
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(sceneGuid))
                    return UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
#endif
                return scenePath;
            }
        }
        public bool IsAssigned => !string.IsNullOrWhiteSpace(Path);

#if UNITY_EDITOR
        public void SynchronizePath() => scenePath = Path;
#endif
    }
}
