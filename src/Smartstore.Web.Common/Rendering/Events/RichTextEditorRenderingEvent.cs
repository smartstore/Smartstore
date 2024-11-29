using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.Rendering.Events
{
    // TODO: (mh) DESCRIBE please!
    public class RichTextEditorRenderingEvent(string flavor, ViewContext viewContext)
    {
        /// <summary>
        /// The rich text editor flavor, e.g. "summernote".
        /// </summary>
        public string Flavor { get; } = flavor;

        /// <summary>
        /// The view context.
        /// </summary>
        public ViewContext ViewContext { get; } = Guard.NotNull(viewContext);

        // TODO: (mh) DESCRIBE please!
        public List<Widget> Widgets { get; set; } = [];
    }
}