using System;

namespace GameCore
{
    public class UnknownCommandException : Exception
    {
        public string CommandName { get; }

        public UnknownCommandException(string commandName) : base($"Invalid command: {commandName}")
        {
            CommandName = commandName;
        }
    }
}
