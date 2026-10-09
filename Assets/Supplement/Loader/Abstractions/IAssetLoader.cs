using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Supplement.Loader.Abstractions
{
    /// <summary>
    /// アセットを非同期で読み込む。
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// <paramref name="address"/>のアセットを読み込む。
        /// </summary>
        /// <param name="address">アセットのアドレス。</param>
        /// <param name="token">読み込みを取り消すトークン。取り消すと読み込み途中のアセットは解放する。</param>
        /// <typeparam name="T">アセットの型。</typeparam>
        /// <returns>読み込んだアセットのハンドル。使い終わったら破棄する。</returns>
        /// <exception cref="AssetLoadFailedException">読み込みに失敗した。</exception>
        UniTask<IAssetHandle<T>> LoadAssetAsync<T>(string address, CancellationToken token) where T : Object;

        /// <summary>
        /// <paramref name="label"/>の付いた<typeparamref name="T"/>のアセットをすべて読み込む。
        /// 1つでも失敗・取り消しされると、読み込み済みのものも含めてすべて解放して例外を投げる。
        /// </summary>
        /// <param name="label">アセットに付けたラベル。</param>
        /// <param name="token">読み込みを取り消すトークン。</param>
        /// <typeparam name="T">アセットの型。</typeparam>
        /// <returns>
        /// 読み込んだアセットのハンドル。該当するアセットが無ければ空。使い終わったら
        /// <see cref="AssetHandleExtensions.DisposeAll{T}"/>などですべて破棄する。
        /// </returns>
        UniTask<IReadOnlyList<IAssetHandle<T>>> LoadAssetsByLabelAsync<T>(string label, CancellationToken token)
            where T : Object;
    }
}