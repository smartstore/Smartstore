namespace Smartstore.Core.Security
{
    public sealed class AccessDeniedException : UnauthorizedAccessException
    {
        public AccessDeniedException()
            : this(string.Empty)
        {
        }

        public AccessDeniedException(string message, string returnUrl = null)
            : base(message.EmptyNull())     // Do not pass null to avoid non-localized messages.
        {
            Data["ReturnUrl"] = returnUrl;
        }

        public AccessDeniedException(string message, Exception innerException, string returnUrl = null)
            : base(message.EmptyNull(), innerException)
        {
            Data["ReturnUrl"] = returnUrl;
        }
    }
}
