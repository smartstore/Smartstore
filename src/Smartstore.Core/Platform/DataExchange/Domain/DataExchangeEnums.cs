namespace Smartstore.Core.DataExchange
{
    /// <summary>
    /// Data exchange abortion types.
    /// </summary>
    public enum DataExchangeAbortion
    {
        /// <summary>
        /// No abortion. Go on with processing.
        /// </summary>
        None = 0,

        /// <summary>
        /// Break item processing but not the rest of the execution. Typically used for demo limitations.
        /// </summary>
        Soft,

        /// <summary>
        /// Break processing immediately.
        /// </summary>
        Hard
    }

    public enum DataExchangeCompletionEmail
    {
        /// <summary>
        /// Always send a completion email.
        /// </summary>
        Always = 0,

        /// <summary>
        /// Only send a completion email if an error occurred.
        /// </summary>
        OnError,

        /// <summary>
        /// Never send a completion email.
        /// </summary>
        Never
    }
}
