#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Supplement.Core
{
    /// <summary>
    /// 複数の<see cref="IDisposable"/>をまとめて持ち、まとめて破棄する。
    /// </summary>
    /// <remarks>
    /// 破棄の途中で例外が出ても残りはすべて破棄し、例外は最後にまとめて投げる(1件ならそのまま、複数なら
    /// <see cref="AggregateException"/>)。
    /// </remarks>
    public sealed class DisposableBag : IDisposable
    {
        private List<IDisposable?> list;

        /// <summary>
        /// 空の<see cref="DisposableBag"/>を作る。
        /// </summary>
        public DisposableBag()
        {
            list = new List<IDisposable?>();
        }

        /// <summary>
        /// 最初に確保する容量を指定して、空の<see cref="DisposableBag"/>を作る。
        /// </summary>
        /// <param name="capacity">最初に確保する容量。</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/>が負。</exception>
        public DisposableBag(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            list = new List<IDisposable?>(capacity);
        }

        /// <summary>
        /// <paramref name="disposables"/>を持った<see cref="DisposableBag"/>を作る。
        /// </summary>
        /// <param name="disposables">最初に持たせる<see cref="IDisposable"/>。</param>
        /// <param name="capacity">最初に確保する容量。0なら既定の容量。</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposables"/>がnull。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/>が負。</exception>
        public DisposableBag(IEnumerable<IDisposable> disposables, int capacity = 0)
        {
            if (disposables == null) throw new ArgumentNullException(nameof(disposables));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            list = capacity > 0 ? new List<IDisposable?>(capacity) : new List<IDisposable?>();
            foreach (var item in disposables)
            {
                list.Add(item);
            }
            Count = list.Count;
        }

        /// <summary>
        /// 持っている<see cref="IDisposable"/>の数。
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// <see cref="Dispose"/>したかどうか。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// このDisposableBagオブジェクトによって保持されているすべてのリソースを解放します。
        /// 内部コレクション内のすべてのIDisposableオブジェクトが破棄され、
        /// Disposeメソッドが呼び出された後、以降の操作は無効になります。
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            Count = 0;
            IsDisposed = true;
            var disposables = list;
            list = null!;

            List<Exception>? exceptions = null;
            foreach (var item in disposables)
            {
                DisposeSafely(item, ref exceptions);
            }
            disposables.Clear();
            ThrowIfAny(exceptions);
        }

        /// <summary>
        /// DisposableBagにIDisposableオブジェクトを追加します。
        /// DisposableBagが既に破棄されている場合は、指定されたIDisposableオブジェクトも即座に破棄されます。
        /// </summary>
        /// <param name="disposable">追加するIDisposableオブジェクト。</param>
        public void Add(IDisposable disposable)
        {
            if (!IsDisposed)
            {
                Count += 1;
                list.Add(disposable);
                return;
            }

            disposable.Dispose();
        }

        /// <summary>
        /// DisposableBagに格納されているすべてのIDisposableオブジェクトを破棄し、
        /// コレクションを空にします。
        /// </summary>
        public void Clear()
        {
            if (IsDisposed) return;
            if (Count == 0) return;

            var targetDisposables = ArrayPool<IDisposable?>.Shared.Rent(list.Count);
            var clearCount = list.Count;

            list.CopyTo(targetDisposables);

            list.Clear();
            Count = 0;

            List<Exception>? exceptions = null;
            try
            {
                foreach (var item in targetDisposables.AsSpan(0, clearCount))
                {
                    DisposeSafely(item, ref exceptions);
                }
            }
            finally
            {
                ArrayPool<IDisposable?>.Shared.Return(targetDisposables, true);
            }
            ThrowIfAny(exceptions);
        }

        /// <summary>
        /// <paramref name="item"/>を持っているかどうかを返す。破棄した後は常にfalse。
        /// </summary>
        /// <param name="item">探す<see cref="IDisposable"/>。</param>
        public bool Contains(IDisposable item)
        {
            if (IsDisposed) return false;

            return list.Contains(item);
        }

        // 呼び出し時点でリストから外しているため、途中で例外が出ても残りを必ずDisposeしないと二度と解放できなくなる。
        // 例外は全件Disposeし終えてからまとめて投げる。
        private static void DisposeSafely(IDisposable? item, ref List<Exception>? exceptions)
        {
            try
            {
                item?.Dispose();
            }
            catch (Exception e)
            {
                (exceptions ??= new List<Exception>()).Add(e);
            }
        }

        private static void ThrowIfAny(List<Exception>? exceptions)
        {
            if (exceptions == null) return;

            if (exceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }
            throw new AggregateException(exceptions);
        }
    }

    /// <summary>
    /// <see cref="DisposableBag"/>を作る・足すための補助。
    /// </summary>
    public static class DisposableBagExtensions
    {
        /// <summary>
        /// <paramref name="disposables"/>を持った<see cref="DisposableBag"/>を作る。
        /// </summary>
        /// <param name="disposables">持たせる<see cref="IDisposable"/>。</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposables"/>がnull。</exception>
        public static DisposableBag Create(IDisposable[] disposables)
        {
            if (disposables == null) throw new ArgumentNullException(nameof(disposables));

            return new DisposableBag(disposables, disposables.Length);
        }

        /// <summary>
        /// <paramref name="disposable"/>を<paramref name="bag"/>に足す。<paramref name="bag"/>が破棄済みなら、すぐに破棄する。
        /// </summary>
        /// <param name="disposable">足す<see cref="IDisposable"/>。</param>
        /// <param name="bag">足す先。</param>
        /// <returns><paramref name="bag"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bag"/>がnull。</exception>
        public static DisposableBag AddTo(this IDisposable disposable, DisposableBag bag)
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));

            bag.Add(disposable);
            return bag;
        }
    }
}