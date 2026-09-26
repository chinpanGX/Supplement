using Cysharp.Threading.Tasks;
using Supplement.Core;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Supplement.Tests.Presentation
{
    internal class ItemElement : MonoBehaviour, IAsyncRenderable<ItemDto>
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Button addButton;
        [SerializeField] private Button subButton;

        private ItemDto dto;

        private IMessageBroker messageBroker;

        public UniTask RenderAsync(ItemDto dto)
        {
            this.dto = dto;
            if (text != null)
            {
                text.text = $"Item {dto.Id}: {dto.Amount}";
            }

            if (dto.UseGlobalMessaging)
            {
                SetupGlobalMessaging();
            }
            else
            {
                SetupHierarchyMessageBroker();
            }
            return UniTask.CompletedTask;
        }

        private void SetupGlobalMessaging()
        {
            messageBroker = ObjectResolverGateway.Resolve<IMessageBroker>();
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(() =>
                {
                    messageBroker.Publish(new IncrementItemAmountMessage { ItemId = dto.Id });
                }
            );

            subButton.onClick.RemoveAllListeners();
            subButton.onClick.AddListener(() =>
                {
                    messageBroker.Publish(new DecrementItemAmountMessage { ItemId = dto.Id });
                }
            );
        }

        private void SetupHierarchyMessageBroker()
        {
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(() =>
                HierarchyMessageBroker.Publish(this, new IncrementItemAmountMessage { ItemId = dto.Id })
            );
            subButton.onClick.RemoveAllListeners();
            subButton.onClick.AddListener(() =>
                HierarchyMessageBroker.Publish(this, new DecrementItemAmountMessage { ItemId = dto.Id })
            );
        }
    }
}