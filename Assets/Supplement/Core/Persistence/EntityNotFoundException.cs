using System;

namespace Supplement.Core
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string message) : base(message)
        {
        }

        public EntityNotFoundException(string entityTypeName, string id)
            : base($"Entity of type {entityTypeName} with ID {id} was not found.")
        {
        }

        public EntityNotFoundException(string entityTypeName, int id)
            : base($"Entity of type {entityTypeName} with ID {id} was not found.")
        {
        }

        public EntityNotFoundException(string entityTypeName, long id)
            : base($"Entity of type {entityTypeName} with ID {id} was not found.")
        {
        }
    }
}