namespace Supplement.Tests.Presentation.Abstractions
{
    public interface ISampleItemListViewDtoFactory
    {
        ItemListViewDto CreateDto(bool useGlobalMessaging);
        ItemListViewDto CreateDtoForUseGlobalMessaging()
        {
            return CreateDto(true);
        }
        ItemListViewDto CreateDtoForUseHierarchyMessaging()
        {
            return CreateDto(false);
        }
    }
}