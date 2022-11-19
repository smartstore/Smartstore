using Smartstore.Core.Widgets;

namespace Smartstore.Core.Content.Menus
{
    public partial class LinkBuilderMetadata
    {
        public int Order { get; init; }
        public string Schema { get; init; }
        public string Icon { get; init; }
        public string ResKey { get; init; }
        public Widget Widget { get; init; }
    }

    /// <summary>
    /// Provides link translator and UI builder metadata for custom link expressions.
    /// </summary>
    public partial interface ILinkProvider
    {
        int Order { get; }

        /// <summary>
        /// Translates a link expression.
        /// </summary>
        /// <param name="expression">
        /// The parsed link expression.
        /// </param>
        /// <param name="languageId">Language identifier. 0 = working language.</param>
        /// <param name="storeId">Store identifier. 0 = current store.</param>
        /// <returns><see cref="LinkTranslationResult"/> instance or <c>null</c></returns>
        Task<LinkTranslationResult> TranslateAsync(LinkExpression expression, int storeId, int languageId);

        /// <summary>
        /// Gets metadata of all links expression patterns supported by this provider.
        /// </summary>
        /// <returns>List of <see cref="LinkBuilderMetadata"/> instances.</returns>
        IEnumerable<LinkBuilderMetadata> GetBuilderMetadata();
    }
}
