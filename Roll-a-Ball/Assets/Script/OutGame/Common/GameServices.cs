namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 現在のシーンに属する OutGame サービスへの窓口
    /// </summary>
    public static class GameServices
    {
        /// <summary>
        /// 現在のシーンで登録されている ScreenManager を取得する
        /// </summary>
        public static ScreenManager Screens { get; private set; }

        /// <summary>
        /// プレイ開始時にシーン依存の static 参照を初期化する
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Screens = null;
        }

        /// <summary>
        /// 現在のシーンの ScreenManager をサービス窓口へ登録する
        /// </summary>
        /// <param name="screenManager">登録する ScreenManager</param>
        internal static void RegisterScreens(ScreenManager screenManager)
        {
            if (screenManager == null)
            {
                UnityEngine.Debug.LogError("null の ScreenManager は登録できません。");
                return;
            }

            if (Screens != null && Screens != screenManager)
            {
                UnityEngine.Debug.LogWarning($"ScreenManager が二重登録されました。新しい Manager を使用します: {screenManager.name}", screenManager);
            }

            Screens = screenManager;
        }

        /// <summary>
        /// 指定された ScreenManager が登録中なら窓口から解除する
        /// </summary>
        /// <param name="screenManager">解除対象の ScreenManager</param>
        internal static void UnregisterScreens(ScreenManager screenManager)
        {
            if (Screens == screenManager)
            {
                Screens = null;
            }
        }
    }
}
