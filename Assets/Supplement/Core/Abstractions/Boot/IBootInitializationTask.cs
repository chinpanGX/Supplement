using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    /// <summary>
    /// 起動時に実行する初期化タスク。具体的な初期化内容はアプリ側で実装する。
    /// </summary>
    /// <remarks>
    /// DIコンテナに<c>As&lt;IBootInitializationTask&gt;()</c>で複数登録すると、<see cref="BootInitializer"/>が<see cref="Priority"/>の
    /// 小さい順に実行する。同じ<see cref="Priority"/>のタスクは並列に実行される。
    /// </remarks>
    public interface IBootInitializationTask
    {
        InitializationPriority Priority { get; }
        UniTask InitializeAsync(CancellationToken ct);
    }
}
