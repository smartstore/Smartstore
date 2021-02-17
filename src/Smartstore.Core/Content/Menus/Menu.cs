using Smartstore.Core.Web;

namespace Smartstore.Core.Content.Menus
{
    public class Menu
    {
        public bool NameIsRequired => true;

        public string Template { get; set; }

        public RouteInfo Route { get; set; }
    }
}
