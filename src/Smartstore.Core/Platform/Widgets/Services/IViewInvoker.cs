#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Invokes and renders (partial) views and components outside of controllers.
    /// </summary>
    public interface IViewInvoker
    {
        /// <summary>
        /// Gets the current <see cref="ViewDataDictionary"/>.
        /// </summary>
        ViewDataDictionary? ViewData { get; }

        /// <summary>
        /// Gets an <see cref="ActionContext"/> instance for the given <paramref name="module"/> area
        /// so that view resolver looks up pathes in the module directory first.
        /// </summary>
        /// <param name="context">The original action context or <c>null</c> to construct a fresh context.</param>
        /// <param name="module">The module to get action context for.</param>
        ActionContext GetActionContext(ActionContext? context, string? module);

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        Task<HtmlString> InvokeComponentAsync(ActionContext context, ViewComponentResult result);

        /// <summary>
        /// Invokes a partial view and returns its html content.
        /// </summary>
        Task<HtmlString> InvokePartialViewAsync(ActionContext context, PartialViewResult result);

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        Task<HtmlString> InvokeViewAsync(ActionContext context, ViewResult result);
    }
}
