using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="ObjectPool{T}"/> を利用して、DTOの配列を <typeparamref name="TRenderable"/> のインスタンス列に
    /// 非同期描画するピュアC#クラス。
    /// </summary>
    /// <remarks>
    /// テンプレート/配置先は <see cref="RecycleRendererFactory"/> などInspectorで設定したものをコンストラクタで受け取る。
    /// 前回の呼び出しが完了する前に <see cref="RenderAsync"/> を再度呼び出さないこと。
    /// </remarks>
    public class RecycleRenderer<TRenderable, TDto> : IDisposable
        where TRenderable : Component, IAsyncRenderable<TDto>
        where TDto : class, IEquatable<TDto>
    {
        private readonly GameObject template;
        private readonly Transform parent;
        private readonly IObjectResolver objectResolver;

        private readonly List<TRenderable> activeRenderables = new();
        private readonly ObjectPool<TRenderable> pool;

        private EquatableReadOnlyList<TDto> previousDtos;
        private bool disposed;

        internal RecycleRenderer(IObjectResolver objectResolver, GameObject template, Transform parent)
        {
            this.objectResolver = objectResolver;
            this.template = template;
            this.parent = parent;

            pool = new ObjectPool<TRenderable>(Create, Get, Release, Destroy);
            template.SetActive(false);
        }

        /// <summary>
        /// <paramref name="dtos"/>の数だけインスタンスを並べ、それぞれに<see cref="IAsyncRenderable{T}.RenderAsync"/>で表示させる。
        /// 余ったインスタンスは非アクティブにしてプールに戻す。前回描画し終えたものと値が等しければ何もしない。
        /// </summary>
        /// <param name="dtos">表示する内容。並び順がヒエラルキー上の並び順になる。</param>
        /// <exception cref="ObjectDisposedException">破棄した後に呼んだ。</exception>
        public async UniTask RenderAsync(EquatableReadOnlyList<TDto> dtos)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RecycleRenderer<TRenderable, TDto>));
            }

            if (dtos == previousDtos)
            {
                return;
            }
            // 描画が途中で失敗したとき、同じDTOでの再試行を弾かないよう、完了するまで前回の値を持たない。
            previousDtos = null;

            while (activeRenderables.Count > dtos.Count)
            {
                var lastIndex = activeRenderables.Count - 1;
                pool.Release(activeRenderables[lastIndex]);
                activeRenderables.RemoveAt(lastIndex);
            }

            while (activeRenderables.Count < dtos.Count)
            {
                activeRenderables.Add(pool.Get());
            }

            using (ListPool<UniTask>.Get(out var tasks))
            {
                for (var i = 0; i < dtos.Count; i++)
                {
                    // ObjectPoolはStack実装のため、Get()で返るインスタンスは生成順と一致しない。
                    // ヒエラルキー上の並び順を論理インデックスに揃えるため、都度SetSiblingIndexする。
                    activeRenderables[i].transform.SetSiblingIndex(i);
                    tasks.Add(activeRenderables[i].RenderAsync(dtos[i]));
                }
                await UniTask.WhenAll(tasks);
            }
            previousDtos = dtos;
        }

        /// <summary>
        /// 生成したインスタンスをすべて破棄する。2回目以降は何もしない。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            foreach (var renderable in activeRenderables)
            {
                pool.Release(renderable);
            }
            activeRenderables.Clear();
            pool.Dispose();
        }

        private TRenderable Create()
        {
            var instance = objectResolver.Instantiate(template, parent);
            return instance.GetComponent<TRenderable>();
        }

        private static void Get(TRenderable renderable)
        {
            renderable.gameObject.SetActive(true);
        }

        // ファクトリのOnDestroyからDisposeされるときは、ヒエラルキーの破棄で子が先に破棄済みのことがある。
        private static void Release(TRenderable renderable)
        {
            if (renderable == null)
            {
                return;
            }
            renderable.gameObject.SetActive(false);
        }

        private static void Destroy(TRenderable renderable)
        {
            if (renderable == null)
            {
                return;
            }
            UnityEngine.Object.Destroy(renderable.gameObject);
        }
    }
}
