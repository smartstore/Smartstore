using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Content.Menus
{
    ///// <summary>
    ///// Provides methods to resolve link expressions.
    ///// </summary>
    //public partial interface ILinkResolver
    //{
    //    /// <summary>
    //    /// Resolves a link expression.
    //    /// </summary>
    //    /// <param name="linkExpression">
    //    /// Link expression, e.g. product:123, category:234, topic:2, topic:aboutus etc.
    //    /// Supported entities are product, category, manufacturer and topic. Value of topic can
    //    /// either be the topic id or system name.
    //    /// </param>
    //    /// <param name="roles">Customer roles to check access for. <c>null</c> to use current customer's roles.</param>
    //    /// <param name="languageId">Language identifier. 0 to use current working language.</param>
    //    /// <param name="storeId">Store identifier. 0 to use current store.</param>
    //    /// <returns>LinkResolverResult</returns>
    //    Task<LinkResolverResult> ResolveAsync(string linkExpression, IEnumerable<CustomerRole> roles = null, int languageId = 0, int storeId = 0);
    //}

    /// <summary>
    /// Provides methods to create and resolve link expressions.
    /// </summary>
    public partial interface ILinkResolver
    {
        IEnumerable<LinkBuilderMetadata> GetBuilderMetadata();

        /// <summary>
        /// Resolves a link expression.
        /// </summary>
        /// <param name="expression">
        /// The parsed link expression.
        /// </param>
        /// <param name="roles">Customer roles to check access for. <c>null</c> to use current customer's roles.</param>
        /// <param name="languageId">Language identifier. 0 to use current working language.</param>
        /// <param name="storeId">Store identifier. 0 to use current store.</param>
        /// <returns>LinkResolutionResult</returns>
        Task<LinkResolutionResult> ResolveAsync(LinkExpression expression, IEnumerable<CustomerRole> roles = null, int languageId = 0, int storeId = 0);
    }

    public static class ILinkResolverExtensions
    {
        /// <inheritdoc cref="ILinkResolver.ResolveAsync(LinkExpression, IEnumerable{CustomerRole}, int, int)"/>
        /// <param name="expression">
        /// Link expression, e.g. product:123, category:234, topic:2, topic:aboutus etc.
        /// Supported schemas are product, category, manufacturer, topic, url and file. Target of topic can
        /// either be the topic id or system name. Custom providers may provide more schemas.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<LinkResolutionResult> ResolveAsync(this ILinkResolver resolver, 
            string expression, 
            IEnumerable<CustomerRole> roles = null, 
            int languageId = 0, 
            int storeId = 0)
        {
            return resolver.ResolveAsync(LinkExpression.Parse(expression), roles, languageId, storeId);
        }
    }
}
