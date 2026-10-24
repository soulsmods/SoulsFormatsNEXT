using System;

namespace SoulsFormats.Exceptions
{
    /// <summary>
    /// Throw when the Oodle library could not be found.
    /// </summary>
    public class NoOodleFoundException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="NoOodleFoundException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NoOodleFoundException(string message) : base(message) { }

        /// <summary>
        /// Creates a new <see cref="NoOodleFoundException"/> caused by <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">Why no library could be loaded.</param>
        public NoOodleFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
