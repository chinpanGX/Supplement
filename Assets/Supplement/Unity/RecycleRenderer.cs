using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        where TDto : class
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
            previousDtos = dtos;

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
        }

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

        private static void Release(TRenderable renderable)
        {
            renderable.gameObject.SetActive(false);
        }

        private static void Destroy(TRenderable renderable)
        {
            UnityEngine.Object.Destroy(renderable.gameObject);
        }
    }
}
