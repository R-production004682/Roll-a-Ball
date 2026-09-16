namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 現在のシーンに属する OutGame サービスへの窓口
    /// </summary>
    public static class GameServices
    {
        private static ScreenManager screens;
        private static SettingsDialogHost settings;

        /// <summary>
        /// 現在のシーンで登録されている ScreenManager を取得する
        /// </summary>
        public static ScreenManager Screens
        {
            get
            {
                if (screens == null)
                {
                    screens = UnityEngine.Object.FindFirstObjectByType<ScreenManager>(
                        UnityEngine.FindObjectsInactive.Exclude);
                }

                return screens;
            }
            private set => screens = value;
        }

        /// <summary>
        /// 現在のシーンで設定ダイアログを表示する Host を取得する
        /// </summary>
        public static SettingsDialogHost Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = UnityEngine.Object.FindFirstObjectByType<SettingsDialogHost>(
                        UnityEngine.FindObjectsInactive.Exclude);
                }

                return settings;
            }
            private set => settings = value;
        }

        /// <summary>
        /// プレイ開始時にシーン依存の static 参照を初期化する
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Screens = null;
            Settings = null;
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

        /// <summary>
        /// 現在のシーンの設定ダイアログ Host をサービス窓口へ登録する
        /// </summary>
        /// <param name="settingsDialogHost">登録する設定ダイアログ Host</param>
        internal static void RegisterSettings(SettingsDialogHost settingsDialogHost)
        {
            if (settingsDialogHost == null)
            {
                UnityEngine.Debug.LogError("null の SettingsDialogHost は登録できません。");
                return;
            }

            if (Settings != null && Settings != settingsDialogHost)
            {
                UnityEngine.Debug.LogWarning(
                    $"SettingsDialogHost が重複登録されました。後から登録した Host を使用します: {settingsDialogHost.name}",
                    settingsDialogHost);
            }

            Settings = settingsDialogHost;
        }

        /// <summary>
        /// 破棄される設定ダイアログ Host が登録中の場合だけサービス窓口から解除する
        /// </summary>
        /// <param name="settingsDialogHost">解除する設定ダイアログ Host</param>
        internal static void UnregisterSettings(SettingsDialogHost settingsDialogHost)
        {
            if (Settings == settingsDialogHost)
            {
                Settings = null;
            }
        }
    }
}
