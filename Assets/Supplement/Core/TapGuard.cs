using System;

namespace Supplement.Core
{
    /// <summary>
    /// 非同期処理などの多重実行を防ぐための再入ガード。
    /// </summary>
    /// <remarks>
    /// UI(Buttonなど)には依存しない。多重タップ防止にButtonの<c>interactable</c>も連動させたい場合は、
    /// 呼び出し側が<see cref="IsGuarding"/>を見て自分で制御すること。
    /// </remarks>
    public sealed class TapGuard
    {
        private int guardCount;

        public bool IsGuarding => guardCount > 0;

        /// <summary>
        /// ガードを開始する。返却された<see cref="IDisposable"/>を破棄するとガードを終了する。
        /// ネストして呼び出した場合は、すべての<see cref="IDisposable"/>が破棄されるまでガードが継続する。
        /// </summary>
        public IDisposable BeginGuard()
        {
            guardCount++;
            return new GuardHandle(this);
        }

        private void EndGuard()
        {
            guardCount = Math.Max(0, guardCount - 1);
        }

        private sealed class GuardHandle : IDisposable
        {
            private readonly TapGuard owner;
            private bool disposed;

            public GuardHandle(TapGuard owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                owner.EndGuard();
            }
        }
    }
}
