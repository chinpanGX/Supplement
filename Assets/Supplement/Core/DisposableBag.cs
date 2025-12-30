#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Supplement.Core
{
    public sealed class DisposableBag : IDisposable
    {
        private List<IDisposable?> list;

        public DisposableBag()
        {
            list = new List<IDisposable?>();
        }

        public DisposableBag(int capacity)
        {
            if (capacity < 0) throw new ArgumentException(nameof(capacity));

            list = new List<IDisposable?>(capacity);
        }

        public DisposableBag(IEnumerable<IDisposable> disposables, int capacity = 0)
        {
            if (disposables == null) throw new ArgumentNullException(nameof(disposables));
            if (capacity < 0) throw new ArgumentException(nameof(capacity));

            list = capacity > 0 ? new List<IDisposable?>(capacity) : new List<IDisposable?>();
            foreach (var item in disposables)
            {
                list.Add(item);
            }
            Count = list.Count;
        }
        public int Count { get; private set; }
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

            foreach (var item in disposables)
            {
                item?.Dispose();
            }
            disposables.Clear();
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

            try
            {
                foreach (var item in targetDisposables.AsSpan(0, clearCount))
                {
                    item?.Dispose();
                }
            }
            finally
            {
                ArrayPool<IDisposable?>.Shared.Return(targetDisposables, true);
            }
        }

        public bool Contains(IDisposable item)
        {
            if (IsDisposed) return false;

            return list.Contains(item);
        }
    }

    public static class DisposableBagExtensions
    {
        public static DisposableBag Create(IDisposable[] disposables)
        {
            if (disposables == null) throw new ArgumentNullException(nameof(disposables));

            return new DisposableBag(disposables, disposables.Length);
        }

        public static DisposableBag AddTo(this IDisposable disposable, DisposableBag bag)
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));

            bag.Add(disposable);
            return bag;
        }
    }
}