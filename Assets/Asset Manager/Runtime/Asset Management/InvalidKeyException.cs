using System;

namespace Skywatch.AssetManagement
{
    public class InvalidKeyException : Exception
    {
        public InvalidKeyException(string message) : base(message)
        {

        }
    }
}
