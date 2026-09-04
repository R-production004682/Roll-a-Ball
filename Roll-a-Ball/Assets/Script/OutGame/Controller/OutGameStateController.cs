using System;
using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// OutGame で共有するゲーム進行状態と、それに付随する時間・カーソル状態を管理する
    /// </summary>
    internal static class OutGameStateController
    {
        /// <summary>
        /// 現在のゲーム進行状態
        /// </summary>
        public static GameFlowState Current { get; private set; } = GameFlowState.Menu;

        /// <summary>
        /// 現在プレイ中かどうか
        /// </summary>
        public static bool IsPlaying => Current == GameFlowState.Playing;

        /// <summary>
        /// ゲーム進行状態が変更されたときに通知するイベント
        /// </summary>
        public static event Action<GameFlowState> StateChanged;

        /// <summary>
        /// ゲーム進行状態を変更し、時間とカーソルの状態を反映する
        /// </summary>
        /// <param name="state">変更先のゲーム進行状態</param>
        public static void Enter(GameFlowState state)
        {
            Current = state;

            var isPlaying = state == GameFlowState.Playing;
            var isStopped = state == GameFlowState.Paused || state == GameFlowState.Cleared;

            Time.timeScale = isStopped ? 0f : 1f;
            Cursor.lockState = isPlaying ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isPlaying;

            StateChanged?.Invoke(state);
        }
    }
}
