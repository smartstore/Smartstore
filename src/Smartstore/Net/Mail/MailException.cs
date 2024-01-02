using System.Runtime.Serialization;

namespace Smartstore.Net.Mail
{
    [Serializable]
    public class MailException : Exception
    {
        public MailException(string message)
            : base(message)
        {
        }

        public MailException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}