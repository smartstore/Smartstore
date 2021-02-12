using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Web.Rendering
{
    public interface IHtmlAttributesContainer
    {
        AttributeDictionary HtmlAttributes
        {
            get;
        }
    }
}
