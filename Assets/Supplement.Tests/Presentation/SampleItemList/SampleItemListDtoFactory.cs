using System.Linq;
using Supplement.Tests.Application.Abstractions;
using Supplement.Tests.Presentation.Abstractions;
using ItemDto = Supplement.Tests.Presentation.Abstractions.ItemDto;

namespace Supplement.Tests.Presentation
{
    public class SampleItemListDtoFactory : ISampleItemListViewDtoFactory
    {
        private readonly IItemService itemService;
        public SampleItemListDtoFactory(IItemService itemService)
        {
            this.itemService = itemService;
        }
        
        public ItemListViewDto CreateDto(bool useGlobalMessaging)
        {
            var items = itemService.GetAll()
                .Select(x => new ItemDto()
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        UseGlobalMessaging = useGlobalMessaging
                    }
                )
                .ToEquatableReadOnlyList();

            return new ItemListViewDto(GetTitle(useGlobalMessaging), items, useGlobalMessaging);
        }
        
        private string GetTitle(bool useGlobalMessaging)
        {
            return useGlobalMessaging ? "Sample Item List (Global Messaging)" : "Sample Item List (Local Messaging)";
        }
    }
}