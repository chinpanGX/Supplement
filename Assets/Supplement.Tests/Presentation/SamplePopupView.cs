using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation.Abstractions;
using UnityEngine;
using VContainer;

namespace Supplement.Tests.Presentation
{
    public class SamplePopupView : MonoBehaviour, IView
    {
        private IPresenter presenter;
        
        [Inject]
        public void Construct(IPresenter presenter)
        {
            this.presenter = presenter;
        }
        
        public UniTask RenderAsync(ViewDto dto)
        {
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}