using System;
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

        [Inject]
        public void Construct(IObjectResolver objectResolver)
        {
            this.objectResolver = objectResolver;
        }

        public RecycleRenderer<TRenderable, TDto> Create<TRenderable, TDto>()
            where TRenderable : Component, IAsyncRenderable<TDto>
            where TDto : class
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
