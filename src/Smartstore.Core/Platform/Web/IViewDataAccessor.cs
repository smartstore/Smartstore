#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Provides global access to the current ViewData dictionary.
    /// </summary>
    public interface IViewDataAccessor : IFilterMetadata
    {
        /// <summary>
        /// The current root ViewData instance. ViewData is populated right before the execution of any controller action.
        /// </summary>
        ViewDataDictionary? ViewData { get; }
    }
}
