using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Smartstore.Admin.Models.Themes;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Web.Bundling;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;
using Smartstore.Web.Theming;

namespace Smartstore.Admin.Controllers
{
    public class ThemeController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IThemeRegistry _themeRegistry;
        private readonly IThemeVariableService _themeVarService;
        private readonly IThemeContext _themeContext;
        private readonly IBundleCache _bundleCache;
        private readonly IOptionsMonitor<BundlingOptions> _bundlingOptions;

        public ThemeController(
            SmartDbContext db,
            IThemeRegistry themeRegistry,
            IThemeVariableService themeVarService,
            IThemeContext themeContext,
            IBundleCache bundleCache,
            IOptionsMonitor<BundlingOptions> bundlingOptions)
        {
            _db = db;
            _themeRegistry = themeRegistry;
            _themeVarService = themeVarService;
            _themeContext = themeContext;
            _bundleCache = bundleCache;
            _bundlingOptions = bundlingOptions;
        }

        #region Themes

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Theme.Read)]
        public async Task<IActionResult> List(int? storeId)
        {
            var selectedStoreId = storeId ?? Services.StoreContext.CurrentStore.Id;
            var themeSettings = await Services.SettingFactory.LoadSettingsAsync<ThemeSettings>(selectedStoreId);
            var model = await MapperFactory.MapAsync<ThemeSettings, ThemeListModel>(themeSettings);

            var bundlingOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "{0} ({1})".FormatCurrent(T("Common.Auto"), T("Common.Recommended")) },
                new SelectListItem { Value = "1", Text = T("Common.No") },
                new SelectListItem { Value = "2", Text = T("Common.Yes") }
            };
            model.AvailableBundleOptimizationValues.AddRange(bundlingOptions);
            model.AvailableBundleOptimizationValues.FirstOrDefault(x => int.Parse(x.Value) == model.BundleOptimizationEnabled).Selected = true;

            var assetCachingOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = T("Common.Auto") },
                new SelectListItem { Value = "1", Text = T("Common.No") },
                new SelectListItem { Value = "2", Text = "{0} ({1})".FormatCurrent(T("Common.Yes"), T("Common.Recommended")) }
            };
            model.AvailableAssetCachingValues.AddRange(assetCachingOptions);
            model.AvailableAssetCachingValues.FirstOrDefault(x => int.Parse(x.Value) == model.AssetCachingEnabled).Selected = true;

            // Add theme configs.
            model.Themes.AddRange(GetThemes(themeSettings));

            model.StoreId = selectedStoreId;
            model.AvailableStores = Services.StoreContext.GetAllStores().ToSelectListItems();

            return View(model);
        }

        private IList<ThemeManifestModel> GetThemes(ThemeSettings themeSettings, bool includeHidden = true)
        {
            var themes = from m in _themeRegistry.GetThemeManifests(includeHidden)
                         select PrepareThemeManifestModel(m, themeSettings);

            var sortedThemes = themes.ToArray().SortTopological(StringComparer.OrdinalIgnoreCase).Cast<ThemeManifestModel>();

            return sortedThemes.OrderByDescending(x => x.IsActive).ToList();
        }

        protected virtual ThemeManifestModel PrepareThemeManifestModel(ThemeManifest manifest, ThemeSettings themeSettings)
        {
            var model = new ThemeManifestModel
            {
                Name = manifest.ThemeName,
                BaseTheme = manifest.BaseThemeName,
                Title = manifest.ThemeTitle,
                Description = manifest.Description,
                Author = manifest.Author,
                Url = manifest.Url,
                Version = manifest.Version,
                PreviewImageUrl = $"~/themes/{manifest.ThemeName}/{manifest.PreviewImagePath.Trim('/')}",
                IsActive = themeSettings.DefaultTheme == manifest.ThemeName,
                State = manifest.State,
            };

            //model.IsConfigurable = HostingEnvironment.VirtualPathProvider.FileExists("{0}{1}/Views/Shared/ConfigureTheme.cshtml".FormatInvariant(manifest.Location.EnsureEndsWith("/"), manifest.ThemeName));
            model.IsConfigurable = true;

            return model;
        }

        [Permission(Permissions.Configuration.Theme.Read)]
        public IActionResult Configure(string theme, int storeId)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction("List", new { storeId });
            }

            var model = new ConfigureThemeModel
            {
                ThemeName = theme,
                StoreId = storeId,
                AvailableStores = Services.StoreContext.GetAllStores().ToSelectListItems()
            };

            ViewData["ConfigureThemeUrl"] = Url.Action("Configure", new { theme });
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> Configure(string theme, int storeId, IDictionary<string, string[]> values, bool continueEditing)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction("List", new { storeId });
            }

            try
            {
                var variables = FixThemeVarValues(values);

                if (variables.Count > 0)
                {
                    await _themeVarService.SaveThemeVariablesAsync(theme, storeId, variables);
                    Services.ActivityLogger.LogActivity("EditThemeVars", T("ActivityLog.EditThemeVars"), theme);
                }

                NotifySuccess(T("Admin.Configuration.Themes.Notifications.ConfigureSuccess"));

                return continueEditing
                    ? RedirectToAction("Configure", new { theme, storeId })
                    : RedirectToAction("List", new { storeId });
            }
            catch (ThemeValidationException ex)
            {
                TempData["SassParsingError"] = ex.Message.Trim().TrimStart('\r', '\n', '/', '*').TrimEnd('*', '/', '\r', '\n');
                TempData["OverriddenThemeVars"] = ex.AttemptedVars;
                NotifyError(T("Admin.Configuration.Themes.Notifications.ConfigureError"));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            // Fail.
            return RedirectToAction("Configure", new { theme, storeId });
        }

        private static IDictionary<string, object> FixThemeVarValues(IDictionary<string, string[]> values)
        {
            var fixedDict = new Dictionary<string, object>();

            foreach (var kvp in values)
            {
                var value = kvp.Value;
                var strValue = (string)null;

                if (value.Length == 1)
                {
                    strValue = value[0];
                }
                else if (value.Length == 2)
                {
                    // Is a boolean
                    strValue = value[0].ToBool().ToString().ToLowerInvariant();
                }

                fixedDict[kvp.Key] = strValue;
            }

            return fixedDict;
        }

        [HttpPost, FormValueRequired("reset-vars"), ActionName("Configure")]
        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> Reset(string theme, int storeId)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction("List", new { storeId });
            }

            await _themeVarService.DeleteThemeVariablesAsync(theme, storeId);

            Services.ActivityLogger.LogActivity("ResetThemeVars", T("ActivityLog.ResetThemeVars"), theme);
            NotifySuccess(T("Admin.Configuration.Themes.Notifications.ResetSuccess"));

            return RedirectToAction("Configure", new { theme, storeId });
        }

        #endregion

        #region Preview

        #endregion
    }
}
