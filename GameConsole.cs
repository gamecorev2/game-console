using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GameCore
{
    public class GameConsole
    {
        private readonly Dictionary<Type, Func<string, object>> m_converters = new Dictionary<Type, Func<string, object>>();

        private readonly Dictionary<string, Delegate> m_commands = new Dictionary<string, Delegate>();

        private string m_commandPrefix = "/";

        private bool m_validated = false;

        public char ArgumentSeparator { get; set; } = ' ';

        public string CommandPrefix
        {
            get => m_commandPrefix;
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));
                m_commandPrefix = value;
            }
        }

        public bool IsValidated => m_validated;

        public string[] Commands => m_commands.Keys.ToArray();

        public GameConsole(bool addDefaultConverters = true)
        {
            if (addDefaultConverters)
            {
                AddConverter(Convert.ToString);
                AddConverter(Convert.ToChar);
                AddConverter(Convert.ToSByte);
                AddConverter(Convert.ToByte);
                AddConverter(Convert.ToInt16);
                AddConverter(Convert.ToUInt16);
                AddConverter(Convert.ToInt32);
                AddConverter(Convert.ToUInt32);
                AddConverter(Convert.ToInt64);
                AddConverter(Convert.ToUInt64);
                AddConverter(Convert.ToBoolean);
                AddConverter(Convert.ToSingle);
                AddConverter(Convert.ToDouble);
            }
        }

        public void RegisterCommand<T>(string name, Action<T> handler)
        {
            if (m_commands.ContainsKey(name))
                throw new CommandAlreadyRegisteredException();
            m_commands[name] = handler;
            m_validated = false;
        }

        public void AddConverter<T>(Func<string, T> converter)
        {
            AddConverter(typeof(T), input => converter(input));
        }

        public void AddConverter(Type type, Func<string, object> converter)
        {
            m_converters.Add(type, converter);
        }

        public bool HandleInput(string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (!input.StartsWith(CommandPrefix)) return false;
            input = input.Substring(CommandPrefix.Length);

            string[] parts = input.Split(ArgumentSeparator);
            if (parts.Length == 0) return false;

            string commandName = parts[0];

            AssertCommandExists(commandName);

            string[] args;
            if (parts.Length > 1)
            {
                args = new string[parts.Length - 1];
                Array.Copy(parts, 1, args, 0, args.Length);
            }
            else args = Array.Empty<string>();

            ExecuteCommand(commandName, args);
            return true;
        }

        public void ExecuteCommand(string commandName, params string[] args)
        {
            AssertValidated();
            AssertCommandExists(commandName);

            Delegate command = m_commands[commandName];
            ParameterInfo[] parameters = command.Method.GetParameters();
            
            object[] argVs = new object[parameters.Length];

            foreach (
                (int i, (ParameterInfo parameter, string value)) in 
                new Enumerate<(ParameterInfo, string)>(new Zip<ParameterInfo, string>(parameters, args))
                )
            {
                argVs[i] = m_converters[parameter.ParameterType].Invoke(value);
            }

            ExecuteCommand(commandName, argVs);
        }

        public void ExecuteCommand(string commandName, params object[] args)
        {
            AssertValidated();
            AssertCommandExists(commandName);

            m_commands[commandName].DynamicInvoke(args);
        }

        public bool CommandExists(string commandName) => m_commands.ContainsKey(commandName);

        private bool AssertCommandExists(string commandName)
        {
            if (CommandExists(commandName)) return true;
            throw new UnknownCommandException(commandName);
        }

        private void AssertValidated()
        {
            if (m_validated) return;
            throw new InvalidOperationException($"The console must be validated before being used");
        }

        public void Validate()
        {
            foreach (Delegate @delegate in m_commands.Values)
            {
                foreach (ParameterInfo parameter in @delegate.Method.GetParameters())
                {
                    if (!m_converters.ContainsKey(parameter.ParameterType))
                        throw new UnconvertibleCommandParameterException(parameter);
                }
            }
            m_validated = true;
        }
    }
}
