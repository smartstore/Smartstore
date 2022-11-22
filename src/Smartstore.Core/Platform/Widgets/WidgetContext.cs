#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Widgets
{
    public class WidgetContext
    {
        public WidgetContext(ActionContext actionContext)
            : this(actionContext, null)
        {
        }

        public WidgetContext(ActionContext actionContext, object? model)
        {
            ActionContext = Guard.NotNull(actionContext, nameof(actionContext));
            Model = model;

            if (actionContext is ViewContext viewContext)
            {
                ViewData = viewContext.ViewData;
                TempData = viewContext.TempData;
                Writer = viewContext.Writer;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ActionContext"/>
        /// </summary>
        public ActionContext ActionContext { get; set; }

        /// <summary>
        /// Gets the <see cref="HttpContext"/>
        /// </summary>
        public HttpContext HttpContext 
        { 
            get => ActionContext.HttpContext;
        }

        /// <summary>
        /// Gets or sets the name of the parent zone.
        /// </summary>
        public string? Zone { get; set; }

        /// <summary>
        /// Gets or sets the call site model.
        /// </summary>
        public object? Model { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewDataDictionary"/>.
        /// </summary>
        public ViewDataDictionary? ViewData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITempDataDictionary"/>.
        /// </summary>
        public ITempDataDictionary? TempData { get; set; }

        /// <summary>
        /// Gets the <see cref="TextWriter"/> for output.
        /// </summary>
        public TextWriter? Writer { get; }
    }
}
