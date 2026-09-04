using System.Collections.Generic;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// SceneRouter.SceneType からシーン名を取得するためのmapper
    /// </summary>
    internal static class SceneNameMap
    {
        private static readonly IReadOnlyDictionary<SceneType, string> SceneNames =
            new Dictionary<SceneType, string>
            {
                [SceneType.Title] = "TitleScene",
                [SceneType.OutGameTest] = "OutGameTestScene",
                [SceneType.Main] = "MainScene"
            };

        public static bool TryGet(SceneType sceneType, out string sceneName)
        {
            return SceneNames.TryGetValue(sceneType, out sceneName);
        }
    }
}
