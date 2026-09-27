using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Supplement.Tests.Presentation
{
    public class SampleItemListView : MonoBehaviour, IView
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject itemTemplate;
        [SerializeField] private RecycleRendererFactory itemElementFactory;

        private RecycleRenderer<ItemElement, ItemDto> itemElementPool;
        
        private ISampleItemListPresenter presenter;
        
        [Inject]
        public void Construct(ISampleItemListPresenter presenter)
        {
            this.presenter = presenter;
        }

        public async UniTask RenderAsync(ViewDto dto)
        {
            if (dto is ItemListViewDto itemListDto)
            {
                if (titleText != null)
                {
                    titleText.text = itemListDto.Title;
                }
                
                await itemElementPool.RenderAsync(itemListDto.Items);
            }
            
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(presenter.Pop);
        }

        public void Dispose()
        {
            itemElementPool.Dispose();
            Destroy(gameObject);
        }

        private void Awake()
        {
            itemElementPool = itemElementFactory.Create<ItemElement, ItemDto>();
            var messageBroker = this.GetOrAddComponent<HierarchyMessageBroker>();
            messageBroker.Subscribe<IncrementItemAmountMessage>(x => presenter.AddItemAmount(x.ItemId));
            messageBroker.Subscribe<DecrementItemAmountMessage>(x => presenter.SubtractItemAmount(x.ItemId));
        }
    }
}