using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation.Abstractions;
using UnityEngine;
using VContainer;

namespace Supplement.Tests.Presentation
{
    public class SamplePopupDebugLogView : IView
    {
        private IPresenter presenter;

        [Inject]
        public void Construct(IPresenter presenter)
        {
            this.presenter = presenter;
            Debug.Log("[SamplePopupDebugLogView] constructed.");
        }
        
        public UniTask RenderAsync(ViewDto dto)
        {
            if (dto is SamplePopupDto samplePopupDto)
            {
                var fullMessage = $"[SamplePopupDebugLogView] Title: {samplePopupDto.Title}, Message: {samplePopupDto.Message}";
                Debug.Log(fullMessage);
            }
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            Debug.Log("[SamplePopupDebugLogView] disposed.");
        }
    }
}