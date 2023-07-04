using System;

namespace SubnauticaCommons.Objects.Exceptions
{
    public class ConfigEntryException : Exception
    {
        public ConfigEntryException() { }

        public ConfigEntryException(string message) : base(message) { }
    }
}