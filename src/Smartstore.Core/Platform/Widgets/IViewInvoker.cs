using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Invokes and renders (partial) views and components outside of controllers.
    /// </summary>
    public interface IViewInvoker
    {
        ViewDataDictionary ViewData { get; }

        /// <summary>
        /// Gets an <see cref="ActionContext"/> instance for the given <paramref name="module"/> area
        /// so that view resolver looks up pathes in the module directory first.
        /// </summary>
        /// <param name="context">The original action context</param>
        /// <param name="module">The module to get action context for.</param>
        ActionContext GetActionContext(ActionContext context, string module);

        /// <summary>
        /// Invokes a view and writes its html content to given <paramref name="writer"/>.
        /// </summary>
        Task InvokeViewAsync(ActionContext context, ViewResult result, TextWriter writer);

        /// <summary>
        /// Invokes a partial view and writes its html content to given <paramref name="writer"/>.
        /// </summary>
        Task InvokePartialViewAsync(ActionContext context, PartialViewResult result, TextWriter writer);

        /// <summary>
        /// Invokes a view component and writes its html content to given <paramref name="writer"/>.
        /// </summary>
        Task InvokeComponentAsync(ActionContext context, ViewComponentResult result, TextWriter writer);
    }
}
