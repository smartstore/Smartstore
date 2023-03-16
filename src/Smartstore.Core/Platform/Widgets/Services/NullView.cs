using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Smartstore.Core.Widgets
{
    public sealed class NullView : IView
    {
        public static NullView Instance { get; } = new();

        public string Path => string.Empty;

        public Task RenderAsync(ViewContext viewContext)
        {
            Guard.NotNull(viewContext, nameof(viewContext));
            return Task.CompletedTask;
        }
    }
}
