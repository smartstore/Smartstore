namespace Smartstore.Core.AI
{
    /// <summary>
    /// Interface for AI chat cache.
    /// </summary>
    public interface IAIChatCache
    {
        /// <summary>
        /// Generates a new session token.
        /// </summary>
        string GenerateSessionToken();

        /// <summary>
        /// Gets the AI chat from cache.
        /// </summary>
        /// <param name="token">The token to get chat for.</param>
        /// <returns>The chat instance.</returns>
        Task<AIChat> GetAsync(string token);

        /// <summary>
        /// Put the AI chat into cache.
        /// </summary>
        /// <param name="slidingExpiration">The sliding expiration time. Default is 20 minutes.</param>
        Task PutAsync(string token, AIChat chat, TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Removes the AI chat from cache.
        /// </summary>
        /// <param name="token"></param>
        Task RemoveAsync(string token);
    }
}
