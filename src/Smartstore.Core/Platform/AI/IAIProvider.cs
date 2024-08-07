using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    public partial interface IAIProvider : IProvider
    {
        /// <summary>
        /// Gets a value indicating whether the provider is configured.
        /// </summary>
        /// <returns></returns>
        bool IsConfigured();

        /// <summary>
        /// Gets a value indicating whether the provider supports the given <paramref name="feature"/>.
        /// </summary>
        bool Supports(AIProviderFeatures feature);

        /// <summary>
        /// Gets <see cref="RouteInfo"/> for the given modal dialog type.
        /// </summary>
        RouteInfo GetDialogRoute(AIDialogType modalDialogType);

        Task<string> GetAnswerAsync(string prompt);

        IAsyncEnumerable<string> GetAnswerStreamAsync(string prompt);

        Task<List<string>> GetImageUrlsAsync(string prompt, int numberOfImages);
    }
}