using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation.Abstractions;
using TMPro;
using UnityEngine;
using VContainer;

namespace Supplement.Tests.Presentation
{
    public class SamplePopupView : MonoBehaviour, IView
    {
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI messageText;
        
        private IPresenter presenter;
        
        [Inject]
        public void Construct(IPresenter presenter)
        {
            this.presenter = presenter;
        }
        
        public UniTask RenderAsync(ViewDto dto)
        {
            if (dto is SamplePopupDto popupDto)
            {
                titleText.text = popupDto.Title;
                messageText.text = popupDto.Message;
            }
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}