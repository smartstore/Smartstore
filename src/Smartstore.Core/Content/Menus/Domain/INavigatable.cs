using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Content.Menus
{
    public interface INavigatable
    {
        string ControllerName { get; set; }
        string ActionName { get; set; }
        string RouteName { get; set; }

        string Url { get; set; }

        RouteValueDictionary RouteValues { get; }
        ModifiedParameter ModifiedParam { get; }
    }
}
