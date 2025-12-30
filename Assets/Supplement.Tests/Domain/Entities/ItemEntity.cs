using System;

namespace Supplement.Tests.Domain
{
    public class ItemEntity
    {
        public readonly int Id;

        public ItemEntity(int itemId, int amount)
        {
            Id = itemId;
            Amount = amount;
        }

        public ItemEntity(ItemEntity from)
            : this(from.Id, from.Amount)
        {
        }
        public int Amount { get; private set; }

        public static ItemEntity CreateNew(int itemId, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.");
            }

            return new ItemEntity(itemId, amount);
        }

        public ItemEntity AddAmount(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.");
            }

            var newEntity = new ItemEntity(this);
            newEntity.Amount += amount;
            return newEntity;
        }
    }
}