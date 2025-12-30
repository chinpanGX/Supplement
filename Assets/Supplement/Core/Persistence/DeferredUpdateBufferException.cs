using System;

namespace Supplement.Core
{
    public class DeferredUpdateBufferException : Exception
    {
        public DeferredUpdateBufferException(string message) : base(message)
        {
        }
    }
}