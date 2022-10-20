namespace Smartstore.Data.Hooks
{
    public sealed class HookException : Exception
    {
        public HookException(string message)
            : base(message) 
        { 
        }

        public HookException(string message, Exception innerException)
            : base(message, innerException) 
        { 
        }
    }
}
