using System;

namespace Supplement.Core
{
    public class SaveDataRepositoryException : Exception
    {
        public SaveDataRepositoryException(string message) : base(message)
        {
        }

        public SaveDataRepositoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}