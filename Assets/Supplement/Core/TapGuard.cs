using System;
using System.Collections.Generic;

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
        // 解除されていないハンドルのID。ハンドルは構造体でコピーされうるため、同じハンドル(とそのコピー)を
        // 何度Disposeしても解除が1回だけになるよう、IDで管理する。
        private readonly HashSet<int> activeHandleIds = new();
        private int lastHandleId;

        /// <summary>
        /// 破棄されていないハンドルが1つでもあるかどうか。
        /// </summary>
        public bool IsGuarding => activeHandleIds.Count > 0;

        /// <summary>
        /// ガードを開始する。返却されたハンドルを破棄するとガードを終了する。
        /// ネストして呼び出した場合は、すべてのハンドルが破棄されるまでガードが継続する。
        /// </summary>
        /// <remarks>
        /// ハンドルは構造体のため、<c>using</c>や<see cref="GuardHandle"/>型の変数で受ければGCアロケーションは起きない。
        /// <see cref="IDisposable"/>型の変数で受けるとボックス化される。
        /// </remarks>
        public GuardHandle BeginGuard()
        {
            var id = unchecked(++lastHandleId);
            activeHandleIds.Add(id);
            return new GuardHandle(this, id);
        }

        private void EndGuard(int id)
        {
            activeHandleIds.Remove(id);
        }

        /// <summary>
        /// <see cref="BeginGuard"/>が返すハンドル。破棄するとガードを1つ終える。
        /// </summary>
        public readonly struct GuardHandle : IDisposable
        {
            private readonly TapGuard owner;
            private readonly int id;

            internal GuardHandle(TapGuard owner, int id)
            {
                this.owner = owner;
                this.id = id;
            }

            /// <summary>
            /// ガードを終える。同じハンドル(とそのコピー)を何度破棄しても、終えるのは1回だけ。
            /// </summary>
            public void Dispose()
            {
                owner?.EndGuard(id);
            }
        }
    }
}