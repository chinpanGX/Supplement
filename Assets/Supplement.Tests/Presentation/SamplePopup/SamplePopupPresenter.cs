using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation.Abstractions;
using UnityEngine;

namespace Supplement.Tests.Presentation
{
    public class SamplePopupPresenter : IPresenter
    {
        private readonly ViewDto dto;
        private readonly CancellationTokenSource cts = new();
        
        private IView view;

        public SamplePopupPresenter(ViewDto dto)
        {
            this.dto = dto;
            Debug.Log("[SamplePopupPresenter] constructed.");
        }

        public async UniTask RenderAsync(IView view)
        {
            this.view = view;
            await this.view.RenderAsync(dto);
            Debug.Log("[SamplePopupPresenter] rendered the view.");
        }

        public void Dispose()
        {
            view.Dispose();
            cts?.Cancel();
            cts?.Dispose();
            Debug.Log("[SamplePopupPresenter] disposed.");
        }
    }
}