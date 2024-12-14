namespace Smartstore.Core.AI
{
    /// <summary>
    /// Respresents a message role.
    /// </summary>
    public static partial class KnownAIMessageRoles
    {
        /// <summary>
        /// Used to provide content (requests or comments) for the assistant to respond to.
        /// </summary>
        /// <example>Who won the world series in 2020?</example>
        public const string User = "user";

        /// <summary>
        /// Used to set the behavior of the assistant.
        /// </summary>
        /// <example>You are a helpful assistant.</example>
        public const string System = "system";

        /// <summary>
        /// Used for assistant responses, but can also be written by the client to give examples of desired behavior.
        /// </summary>
        /// <example>The Los Angeles Dodgers won the World Series in 2020.</example>
        public const string Assistant = "assistant";
    }
}
