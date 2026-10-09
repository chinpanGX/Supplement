using System;
using Supplement.Core;
using UnityEngine;
using VContainer;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="RecycleRenderer{TRenderable,TDto}"/> が必要とするテンプレートと配置先のTransformを
    /// Inspectorから設定し、<see cref="RecycleRenderer{TRenderable,TDto}"/>を生成するコンポーネント。
    /// </summary>
    /// <remarks>
    /// <see cref="RecycleRenderer{TRenderable,TDto}"/> 自体はジェネリックのためAdd Componentからは配置できない。
    /// このコンポーネントを配置してInspectorで設定し、<see cref="Create{TRenderable,TDto}"/>で生成して使う。
    /// </remarks>
    public class RecycleRendererFactory : MonoBehaviour
    {
        [SerializeField] private GameObject template;
        [SerializeField] private Transform parent;
        
        private IObjectResolver objectResolver;
        private IDisposable disposable;

        /// <summary>
        /// VContainerから注入する。インスタンスの生成に使う。
        /// </summary>
        /// <param name="objectResolver">インスタンスを生成するリゾルバー。</param>
        [Inject]
        public void Construct(IObjectResolver objectResolver)
        {
            this.objectResolver = objectResolver;
        }

        /// <summary>
        /// Inspectorで設定したテンプレートと配置先を使う<see cref="RecycleRenderer{TRenderable,TDto}"/>を作る。
        /// 作ったものは、このコンポーネントが破棄されるときに一緒に破棄する。
        /// </summary>
        /// <typeparam name="TRenderable">テンプレートに付いている、表示を担うコンポーネントの型。</typeparam>
        /// <typeparam name="TDto">表示する内容を表すDTOの型。</typeparam>
        /// <exception cref="InvalidOperationException">
        /// すでに作っている(1つのファクトリで作れるのは1つだけ)、またはDIコンテナを通さずに生成されていて注入されていない。
        /// </exception>
        public RecycleRenderer<TRenderable, TDto> Create<TRenderable, TDto>()
            where TRenderable : Component, IAsyncRenderable<TDto>
            where TDto : class, IEquatable<TDto>
        {
            if (disposable != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(Create)} was already called on this {nameof(RecycleRendererFactory)}. Create only one RecycleRenderer per factory.");
            }

            if (objectResolver == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(objectResolver)} is not injected yet. {nameof(RecycleRendererFactory)} must be instantiated through the DI container before calling {nameof(Create)}.");
            }

            var renderer = new RecycleRenderer<TRenderable, TDto>(objectResolver, template, parent);
            disposable = renderer;
            return renderer;
        }

        private void OnDestroy()
        {
            disposable?.Dispose();
        }
    }
}
