using Microsoft.AspNetCore.Http;
using Smartstore.Core.Theming;
using Smartstore.Core.Web;

namespace Smartstore.Web.Theming
{
    public partial class DefaultThemeContext : IThemeContext
    {
        internal const string OverriddenThemeNameKey = "OverriddenThemeName";

        private readonly IWorkContext _workContext;
        private readonly ThemeSettings _themeSettings;
        private readonly IThemeRegistry _themeRegistry;
        private readonly IPreviewModeCookie _previewCookie;
        private readonly HttpContext _httpContext;

        private bool _themeIsCached;
        private string _cachedThemeName;

        private ThemeDescriptor _currentTheme;

        public DefaultThemeContext(
            IWorkContext workContext,
            ThemeSettings themeSettings,
            IThemeRegistry themeRegistry,
            IPreviewModeCookie previewCookie,
            IHttpContextAccessor httpContextAccessor)
        {
            _workContext = workContext;
            _themeSettings = themeSettings;
            _themeRegistry = themeRegistry;
            _previewCookie = previewCookie;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public virtual string WorkingThemeName
        {
            get
            {
                if (_themeIsCached)
                {
                    return _cachedThemeName;
                }

                var customer = _workContext.CurrentCustomer;
                bool isUserSpecific = false;
                string theme = string.Empty;

                if (_themeSettings.AllowCustomerToSelectTheme)
                {
                    if (_themeSettings.SaveThemeChoiceInCookie)
                    {
                        theme = _httpContext.GetUserThemeChoiceFromCookie();
                    }
                    else if (customer != null)
                    {
                        theme = customer.GenericAttributes.WorkingThemeName;
                    }

                    isUserSpecific = theme.HasValue();
                }

                // default store theme
                if (string.IsNullOrEmpty(theme))
                {
                    theme = _themeSettings.DefaultTheme;
                }

                // ensure that theme exists
                if (!_themeRegistry.ContainsTheme(theme))
                {
                    var descriptor = _themeRegistry.GetThemeDescriptors().FirstOrDefault();
                    if (descriptor == null)
                    {
                        // no active theme in system. Throw!
                        throw Error.Application("At least one theme must be in active state, but the theme registry does not contain a valid theme package.");
                    }

                    theme = descriptor.Name;
                    if (isUserSpecific)
                    {
                        // the customer chosen theme does not exists (anymore). Invalidate it!
                        _httpContext.SetUserThemeChoiceInCookie(null);
                        if (customer != null)
                        {
                            customer.GenericAttributes.WorkingThemeName = null;
                            customer.GenericAttributes.SaveChanges();
                        }
                    }

                }

                // cache theme
                _cachedThemeName = theme;
                _themeIsCached = true;

                return theme;
            }
            set
            {
                if (!_themeSettings.AllowCustomerToSelectTheme)
                    return;

                if (value.HasValue() && !_themeRegistry.ContainsTheme(value))
                    return;

                _httpContext.SetUserThemeChoiceInCookie(value.NullEmpty());

                if (_workContext.CurrentCustomer != null)
                {
                    _workContext.CurrentCustomer.GenericAttributes.WorkingThemeName = value.NullEmpty();
                    _workContext.CurrentCustomer.GenericAttributes.SaveChanges();
                }

                // clear cache
                _themeIsCached = false;
            }
        }

        public virtual ThemeDescriptor CurrentTheme
        {
            get
            {
                if (_currentTheme == null)
                {
                    var themeOverride = GetRequestTheme() ?? GetPreviewTheme();
                    if (themeOverride != null)
                    {
                        // The theme to be used can be overwritten on request/session basis (e.g. for live preview, editing etc.)
                        _currentTheme = _themeRegistry.GetThemeDescriptor(themeOverride);
                    }
                    else
                    {
                        _currentTheme = _themeRegistry.GetThemeDescriptor(WorkingThemeName);
                    }

                }

                return _currentTheme;
            }
        }

        public string GetRequestTheme()
        {
            try
            {
                return _httpContext?.GetItem<string>(OverriddenThemeNameKey, forceCreation: false);
            }
            catch
            {
                return null;
            }
        }

        public void SetRequestTheme(string theme)
        {
            try
            {
                var items = _httpContext?.Items;

                if (items == null)
                {
                    return;
                }

                if (theme.HasValue())
                {
                    items[OverriddenThemeNameKey] = theme;
                }
                else if (items.ContainsKey(OverriddenThemeNameKey))
                {
                    items.Remove(OverriddenThemeNameKey);
                }

                _currentTheme = null;
            }
            catch
            {
            }
        }

        public string GetPreviewTheme()
        {
            return _previewCookie.GetOverride(OverriddenThemeNameKey);
        }

        public void SetPreviewTheme(string theme)
        {
            if (theme.HasValue())
            {
                _previewCookie.SetOverride(OverriddenThemeNameKey, theme);
            }
            else
            {
                _previewCookie.RemoveOverride(OverriddenThemeNameKey);
            }
        }
    }
}
