using System;

namespace Supplement.Loader.Abstractions
{
    public class AssetLoadFailedException : Exception
    {
        public AssetLoadFailedException(string message) : base(message)
        {
        }
    }
}