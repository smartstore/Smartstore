using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Smartstore.Caching;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Utilities;

namespace Smartstore.Core.Packaging
{
    public class CheckUpdateResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LanguageCode { get; set; }
        public string Version { get; set; }
        public string FullName { get; set; }
        public string ReleaseNotes { get; set; }
        public string InfoUrl { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime ReleaseDateUtc { get; set; }
        public bool IsStable { get; set; }
        public bool AutoUpdatePossible { get; set; }
        public string AutoUpdatePackageUrl { get; set; }
    }

    /// <summary>
    /// Checks for application updates.
    /// </summary>
    public class UpdateChecker
    {
        const string CacheKeyPrefix = "maintenance:checkupdateresult";

        private readonly IApplicationContext _appContext;
        private readonly ICacheManager _cache;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IPermissionService _permissionService;
        private readonly CommonSettings _commonSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public UpdateChecker(
            IApplicationContext appContext,
            ICacheManager cache,
            IWorkContext workContext,
            IStoreContext storeContext,
            IPermissionService permissionService,
            CommonSettings commonSettings,
            IHttpClientFactory httpClientFactory)
        {
            _appContext = appContext;
            _cache = cache;
            _workContext = workContext;
            _storeContext = storeContext;
            _permissionService = permissionService;
            _commonSettings = commonSettings;
            _httpClientFactory = httpClientFactory;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<CheckUpdateResult> CheckUpdateAsync(bool enforce = false)
        {
            var curVersion = SmartstoreVersion.CurrentFullVersion;
            var lang = _workContext.WorkingLanguage.UniqueSeoCode;
            var cacheKey = "{0}-{1}".FormatInvariant(CacheKeyPrefix, lang);

            if (enforce)
            {
                await _cache.RemoveByPatternAsync(CacheKeyPrefix + "*");
            }

            var result = await _cache.GetAsync(cacheKey, async (ctx) =>
            {
                ctx.ExpiresIn(TimeSpan.FromHours(12));

                var noUpdateResult = new CheckUpdateResult { UpdateAvailable = false, LanguageCode = lang, CurrentVersion = curVersion };

                try
                {
                    string url = $"https://dlm.smartstore.com/api/v1/apprelease/CheckUpdate?app=Smartstore&version={curVersion}&language={lang}";
                    var client = _httpClientFactory.CreateClient();

                    client.Timeout = TimeSpan.FromSeconds(3);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("Authorization-Key", _storeContext.CurrentStore.Url.TrimEnd('/'));
                    client.DefaultRequestHeaders.Add("X-Application-ID", _appContext.RuntimeInfo.ApplicationIdentifier);
                    client.DefaultRequestHeaders.Add("X-Environment-ID", _appContext.RuntimeInfo.EnvironmentIdentifier);

                    var response = await client.GetAsync(url);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return noUpdateResult;
                    }

                    var model = await response.Content.ReadFromJsonAsync<CheckUpdateResult>();

                    model.UpdateAvailable = true;
                    model.CurrentVersion = curVersion;
                    model.LanguageCode = lang;

                    if (CommonHelper.IsDevEnvironment || !_commonSettings.AutoUpdateEnabled || !_permissionService.Authorize(Permissions.System.Maintenance.Execute))
                    {
                        model.AutoUpdatePossible = false;
                    }

                    // Don't show message if user decided to suppress it
                    var suppressKey = $"SuppressUpdateMessage.{curVersion}.{model.Version}";
                    if (enforce)
                    {
                        // but ignore user's decision if 'enforce'
                        _workContext.CurrentCustomer.GenericAttributes.Set<bool?>(suppressKey, null);
                        await _workContext.CurrentCustomer.GenericAttributes.SaveChangesAsync();
                    }

                    var showMessage = enforce || _workContext.CurrentCustomer.GenericAttributes.Get<bool?>(suppressKey).GetValueOrDefault() == false;
                    if (!showMessage)
                    {
                        return noUpdateResult;
                    }

                    return model;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An error occurred while checking for application update.");
                    return noUpdateResult;
                }
            });

            return result;
        }

        public async Task SuppressMessageAsync(string myVersion, string newVersion)
        {
            var suppressKey = $"SuppressUpdateMessage.{myVersion}.{newVersion}";
            _workContext.CurrentCustomer.GenericAttributes.Set(suppressKey, true);
            await _workContext.CurrentCustomer.GenericAttributes.SaveChangesAsync();
            await _cache.RemoveByPatternAsync(CacheKeyPrefix + "*");
        }
    }
}
