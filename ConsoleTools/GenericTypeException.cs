using System;
using System.Runtime.Serialization;

namespace ConsoleTools
{
    [Serializable]
    public class GenericTypeException : Exception
    {
        public GenericTypeException()
        {
        }

        public GenericTypeException(string message) : base(message)
        {
        }

        public GenericTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GenericTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}