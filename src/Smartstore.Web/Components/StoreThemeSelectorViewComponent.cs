using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Theming;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Components
{
    public class StoreThemeSelectorViewComponent : SmartViewComponent
    {
        private readonly Lazy<IThemeRegistry> _themeRegistry;
        private readonly ThemeSettings _themeSettings;
        private readonly IThemeContext _themeContext;

        public StoreThemeSelectorViewComponent(Lazy<IThemeRegistry> themeRegistry, ThemeSettings themeSettings, IThemeContext themeContext)
        {
            _themeRegistry = themeRegistry;
            _themeSettings = themeSettings;
            _themeContext = themeContext;
        }

        public IViewComponentResult Invoke()
        {
            if (!_themeSettings.AllowCustomerToSelectTheme)
                return Empty();

            var currentTheme = _themeRegistry.Value.GetThemeManifest(_themeContext.WorkingThemeName);

            ViewBag.CurrentStoreTheme = new StoreThemeModel
            {
                Name = currentTheme.ThemeName,
                Title = currentTheme.ThemeTitle
            };

            ViewBag.AvailableStoreThemes = _themeRegistry.Value
                .GetThemeManifests()
                .Select(x =>
                {
                    return new StoreThemeModel
                    {
                        Name = x.ThemeName,
                        Title = x.ThemeTitle
                    };
                })
                .ToList();

            return View();
        }
    }
}
