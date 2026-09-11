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
        /// プレイ開始時に状態、時間、音声、カーソルを初期値へ戻す
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Current = GameFlowState.Menu;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StateChanged = null;
        }

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
            if (!Enum.IsDefined(typeof(GameFlowState), state))
            {
                Debug.LogError($"未定義の GameFlowState は設定できません: {state}");
                return;
            }

            if (Current == state)
            {
                ApplyRuntimeState(state);
                return;
            }

            var previousState = Current;
            Current = state;
            ApplyRuntimeState(state);
            NotifyStateChanged(state);
            Debug.Log($"OutGame 状態を変更しました: {previousState} -> {state}");
        }

        /// <summary>
        /// 指定された進行状態を Unity の時間、音声、カーソルへ反映する
        /// </summary>
        /// <param name="state">反映するゲーム進行状態</param>
        private static void ApplyRuntimeState(GameFlowState state)
        {
            var isPlaying = state == GameFlowState.Playing;
            var isStopped = state == GameFlowState.Paused || state == GameFlowState.Cleared;

            Time.timeScale = isStopped ? 0f : 1f;
            AudioListener.pause = isStopped;
            Cursor.lockState = isPlaying ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isPlaying;
        }

        /// <summary>
        /// 状態変更を購読者へ通知し、個別購読者の例外を記録して処理を継続する
        /// </summary>
        /// <param name="state">通知するゲーム進行状態</param>
        private static void NotifyStateChanged(GameFlowState state)
        {
            if (StateChanged == null)
            {
                return;
            }

            foreach (var callback in StateChanged.GetInvocationList())
            {
                try
                {
                    ((Action<GameFlowState>)callback)(state);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"GameFlowState の通知先で例外が発生しました。\n{exception}");
                }
            }
        }
    }
}
