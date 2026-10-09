using System;
using System.Collections.Generic;
using UnityEngine;
#if SUPPLEMENT_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Supplement.Unity
{
    /// <summary>
    /// 有効な<see cref="BackKeyReceiver"/>をまとめて扱う。戻るキーの入力は1フレームに1回だけ読み、
    /// 押されたフレームだけ各Receiverの最前面判定をする。
    /// </summary>
    internal sealed class BackKeyDispatcher : IUpdatable
    {
        private static readonly BackKeyDispatcher Instance = new();
        private static readonly List<BackKeyReceiver> Receivers = new();
        // 押されたフレームに発火させるReceiverを集める作業用のリスト。毎回作らないよう使い回す。
        private static readonly List<BackKeyReceiver> ReceiversToInvoke = new();

        public static void Add(BackKeyReceiver receiver)
        {
            if (Receivers.Contains(receiver))
            {
                return;
            }

            Receivers.Add(receiver);
            if (Receivers.Count == 1)
            {
                UpdateDispatcher.Register(Instance);
            }
        }

        public static void Remove(BackKeyReceiver receiver)
        {
            if (!Receivers.Remove(receiver))
            {
                return;
            }

            if (Receivers.Count == 0)
            {
                UpdateDispatcher.Unregister(Instance);
            }
        }

        public void OnUpdate()
        {
            if (!WasBackKeyPressedThisFrame())
            {
                return;
            }

            // 全員の判定を済ませてから発火させる。発火で画面が閉じると、その下のReceiverが同じフレームのうちに
            // 最前面になって続けて発火してしまうため。
            foreach (var receiver in Receivers)
            {
                // 1つのReceiverの判定の失敗(Canvasの外に置かれている等)で、ほかのReceiverまで止めない。
                try
                {
                    if (receiver.CanInvoke())
                    {
                        ReceiversToInvoke.Add(receiver);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e, receiver);
                }
            }

            try
            {
                foreach (var receiver in ReceiversToInvoke)
                {
                    // 先に発火したReceiverの処理で、無効化・破棄されていることがある。
                    if (receiver != null && receiver.isActiveAndEnabled)
                    {
                        receiver.Invoke();
                    }
                }
            }
            finally
            {
                ReceiversToInvoke.Clear();
            }
        }

        /// <summary>
        /// Active Input HandlingがInput Systemだけのときに旧Input Managerの<see cref="Input"/>を読むと例外になるため、
        /// 有効な方の入力から読む。両方有効ならInput Systemを優先する。
        /// </summary>
        private static bool WasBackKeyPressedThisFrame()
        {
#if SUPPLEMENT_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            // Input SystemではAndroidの戻るボタンもKeyboardのescapeKeyとして届く。
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // ドメインリロードを無効にしていると、前回のプレイのReceiverが残っていることがあるため消す。
            Receivers.Clear();
            ReceiversToInvoke.Clear();
        }
    }
}