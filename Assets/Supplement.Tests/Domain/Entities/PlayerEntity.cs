namespace Supplement.Tests.Domain
{
    public class PlayerEntity
    {
        public readonly string UniqueId;
        public readonly int Level;

        public PlayerEntity(string uniqueId, int level)
        {
            UniqueId = uniqueId;
            Level = level;
        }
    }
}