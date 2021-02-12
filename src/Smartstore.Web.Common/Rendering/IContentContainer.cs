using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Smartstore.Web.Rendering
{
    public interface IContentContainer
    {
        IDictionary<string, object> ContentHtmlAttributes
        {
            get;
        }

        HelperResult Content
        {
            get;
            set;
        }
    }
}
