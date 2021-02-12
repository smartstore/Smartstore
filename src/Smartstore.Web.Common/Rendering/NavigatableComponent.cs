using Microsoft.AspNetCore.Routing;

namespace Smartstore.Web.Rendering
{
    // TODO: (mh) (core) the name Component stands in name conflict to net core Components.
    public abstract class NavigatableComponent : INavigatable // ,Component
    {
        private string _actionName;
        private string _controllerName;
        private string _routeName;
        private string _url;

        protected NavigatableComponent()
        {
            this.RouteValues = new RouteValueDictionary();
        }

        public string ActionName
        {
            get => _actionName;
            set
            {
                _actionName = value;
                _routeName = _url = null;
            }
        }

        public string ControllerName
        {
            get => _controllerName;
            set
            {
                _controllerName = value;
                _routeName = _url = null;
            }
        }

        public string RouteName
        {
            get => _routeName;
            set
            {
                _routeName = value;
                _controllerName = _actionName = _url = null;
            }
        }

        public RouteValueDictionary RouteValues
        {
            get;
            set;
        }

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                _routeName = _controllerName = _actionName = null;
                RouteValues.Clear();

            }
        }

        public ModifiedParameter ModifiedParam
        {
            get;
            protected set;
        }
    }
}
