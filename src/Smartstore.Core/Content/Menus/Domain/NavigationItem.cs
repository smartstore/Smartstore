using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace Smartstore.Core.Content.Menus
{
    public abstract class NavigationItem : INavigatable, IHideObjectMembers
    {
        private bool _selected;
        private bool _enabled;
        private string _actionName;
        private string _controllerName;
        private string _routeName;
        private string _url;

        public NavigationItem()
        {
            _enabled = true;
        }

        public AttributeDictionary HtmlAttributes { get; set; } = new();
        public AttributeDictionary LinkHtmlAttributes { get; set; } = new();
        public AttributeDictionary BadgeHtmlAttributes { get; set; } = new();

        public RouteValueDictionary RouteValues { get; set; } = new();

        [IgnoreDataMember]
        public ModifiedParameter ModifiedParam { get; } = new();

        /// <summary>
        /// Merges attributes of <see cref="HtmlAttributes"/> and <see cref="LinkHtmlAttributes"/> into one combined dictionary.
        /// </summary>
        /// <returns>New attribute dictionary instance with combined attributes.</returns>
        public AttributeDictionary GetCombinedAttributes()
        {
            if (HtmlAttributes == null && LinkHtmlAttributes == null)
            {
                return null;
            }

            var combined = new AttributeDictionary();
            combined.AddRange(HtmlAttributes ?? LinkHtmlAttributes);

            if (HtmlAttributes != null && LinkHtmlAttributes != null)
            {
                combined.Merge(LinkHtmlAttributes);
            }

            return combined;
        }

        public string ImageUrl { get; set; }

        public int? ImageId { get; set; }

        public string IconLibrary { get; set; }

        public string Icon { get; set; }

        public string IconClass { get; set; }

        public string Text { get; set; }

        public bool Rtl { get; set; }

        public string Summary { get; set; }

        public string BadgeText { get; set; }

        public int BadgeStyle { get; set; }

        public bool Visible { get; set; } = true;

        public bool Encoded { get; set; } = true;

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                if (_selected)
                {
                    _enabled = true;
                }
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!_enabled)
                {
                    _selected = false;
                }
            }
        }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ActionName
        {
            get => _actionName;
            set
            {
                if (_actionName != value)
                {
                    _actionName = value;
                    _routeName = _url = null;
                }
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ControllerName
        {
            get => _controllerName;
            set
            {
                if (_controllerName != value)
                {
                    _controllerName = value;
                    _routeName = _url = null;
                }
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RouteName
        {
            get => _routeName;
            set
            {
                if (_routeName != value)
                {
                    _routeName = value;
                    _controllerName = _actionName = _url = null;
                }
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    _routeName = _controllerName = _actionName = null;
                    RouteValues.Clear();
                }
            }
        }

        /// <summary>
        /// Checks whether action/controller or routeName or url has been specified.
        /// </summary>
		public bool HasRoute
        {
            get => _actionName != null || _routeName != null || _url != null;
        }

        /// <summary>
        /// Checks whether url has been specified with '#' or 'javascript:void()' or empty string.
        /// </summary>
        public bool IsVoid()
        {
            // Perf: order from most to least common
            return _url != null && (_url == "#" || _url.StartsWith("javascript:void") || _url == string.Empty || _url.IsWhiteSpace());
        }

        public override string ToString()
        {
            if (Text.HasValue())
            {
                return Text;
            }

            return base.ToString();
        }
    }
}
