using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Smartstore.Admin.Models.Themes;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.IO;
using Smartstore.Web.Bundling;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class ThemeController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IThemeRegistry _themeRegistry;
        private readonly IThemeVariableService _themeVarService;
        private readonly IThemeContext _themeContext;
        private readonly IBundleCache _bundleCache;
        private readonly Lazy<IIconExplorer> _iconExplorer;
        private readonly IOptionsMonitorCache<BundlingOptions> _bundlingOptionsCache;

        public ThemeController(
            SmartDbContext db,
            IThemeRegistry themeRegistry,
            IThemeVariableService themeVarService,
            IThemeContext themeContext,
            IBundleCache bundleCache,
            Lazy<IIconExplorer> iconExplorer,
            IOptionsMonitorCache<BundlingOptions> bundlingOptionsCache)
        {
            _db = db;
            _themeRegistry = themeRegistry;
            _themeVarService = themeVarService;
            _themeContext = themeContext;
            _bundleCache = bundleCache;
            _bundlingOptionsCache = bundlingOptionsCache;
            _iconExplorer = iconExplorer;
        }

        #region Themes

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
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

        private IList<ThemeDescriptorModel> GetThemes(ThemeSettings themeSettings, bool includeHidden = true)
        {
            var themes = from m in _themeRegistry.GetThemeDescriptors(includeHidden)
                         select PrepareThemeDescriptorModel(m, themeSettings);

            var sortedThemes = themes.ToArray().SortTopological(StringComparer.OrdinalIgnoreCase).Cast<ThemeDescriptorModel>();

            return sortedThemes.OrderByDescending(x => x.IsActive).ToList();
        }

        protected virtual ThemeDescriptorModel PrepareThemeDescriptorModel(ThemeDescriptor descriptor, ThemeSettings themeSettings)
        {
            var model = new ThemeDescriptorModel
            {
                Name = descriptor.Name,
                BaseTheme = descriptor.BaseThemeName,
                FriendlyName = descriptor.FriendlyName,
                Description = descriptor.Description,
                Author = descriptor.Author,
                ProjectUrl = descriptor.ProjectUrl,
                Version = descriptor.Version.ToString(),
                PreviewImageUrl = $"~/themes/{descriptor.Name}/{descriptor.PreviewImagePath.Trim('/')}",
                IsActive = themeSettings.DefaultTheme == descriptor.Name,
                State = descriptor.State,
            };

            //model.IsConfigurable = HostingEnvironment.VirtualPathProvider.FileExists("{0}{1}/Views/Shared/ConfigureTheme.cshtml".FormatInvariant(manifest.Location.EnsureEndsWith("/"), manifest.ThemeName));
            model.IsConfigurable = true;

            return model;
        }

        [HttpPost, ActionName("List")]
        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> ListPost(ThemeListModel model, IFormCollection form)
        {
            var themeSettings = await Services.SettingFactory.LoadSettingsAsync<ThemeSettings>(model.StoreId);

            var themeSwitched = !themeSettings.DefaultTheme.EqualsNoCase(model.DefaultTheme);
            if (themeSwitched)
            {
                await Services.EventPublisher.PublishAsync(new ThemeSwitchedEvent
                {
                    OldTheme = themeSettings.DefaultTheme,
                    NewTheme = model.DefaultTheme
                });
            }

            if (model.BundleOptimizationEnabled != themeSettings.BundleOptimizationEnabled || model.AssetCachingEnabled != themeSettings.AssetCachingEnabled)
            {
                // Invalidate global bundling options if any bundle affecting property has changed.
                _bundlingOptionsCache.TryRemove(Options.DefaultName);
            }

            await MapperFactory.MapAsync(model, themeSettings);
            await Services.SettingFactory.SaveSettingsAsync(themeSettings, model.StoreId);
            await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, themeSettings, form));

            NotifySuccess(T("Admin.Configuration.Updated"));

            return RedirectToAction(nameof(List), new { storeId = model.StoreId });
        }

        [Permission(Permissions.Configuration.Theme.Read)]
        public IActionResult Configure(string theme, int storeId)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction(nameof(List), new { storeId });
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
                return RedirectToAction(nameof(List), new { storeId });
            }

            try
            {
                var variables = FixThemeVarValues(values);

                if (variables.Count > 0)
                {
                    await _themeVarService.SaveThemeVariablesAsync(theme, storeId, variables);
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditThemeVars, T("ActivityLog.EditThemeVars"), theme);
                }

                NotifySuccess(T("Admin.Configuration.Themes.Notifications.ConfigureSuccess"));

                return continueEditing
                    ? RedirectToAction("Configure", new { theme, storeId })
                    : RedirectToAction(nameof(List), new { storeId });
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

        [Permission(Permissions.Configuration.Theme.Update)]
        public IActionResult ReloadThemes(int? storeId)
        {
            _themeRegistry.ReloadThemes();
            return RedirectToAction(nameof(List), new { storeId });
        }

        [HttpPost, FormValueRequired("reset-vars"), ActionName("Configure")]
        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> Reset(string theme, int storeId)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction(nameof(List), new { storeId });
            }

            await _themeVarService.DeleteThemeVariablesAsync(theme, storeId);

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.ResetThemeVars, T("ActivityLog.ResetThemeVars"), theme);
            NotifySuccess(T("Admin.Configuration.Themes.Notifications.ResetSuccess"));

            return RedirectToAction("Configure", new { theme, storeId });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> ImportVariables(string theme, int storeId, IFormCollection form)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction(nameof(List), new { storeId });
            }

            try
            {
                var file = form.Files["importxmlfile"];
                if (file != null && file.Length > 0)
                {
                    int importedCount = 0;
                    using var stream = file.OpenReadStream();
                    importedCount = await _themeVarService.ImportVariablesAsync(theme, storeId, await stream.AsStringAsync());

                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.ImportThemeVars, T("ActivityLog.ResetThemeVars"), importedCount, theme);
                    NotifySuccess(T("Admin.Configuration.Themes.Notifications.ImportSuccess", importedCount));
                }
                else
                {
                    NotifyError(T("Admin.Configuration.Themes.Notifications.UploadFile"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Configure", new { theme, storeId });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Theme.Read)]
        public async Task<IActionResult> ExportVariables(string theme, int storeId, IFormCollection form)
        {
            if (!_themeRegistry.ContainsTheme(theme))
            {
                return RedirectToAction(nameof(List), new { storeId });
            }

            try
            {
                var xml = await _themeVarService.ExportVariablesAsync(theme, storeId);

                if (xml.IsEmpty())
                {
                    NotifyInfo(T("Admin.Configuration.Themes.Notifications.NoExportInfo"));
                }
                else
                {
                    string profileName = form["exportprofilename"];
                    string fileName = "themevars-{0}{1}-{2}.xml".FormatCurrent(theme,
                        profileName.HasValue() ? '-' + PathUtility.SanitizeFileName(profileName) : string.Empty, DateTime.Now.ToString("yyyyMMdd"));

                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.ExportThemeVars, T("ActivityLog.ExportThemeVars"), theme);

                    return new XmlDownloadResult(xml, fileName);
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Configure", new { theme, storeId });
        }

        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> ClearAssetCache()
        {
            try
            {
                await _bundleCache.ClearAsync();
                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToReferrer();
        }

        #endregion

        #region Preview

        /// <summary>
        /// Initializes the preview mode.
        /// </summary>
        [Permission(Permissions.Configuration.Theme.Read)]
        public async Task<IActionResult> Preview(string theme, int? storeId, string returnUrl)
        {
            if (!storeId.HasValue)
            {
                storeId = Services.StoreContext.CurrentStore.Id;
            }

            if (theme.IsEmpty())
            {
                theme = (await Services.SettingFactory.LoadSettingsAsync<ThemeSettings>(storeId.Value)).DefaultTheme;
            }

            if (!_themeRegistry.ContainsTheme(theme))
            {
                return NotFound();
            }

            _themeContext.SetPreviewTheme(theme);
            Services.StoreContext.SetPreviewStore(storeId);

            if (returnUrl.IsEmpty())
            {
                returnUrl = Services.WebHelper.GetUrlReferrer()?.PathAndQuery;
            }

            TempData["PreviewModeReturnUrl"] = returnUrl;

            return RedirectToAction("Index", "Home", new { area = (string)null });
        }

        /// <summary>
        /// Refreshes the preview mode (after a select change).
        /// </summary>
        [HttpPost, ActionName("PreviewTool")]
        [FormValueRequired(FormValueRequirementMatch.MatchAll, "theme", "storeId")]
        [FormValueAbsent(FormValueRequirementOperator.StartsWith, "PreviewMode.")]
        [Permission(Permissions.Configuration.Theme.Update)]
        public IActionResult PreviewToolPost(string theme, int storeId, string returnUrl)
        {
            _themeContext.SetPreviewTheme(theme);
            Services.StoreContext.SetPreviewStore(storeId);

            return RedirectToReferrer(returnUrl);
        }

        /// <summary>
        /// Exits the preview mode.
        /// </summary>
        [HttpPost, ActionName("PreviewTool"), FormValueRequired("PreviewMode.Exit")]
        [Permission(Permissions.Configuration.Theme.Read)]
        public IActionResult ExitPreview()
        {
            _themeContext.SetPreviewTheme(null);
            Services.StoreContext.SetPreviewStore(null);

            var returnUrl = (string)TempData["PreviewModeReturnUrl"];
            return RedirectToReferrer(returnUrl);
        }

        /// <summary>
        /// // Applies the current previewed theme and exits the preview mode.
        /// </summary>
        [HttpPost, ActionName("PreviewTool"), FormValueRequired("PreviewMode.Apply")]
        [Permission(Permissions.Configuration.Theme.Update)]
        public async Task<IActionResult> ApplyPreviewTheme(string theme, int storeId)
        {
            var themeSettings = await Services.SettingFactory.LoadSettingsAsync<ThemeSettings>(storeId);
            var oldTheme = themeSettings.DefaultTheme;
            themeSettings.DefaultTheme = theme;
            var themeSwitched = !oldTheme.EqualsNoCase(theme);

            if (themeSwitched)
            {
                await Services.EventPublisher.PublishAsync(new ThemeSwitchedEvent
                {
                    OldTheme = oldTheme,
                    NewTheme = theme
                });
            }

            await Services.SettingFactory.SaveSettingsAsync(themeSettings, storeId);

            NotifySuccess(T("Admin.Configuration.Updated"));

            return ExitPreview();
        }

        #endregion

        #region UI Helpers

        [HttpPost]
        public JsonResult SearchIcons(string term, string selected = null, int page = 1)
        {
            const int pageSize = 250;

            var iconExplorer = _iconExplorer.Value;
            var icons = iconExplorer.All.AsEnumerable();

            if (term.HasValue())
            {
                icons = iconExplorer.FindIcons(term, true);
            }

            var result = icons.ToPagedList(page - 1, pageSize);

            if (selected.HasValue() && term.IsEmpty())
            {
                var selIcon = iconExplorer.GetIconByName(selected);
                if (!selIcon.IsPro && !result.Contains(selIcon))
                {
                    result.Insert(0, selIcon);
                }
            }

            return Json(new
            {
                results = result.AsEnumerable().Select(x => new
                {
                    id = x.Name,
                    text = x.Name,
                    hasRegularStyle = x.HasRegularStyle,
                    isBrandIcon = x.IsBrandIcon,
                    isPro = x.IsPro,
                    label = x.Label,
                    styles = x.Styles
                }),
                pagination = new { more = result.HasNextPage }
            });
        }

        #endregion
    }
}
