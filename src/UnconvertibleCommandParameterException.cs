using System;

namespace GameCore
{
    public class UnconvertibleCommandParameterException : Exception
    {
        public UnconvertibleCommandParameterException(System.Reflection.ParameterInfo parameter)
            : base($"Could not find a suitable converter for parameter {parameter.Name} of method {parameter.Member}")
        {

        }
    }
}
