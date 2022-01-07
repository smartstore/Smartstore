using Autofac;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Content.Menus
{
    public static class MenuUrlHelperExtensions
    {
        /// <summary>
        /// Resolves a link to a topic page.
        /// </summary>
        /// <param name="systemName">The system name of the topic.</param>
        /// <returns>Link</returns>
        /// <remarks>
        /// This method returns an empty string in following cases:
        /// - the requested page does not exist.
        /// - the current user has no permission to acces the page.
        /// </remarks>
        public static Task<string> TopicAsync(this IUrlHelper urlHelper, string systemName, bool popup = false)
        {
            Guard.NotEmpty(systemName, nameof(systemName));

            var expression = "topic:" + systemName;
            if (popup)
            {
                expression += "?popup=true";
            }

            return EntityAsync(urlHelper, expression);
        }

        /// <summary>
        /// Resolves a link label for a topic page.
        /// The label is either the page short title or title.
        /// </summary>
        /// <param name="systemName">The system name of the topic.</param>
        /// <returns>Label</returns>
        /// <remarks>
        /// This method returns an empty string if the requested page does not exist.
        /// </remarks>
        public static Task<string> TopicLabelAsync(this IUrlHelper urlHelper, string systemName)
        {
            Guard.NotEmpty(systemName, nameof(systemName));

            return EntityLabelAsync(urlHelper, "topic:" + systemName);
        }

        /// <summary>
        /// Resolves a link to a system internal entity like product, topic, category or manufacturer.
        /// </summary>
        /// <param name="expression">A link expression as supported by the <see cref="ILinkResolver"/></param>
        /// <returns>Link</returns>
        /// <remarks>
        /// This method returns an empty string in following cases:
        /// - the requested entity does not exist.
        /// - the current user has no permission to acces the entity.
        /// </remarks>
        public static async Task<string> EntityAsync(this IUrlHelper urlHelper, string expression)
        {
            Guard.NotEmpty(expression, nameof(expression));

            var linkResolver = urlHelper.ActionContext.HttpContext.GetServiceScope().Resolve<ILinkResolver>();
            var link = await linkResolver.ResolveAsync(expression);

            if (link.Status == LinkStatus.Ok)
            {
                return link.Link.EmptyNull();
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolves a link label for a system internal entity like product, topic, category or manufacturer.
        /// The label is either the entity short title, title or name, whichever is applicable.
        /// </summary>
        /// <param name="expression">A link expression as supported by the <see cref="ILinkResolver"/></param>
        /// <returns>Label</returns>
        /// <remarks>
        /// This method returns an empty string if the requested entity does not exist.
        /// </remarks>
        public static async Task<string> EntityLabelAsync(this IUrlHelper urlHelper, string expression)
        {
            Guard.NotEmpty(expression, nameof(expression));

            var linkResolver = urlHelper.ActionContext.HttpContext.GetServiceScope().Resolve<ILinkResolver>();
            var link = await linkResolver.ResolveAsync(expression);

            if (link.Status == LinkStatus.Ok || link.Status == LinkStatus.Forbidden)
            {
                return link.Label;
            }

            return string.Empty;
        }
    }
}
