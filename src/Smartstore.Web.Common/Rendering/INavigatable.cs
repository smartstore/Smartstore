using Microsoft.AspNetCore.Routing;

namespace Smartstore.Web.Rendering
{
    public interface INavigatable
    {
        string ControllerName
        {
            get;
            set;
        }

        string ActionName
        {
            get;
            set;
        }

        string RouteName
        {
            get;
            set;
        }

        RouteValueDictionary RouteValues
        {
            get;
        }

        ModifiedParameter ModifiedParam
        {
            get;
        }

        string Url
        {
            get;
            set;
        }
    }
}
