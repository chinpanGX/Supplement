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
        /// <summary>
        /// 実行する順番。小さいものから順に実行する。
        /// </summary>
        InitializationPriority Priority { get; }

        /// <summary>
        /// 初期化を行う。
        /// </summary>
        /// <param name="ct">初期化を取り消すトークン。<see cref="BootInitializer.RunAsync"/>に渡したもの。</param>
        UniTask InitializeAsync(CancellationToken ct);
    }
}
