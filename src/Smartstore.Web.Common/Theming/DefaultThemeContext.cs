using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;

namespace Smartstore.Web.Theming
{
    public partial class DefaultThemeContext : IThemeContext
    {
        internal const string OverriddenThemeNameKey = "OverriddenThemeName";

        private readonly IWorkContext _workContext;
        private readonly ThemeSettings _themeSettings;
        private readonly IThemeRegistry _themeRegistry;
        private readonly HttpContext _httpContext;

        private bool _themeIsCached;
        private string _cachedThemeName;

        private ThemeManifest _currentTheme;

        public DefaultThemeContext(
            IWorkContext workContext,
            ThemeSettings themeSettings,
            IThemeRegistry themeRegistry,
            IHttpContextAccessor httpContextAccessor)
        {
            _workContext = workContext;
            _themeSettings = themeSettings;
            _themeRegistry = themeRegistry;
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
                if (!_themeRegistry.ThemeManifestExists(theme))
                {
                    var manifest = _themeRegistry.GetThemeManifests().FirstOrDefault();
                    if (manifest == null)
                    {
                        // no active theme in system. Throw!
                        throw Error.Application("At least one theme must be in active state, but the theme registry does not contain a valid theme package.");
                    }

                    theme = manifest.ThemeName;
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

                if (value.HasValue() && !_themeRegistry.ThemeManifestExists(value))
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

        public virtual ThemeManifest CurrentTheme
        {
            get
            {
                if (_currentTheme == null)
                {
                    var themeOverride = GetRequestTheme() ?? GetPreviewTheme();
                    if (themeOverride != null)
                    {
                        // the theme to be used can be overwritten on request/session basis (e.g. for live preview, editing etc.)
                        _currentTheme = _themeRegistry.GetThemeManifest(themeOverride);
                    }
                    else
                    {
                        _currentTheme = _themeRegistry.GetThemeManifest(WorkingThemeName);
                    }

                }

                return _currentTheme;
            }
        }

        public string GetRequestTheme()
        {
            try
            {
                return (string)_httpContext?.GetRouteData()?.DataTokens[OverriddenThemeNameKey];
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
                var dataTokens = _httpContext?.GetRouteData()?.DataTokens;

                if (dataTokens == null)
                {
                    return;
                }

                if (theme.HasValue())
                {
                    dataTokens[OverriddenThemeNameKey] = theme;
                }
                else if (dataTokens.ContainsKey(OverriddenThemeNameKey))
                {
                    dataTokens.Remove(OverriddenThemeNameKey);
                }

                _currentTheme = null;
            }
            catch 
            { 
            }
        }

        public string GetPreviewTheme()
        {
            try
            {
                var cookie = _httpContext.GetPreviewModeFromCookie();
                if (cookie != null)
                {
                    return cookie[OverriddenThemeNameKey].ToString().NullEmpty();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void SetPreviewTheme(string theme)
        {
            try
            {
                _httpContext.SetPreviewModeValueInCookie(OverriddenThemeNameKey, theme);
                _currentTheme = null;
            }
            catch 
            { 
            }
        }
    }
}
