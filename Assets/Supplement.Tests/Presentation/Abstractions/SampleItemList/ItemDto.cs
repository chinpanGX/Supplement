namespace Supplement.Tests.Presentation.Abstractions
{
    public record ItemDto
    {
        public int Id { get; init; }
        public int Amount { get; init; }
        public bool UseGlobalMessaging { get; init; }
    }
}