using System;
using System.Runtime.Serialization;

namespace Smartstore.Net.Mail
{
    [Serializable]
    public class EmailException : Exception
    {
        public EmailException(string message)
            : base(message)
        {
        }

        public EmailException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public EmailException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MailSenderException : Exception
    {
        public MailSenderException(string message)
            : base(message)
        {
        }

        public MailSenderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MailSenderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}