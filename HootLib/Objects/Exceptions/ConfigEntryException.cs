using System;

namespace HootLib.Objects.Exceptions
{
    public class ConfigEntryException : Exception
    {
        public ConfigEntryException() { }

        public ConfigEntryException(string message) : base(message) { }
    }
}