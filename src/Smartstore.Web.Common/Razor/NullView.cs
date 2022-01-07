using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Smartstore.Web.Razor
{
    public class NullView : IView
    {
        public static readonly NullView Instance = new();

        public string Path => string.Empty;

        public Task RenderAsync(ViewContext viewContext)
        {
            Guard.NotNull(viewContext, nameof(viewContext));
            return Task.CompletedTask;
        }
    }
}
