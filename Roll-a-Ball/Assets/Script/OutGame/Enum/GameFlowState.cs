namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGame で扱うゲーム進行状態
    /// </summary>
    internal enum GameFlowState
    {
        /// <summary>
        /// メニューを表示している状態
        /// </summary>
        Menu,

        /// <summary>
        /// ゲームをプレイしている状態
        /// </summary>
        Playing,

        /// <summary>
        /// ゲームを一時停止している状態
        /// </summary>
        Paused,

        /// <summary>
        /// ゲームをクリアした状態
        /// </summary>
        Cleared
    }
}
