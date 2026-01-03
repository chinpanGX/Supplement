namespace Supplement.Tests.Application.Abstractions
{
    public struct ItemDto
    {
        public readonly int Id;
        public readonly int Amount;
        
        public ItemDto(int id, int amount)
        {
            Id = id;
            Amount = amount;
        }
    }
}