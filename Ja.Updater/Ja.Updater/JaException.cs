using System;
using System.Runtime.Serialization;

namespace Ja.Updater
{
    public class JaException : Exception
    {
        public JaException()
        {
        }

        public JaException(string message) : base(message)
        {
        }

        public JaException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected JaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
