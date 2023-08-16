using System;

namespace HootLib.Objects.Exceptions
{
    /// <summary>
    /// The exception that is thrown when CSV parsing goes awry for some reason.
    /// </summary>
    public class ParsingException : Exception
    {
        public ParsingException() { }

        public ParsingException(string message) : base(message) { }
    }
}