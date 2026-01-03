using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using VContainer;

namespace Supplement.Tests.Presentation
{
    public class SampleItemListView : MonoBehaviour, IView
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject itemTemplate;

        private ObjectPool<GameObject> itemElementPool;
        private readonly List<GameObject> cachedItemElements = new();
        
        private ISampleItemListPresenter presenter;
        
        [Inject]
        public void Construct(ISampleItemListPresenter presenter)
        {
            this.presenter = presenter;
        }

        public UniTask RenderAsync(ViewDto dto)
        {
            foreach (var itemElement in cachedItemElements)
            {
                itemElementPool.Release(itemElement);
            }
            cachedItemElements.Clear();
            
            itemTemplate.SetActive(false);
            if (dto is ItemListViewDto itemListDto)
            {
                if (titleText != null)
                {
                    titleText.text = itemListDto.Title;
                }

                foreach (var itemDto in itemListDto.Items)
                {
                    var itemElementObj = itemElementPool.Get();
                    itemElementObj.transform.SetAsLastSibling();
                    var itemElement = itemElementObj.GetComponent<IRenderable<ItemDto>>();
                    itemElement.Render(itemDto);
                    cachedItemElements.Add(itemElementObj);
                }
            }
            
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(presenter.Pop);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            itemElementPool.Dispose();
            Destroy(gameObject);
        }

        private void Awake()
        {
            itemElementPool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(itemTemplate, itemTemplate.transform.parent);
                    obj.SetActive(true);
                    return obj;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false)
            );

            var messageBroker = this.GetOrAddComponent<HierarchyMessageBroker>();
            messageBroker.Subscribe<IncrementItemAmountMessage>(x => presenter.AddItemAmount(x.ItemId));
            messageBroker.Subscribe<DecrementItemAmountMessage>(x => presenter.SubtractItemAmount(x.ItemId));
        }
    }
}