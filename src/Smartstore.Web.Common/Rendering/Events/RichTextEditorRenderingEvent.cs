using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.Rendering.Events
{
    /// <summary>
    /// Event that occurs when a rich text editor is being rendered. 
    /// Can be used to inject custom widgets into the editor's common resources zone, 
    /// which is located at the end of the document.
    /// </summary>
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

        /// <summary>
        /// A list of widgets to be rendered within the rich text editor.
        /// </summary>
        public List<Widget> Widgets { get; set; } = [];
    }
}