using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Supplement.Loader.Abstractions
{
    /// <summary>
    /// 読み込んだシーンへのハンドル。破棄するとシーンをアンロードする。
    /// </summary>
    public interface ISceneHandle : IDisposable
    {
        /// <summary>
        /// 読み込みが終わったかどうか。
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// 読み込みに成功したかどうか。
        /// </summary>
        bool Succeeded { get; }

        /// <summary>
        /// 読み込んだシーン。破棄した後に取得すると<see cref="ObjectDisposedException"/>を投げる。
        /// </summary>
        Scene Result { get; }

        /// <summary>
        /// 読み込み時にアクティブ化しなかったシーンをアクティブ化する。
        /// </summary>
        /// <param name="token">待つのを取り消すトークン。</param>
        /// <exception cref="InvalidOperationException">読み込み時にアクティブ化するよう指定していた、またはすでにアクティブ化した。</exception>
        /// <exception cref="ObjectDisposedException">ハンドルを破棄した後に呼んだ。</exception>
        ValueTask ActivateAsync(CancellationToken token);
    }
}