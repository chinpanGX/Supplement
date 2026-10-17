using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Cysharp.Threading.Tasks;
using SupplementSubscribeStrategy = Supplement.Core.AsyncSubscribeStrategy;
using ZeroSubscribeStrategy = ZeroMessenger.AsyncSubscribeStrategy;

namespace Supplement.Benchmarks
{
    // 非同期のハンドラーの完了のしかた。
    public enum HandlerCompletion
    {
        // その場で完了したタスクを返す(キャッシュの更新などの軽い処理)。
        Synchronous,

        // 完了していないタスクを返し、発行の後で完了させる(演出やロードを待つ処理)。
        Asynchronous,
    }

    // 完了していないUniTaskを返し、後から手で完了させる。使い回せるので、計測のたびに確保しない。
    // 続きは完了させたスレッドでその場で動く。
    public sealed class UniTaskSignal : IUniTaskSource
    {
        private UniTaskCompletionSourceCore<AsyncUnit> core;
        private bool waiting;

        public UniTask Wait()
        {
            core.Reset();
            waiting = true;
            return new UniTask(this, core.Version);
        }

        // Waitが呼ばれていない(Dropで捨てられたなど)ときは何もしない。
        public void Complete()
        {
            if (!waiting) return;
            waiting = false;
            core.TrySetResult(AsyncUnit.Default);
        }

        public void GetResult(short token) => core.GetResult(token);

        public UniTaskStatus GetStatus(short token) => core.GetStatus(token);

        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) =>
            core.OnCompleted(continuation, state, token);
    }

    // UniTaskSignalのValueTask版。ZeroMessengerのハンドラーに使う。
    public sealed class ValueTaskSignal : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<bool> core;
        private bool waiting;

        public ValueTask Wait()
        {
            core.Reset();
            waiting = true;
            return new ValueTask(this, core.Version);
        }

        // Waitが呼ばれていない(Dropで捨てられたなど)ときは何もしない。
        public void Complete()
        {
            if (!waiting) return;
            waiting = false;
            core.SetResult(true);
        }

        public void GetResult(short token) => core.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token,
            ValueTaskSourceOnCompletedFlags flags) => core.OnCompleted(continuation, state, token, flags);
    }

    // SupplementのストラテジーをZeroMessengerの同じ名前のものに置き換える。
    public static class Strategies
    {
        public static ZeroSubscribeStrategy ToZero(SupplementSubscribeStrategy strategy) => strategy switch
        {
            SupplementSubscribeStrategy.Sequential => ZeroSubscribeStrategy.Sequential,
            SupplementSubscribeStrategy.Parallel => ZeroSubscribeStrategy.Parallel,
            SupplementSubscribeStrategy.Switch => ZeroSubscribeStrategy.Switch,
            SupplementSubscribeStrategy.Drop => ZeroSubscribeStrategy.Drop,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
        };
    }
}
