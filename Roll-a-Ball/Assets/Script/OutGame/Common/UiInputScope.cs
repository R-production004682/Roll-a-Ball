using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 表示中の UI の入力範囲を登録し、最前面の選択・キャンセル・Player 遮断を共有する
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiInputScope : MonoBehaviour
    {
        [SerializeField] private Selectable firstSelected;
        [SerializeField] private UnityEvent onCancel = new UnityEvent();

        private static readonly List<UiInputScope> scopes = new List<UiInputScope>();
        private static readonly HashSet<object> inputLocks = new HashSet<object>();
        private static int cancelFrame = -1;
        private CanvasGroup canvasGroup;
        private GameObject lastSelection;

        public static bool HasOpenUi => scopes.Count > 0;
        public static bool IsBlocked => inputLocks.Count > 0 || FadeTransition.IsTransitioning;
        public static bool BlocksPlayer => HasOpenUi || IsBlocked || cancelFrame == Time.frameCount;
        public bool CanReceiveInput => isActiveAndEnabled && !IsBlocked &&
            scopes.Count > 0 && scopes[scopes.Count - 1] == this;
        public UnityEvent CancelRequested => onCancel;

        /// <summary>
        /// プレイ開始時に入力範囲と処理中ロックを破棄し、前回の状態を持ち越さない
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            scopes.Clear();
            inputLocks.Clear();
            cancelFrame = -1;
        }

        /// <summary>
        /// 非同期処理中の全 UI と Player 入力を止め、Dispose で解除するハンドルを返す
        /// </summary>
        public static IDisposable BlockInput()
        {
            var inputLock = new InputLock();
            inputLocks.Add(inputLock);
            RefreshScopes();
            return inputLock;
        }

        /// <summary>
        /// 表示された入力範囲を最前面へ登録し、直前のフォーカスを保持する
        /// </summary>
        private void OnEnable()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.ignoreParentGroups = true;
            if (scopes.Count > 0)
            {
                scopes[scopes.Count - 1].RememberSelection();
            }

            scopes.Add(this);
            RefreshScopes();
        }

        /// <summary>
        /// 閉じた入力範囲を解除し、残った最前面の UI とフォーカスを復元する
        /// </summary>
        private void OnDisable()
        {
            RememberSelection();
            scopes.Remove(this);
            RefreshScopes();
            if (!HasOpenUi && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// EventSystem より先に遷移中の遮断とフォーカスを更新し、最前面だけで Cancel を処理する
        /// </summary>
        private void Update()
        {
            ApplyInputState();
            if (!CanReceiveInput)
            {
                return;
            }

            RestoreSelection();
            var module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
            if (Input.GetKeyDown(KeyCode.Escape) ||
                (module != null && module.cancel != null && module.cancel.action.WasPressedThisFrame()))
            {
                RequestCancel();
            }
        }

        /// <summary>
        /// 最前面の Cancel を一フレーム一回通知し、背面や遷移中の要求は無視する
        /// </summary>
        public void RequestCancel()
        {
            if (!CanReceiveInput || cancelFrame == Time.frameCount)
            {
                return;
            }

            cancelFrame = Time.frameCount;
            onCancel.Invoke();
        }

        /// <summary>
        /// 登録中の範囲の入力可否とカーソルを更新し、最前面の選択を復元する
        /// </summary>
        private static void RefreshScopes()
        {
            foreach (var scope in scopes)
            {
                scope.ApplyInputState();
            }

            if (scopes.Count > 0)
            {
                scopes[scopes.Count - 1].RestoreSelection();
            }

            OutGameStateController.RefreshCursor();
        }

        /// <summary>
        /// 最前面だけクリックとナビゲーションを許可する
        /// </summary>
        private void ApplyInputState()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = CanReceiveInput;
            canvasGroup.blocksRaycasts = CanReceiveInput;
        }

        /// <summary>
        /// この範囲に属する現在の選択を保存し、範囲外の選択は保存しない
        /// </summary>
        private void RememberSelection()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.GetComponentInParent<UiInputScope>() == this)
            {
                lastSelection = selected;
            }

        }

        /// <summary>
        /// 有効な保存済み選択または Inspector の初期選択へ戻し、参照不足では選択を解除する
        /// </summary>
        private void RestoreSelection()
        {
            if (!CanReceiveInput || EventSystem.current == null)
            {
                return;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            if (IsValidSelection(selected))
            {
                return;
            }

            var target = IsValidSelection(lastSelection) ? lastSelection :
                firstSelected != null && IsValidSelection(firstSelected.gameObject) ? firstSelected.gameObject : null;
            if (selected != target)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

        /// <summary>
        /// 対象がこの範囲の有効で操作可能な Selectable なら true を返す
        /// </summary>
        private bool IsValidSelection(GameObject target)
        {
            if (target == null || !target.activeInHierarchy || target.GetComponentInParent<UiInputScope>() != this)
            {
                return false;
            }

            var selectable = target.GetComponent<Selectable>();
            return selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable();
        }

        /// <summary>
        /// 非同期処理の入力ロックを一度だけ解除するハンドル
        /// </summary>
        private sealed class InputLock : IDisposable
        {
            /// <summary>
            /// 自身のロックを解除し、重複 Dispose や前回プレイのハンドルは無視する
            /// </summary>
            public void Dispose()
            {
                if (inputLocks.Remove(this))
                {
                    RefreshScopes();
                }
            }
        }
    }
}
