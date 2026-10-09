using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Supplement.Unity
{
    /// <summary>
    /// 登録された<see cref="IUpdatable"/>を、PlayerLoopのUpdateに差し込んだ1か所からまとめて呼ぶ。
    /// </summary>
    /// <remarks>
    /// MonoBehaviourのUpdateはインスタンスごとにネイティブからマネージドへの呼び出しが起きるため、数が多いとその分重くなる。
    /// MonoBehaviourのOnEnableで<see cref="Register"/>、OnDisableで<see cref="Unregister"/>する使い方を想定している。
    /// 呼び出し順は登録順とは限らない。純粋なC#のクラスはVContainerのITickableを使えばよい。
    /// </remarks>
    public static class UpdateDispatcher
    {
        private static readonly List<IUpdatable> Updatables = new();
        private static readonly Dictionary<IUpdatable, int> IndexByUpdatable = new();
        private static bool isUpdating;
        private static bool hasRemovedWhileUpdating;

        /// <summary>
        /// <paramref name="updatable"/>を毎フレーム呼ぶよう登録する。登録済みなら何もしない。
        /// 呼び出し中に登録したものは次のフレームから呼ぶ。
        /// </summary>
        /// <param name="updatable">毎フレーム呼ぶもの。</param>
        /// <exception cref="ArgumentNullException"><paramref name="updatable"/>がnull。</exception>
        public static void Register(IUpdatable updatable)
        {
            if (updatable == null) throw new ArgumentNullException(nameof(updatable));
            if (IndexByUpdatable.ContainsKey(updatable)) return;

            IndexByUpdatable.Add(updatable, Updatables.Count);
            Updatables.Add(updatable);
        }

        /// <summary>
        /// <paramref name="updatable"/>の登録を外す。登録されていないかnullなら何もしない。
        /// 呼び出し中に外しても、ほかの登録が飛ばされることはない。
        /// </summary>
        /// <param name="updatable">外すもの。</param>
        public static void Unregister(IUpdatable updatable)
        {
            if (updatable == null) return;
            if (!IndexByUpdatable.Remove(updatable, out var index)) return;

            if (isUpdating)
            {
                // 回している最中に詰めると、まだ呼んでいない要素が前に移って飛ばされるため、空けておいて回し終えてから詰める。
                Updatables[index] = null;
                hasRemovedWhileUpdating = true;
                return;
            }

            RemoveAtBySwap(index);
        }

        private static void Tick()
        {
            isUpdating = true;
            // 途中で登録されたものは次のフレームから呼ぶ。
            var count = Updatables.Count;
            for (var i = 0; i < count; i++)
            {
                var updatable = Updatables[i];
                if (updatable == null)
                {
                    continue;
                }

                // Unregisterせずに破棄されたMonoBehaviourを呼び続けると毎フレーム例外になるため、外す。
                if (updatable is UnityEngine.Object unityObject && unityObject == null)
                {
                    Unregister(updatable);
                    continue;
                }

                try
                {
                    updatable.OnUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            isUpdating = false;

            if (hasRemovedWhileUpdating)
            {
                hasRemovedWhileUpdating = false;
                for (var i = Updatables.Count - 1; i >= 0; i--)
                {
                    if (Updatables[i] == null)
                    {
                        RemoveAtBySwap(i);
                    }
                }
            }
        }

        // 末尾の要素を空いた位置に移して詰める。順序は変わるが、Removeのたびに後ろをずらさずに済む。
        private static void RemoveAtBySwap(int index)
        {
            var lastIndex = Updatables.Count - 1;
            if (index != lastIndex)
            {
                var last = Updatables[lastIndex];
                Updatables[index] = last;
                if (last != null)
                {
                    IndexByUpdatable[last] = index;
                }
            }
            Updatables.RemoveAt(lastIndex);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            // ドメインリロードを無効にしていると、前回のプレイの登録とPlayerLoopへの差し込みが残っているため、作り直す。
            Updatables.Clear();
            IndexByUpdatable.Clear();
            isUpdating = false;
            hasRemovedWhileUpdating = false;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var subSystems = playerLoop.subSystemList;
            for (var i = 0; i < subSystems.Length; i++)
            {
                if (subSystems[i].type != typeof(UnityEngine.PlayerLoop.Update))
                {
                    continue;
                }

                var updateSystems = new List<PlayerLoopSystem>(subSystems[i].subSystemList);
                updateSystems.RemoveAll(x => x.type == typeof(UpdateDispatcher));
                updateSystems.Add(new PlayerLoopSystem { type = typeof(UpdateDispatcher), updateDelegate = Tick });
                subSystems[i].subSystemList = updateSystems.ToArray();
                PlayerLoop.SetPlayerLoop(playerLoop);
                return;
            }
        }
    }
}