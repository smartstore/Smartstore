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
}
