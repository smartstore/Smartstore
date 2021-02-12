using Smartstore.Core.Web;

namespace Smartstore.Web.TagHelpers
{
    public class Menu
    {
        public bool NameIsRequired => true;

        public string Template { get; set; }

        public RouteInfo Route { get; set; }
    }
}
