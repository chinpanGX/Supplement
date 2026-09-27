using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// 登録された<see cref="IBootInitializationTask"/>を、<see cref="InitializationPriority"/>の小さい順に実行する。
    /// 同じPriorityのタスクは並列に実行する。
    /// </summary>
    /// <remarks>
    /// エントリーポイントにはしない。アプリ側のエントリーポイント(VContainerの<c>IAsyncStartable</c>等)から
    /// <see cref="RunAsync"/>を呼び、初期化後の処理はその後に続けて書く。
    /// </remarks>
    public sealed class BootInitializer
    {
        private readonly IReadOnlyList<IBootInitializationTask> tasks;
        private bool started;

        public BootInitializer(IEnumerable<IBootInitializationTask> tasks)
        {
            this.tasks = tasks.ToArray();
        }

        /// <summary>
        /// 初期化タスクを実行する。いずれかのタスクが失敗した場合は、以降のPriorityのタスクを実行せずにその例外を投げる。
        /// 一度しか実行できない。
        /// </summary>
        public async UniTask RunAsync(CancellationToken ct = default)
        {
            if (started)
            {
                throw new InvalidOperationException($"{nameof(BootInitializer)} has already been run.");
            }
            started = true;

            foreach (var group in tasks.GroupBy(x => x.Priority).OrderBy(x => x.Key))
            {
                await UniTask.WhenAll(group.Select(x => x.InitializeAsync(ct)));
            }
        }
    }
}
