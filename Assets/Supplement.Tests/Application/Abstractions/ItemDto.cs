using System;

namespace Supplement.Tests.Application.Abstractions
{
    public struct ItemDto : IEquatable<ItemDto>
    {
        public readonly int Id;
        public readonly int Amount;

        public ItemDto(int id, int amount)
        {
            Id = id;
            Amount = amount;
        }

        public bool Equals(ItemDto other) => Id == other.Id && Amount == other.Amount;

        public override bool Equals(object obj) => obj is ItemDto other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, Amount);
    }
}