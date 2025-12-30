namespace Supplement.Tests.Presentation
{
    public class SamplePopupDtoFactory : ISamplePopupDtoFactory
    {
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