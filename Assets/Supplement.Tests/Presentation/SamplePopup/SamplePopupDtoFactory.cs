using Supplement.Tests.Application.Abstractions;

namespace Supplement.Tests.Presentation
{
    public class SamplePopupDtoFactory : ISamplePopupDtoFactory
    {
        private readonly IItemService itemService;
        
        public SamplePopupDto CreateSamplePopupDto()
        {
            return new SamplePopupDto()
            {
                Title = "Sample Popup",
                Message = "This is a sample popup message.",
            };
        }
    }
}