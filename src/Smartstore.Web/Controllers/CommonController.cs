using System;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Theming;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Controllers
{
    public class CommonController : SmartController
    {
        private readonly IThemeContext _themeContext;
        private readonly Lazy<IThemeRegistry> _themeRegistry;
        private readonly ThemeSettings _themeSettings;

        public CommonController(IThemeContext themeContext, Lazy<IThemeRegistry> themeRegistry, ThemeSettings themeSettings)
        {
            _themeContext = themeContext;
            _themeRegistry = themeRegistry;
            _themeSettings = themeSettings;
        }

        [HttpPost]
        public IActionResult ChangeTheme(string themeName, string returnUrl = null)
        {
            if (!_themeSettings.AllowCustomerToSelectTheme || (themeName.HasValue() && !_themeRegistry.Value.ThemeManifestExists(themeName)))
            {
                return NotFound();
            }

            _themeContext.WorkingThemeName = themeName;

            if (HttpContext.Request.IsAjaxRequest())
            {
                return Json(new { Success = true });
            }

            return RedirectToReferrer(returnUrl);
        }
    }
}
