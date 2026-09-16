namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// StageSelectScreen の入力、演出、ランキング表示で共有する定数を定義する
    /// </summary>
    internal static class StageSelectScreenConstants
    {
        public const int FirstRank = 1;
        public const float UnrecordedClearTimeSeconds = -1f;
        public const float CentisecondsPerSecond = 100f;
        public const int CentisecondsPerMinute = 6000;
        public const string UnrecordedClearTimeText = "--:--:--";
        public const float MinimumSelectionPulseDuration = 0.05f;
        public const float DefaultSelectionPulseDuration = 0.18f;
        public const float ScrollInputThreshold = 0.01f;
        public const float SelectionPulseScaleAmplitude = 0.035f;
        public const float SelectionPulseStartElapsed = 0f;
        public const float InactiveSelectionPulseElapsed = -1f;
    }
}
