namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// SceneRouter.SceneType からシーン名を取得するためのmapper
    /// </summary>
    internal static class SceneNameMap
    {
        /// <summary>
        /// シーン種別に対応するシーン名を取得する
        /// </summary>
        /// <param name="sceneType">取得対象のシーン種別</param>
        /// <param name="sceneName">取得したシーン名</param>
        /// <returns>対応するシーン名が定義されている場合は true</returns>
        public static bool TryGet(SceneType sceneType, out string sceneName)
        {
            sceneName = sceneType switch
            {
                SceneType.Title => "TitleScene",
                SceneType.OutGameTest => "OutGameTestScene",
                SceneType.Main => "MainScene",
                _ => null
            };

            return sceneName != null;
        }
    }
}
