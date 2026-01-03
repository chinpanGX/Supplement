namespace Supplement.Tests.Presentation.Abstractions
{
    public class ItemListViewDto : ViewDto
    {
        public readonly string Title;
        public readonly EquatableReadOnlyList<ItemDto> Items;
        public readonly bool UseGlobalMessaging;
        
        public ItemListViewDto(string title, EquatableReadOnlyList<ItemDto> items, bool useGlobalMessaging)
        {
            Title = title;
            Items = items;
            UseGlobalMessaging = useGlobalMessaging;
        }
    }
}