using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SoulsFormats.Util
{
    public class NoOodleFoundException : Exception
    {
        public NoOodleFoundException(string message) : base(message) { }
    }
}
