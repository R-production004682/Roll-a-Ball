#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Roll_a_Ball.DebugTools
{
    /// <summary>
    /// 開発用デバッグツールを全Sceneで共有するルートを生成する
    /// </summary>
    internal static class DebugToolBootstrap
    {
        private const string RootObjectName = "RollABall.DebugTools";

        /// <summary>
        /// Scene 読み込み前にデバッグツールの永続オブジェクトを生成する
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindFirstObjectByType<DebugToolController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var rootObject = new GameObject(RootObjectName);
            Object.DontDestroyOnLoad(rootObject);
            rootObject.AddComponent<DebugToolController>();
        }
    }
}
#endif
