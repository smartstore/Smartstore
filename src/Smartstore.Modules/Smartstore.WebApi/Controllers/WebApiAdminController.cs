using Humanizer;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Http;
using Smartstore.Web.Api.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Web.Api.Controllers
{
    public class WebApiAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IWebApiService _apiService;
        private readonly Lazy<IConfigureOptions<ODataOptions>> _odataOptionsConfigurer;
        private readonly IOptions<ODataOptions> _odataOptions;
        private readonly MultiStoreSettingHelper _settingHelper;
        private readonly CustomerSettings _customerSettings;

        public WebApiAdminController(
            SmartDbContext db, 
            IWebApiService apiService,
            Lazy<IConfigureOptions<ODataOptions>> odataOptionsConfigurer,
            IOptions<ODataOptions> odataOptions,
            MultiStoreSettingHelper settingHelper,
            CustomerSettings customerSettings)
        {
            _db = db;
            _apiService = apiService;
            _odataOptionsConfigurer = odataOptionsConfigurer;
            _odataOptions = odataOptions;
            _settingHelper = settingHelper;
            _customerSettings = customerSettings;
        }

        [Permission(WebApiPermissions.Read)]
        [LoadSetting]
        public async Task<IActionResult> Configure(WebApiSettings settings)
        {
            var model = MiniMapper.Map<WebApiSettings, ConfigurationModel>(settings);

            model.ApiOdataUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/odata/v1"), Request, true).EnsureEndsWith('/');
            model.ApiOdataMetadataUrl = model.ApiOdataUrl + "$metadata";
            model.ApiDocsUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/" + WebApiSettings.SwaggerRoutePrefix), Request, true);

            if (Services.ApplicationContext.HostEnvironment.IsDevelopment())
            {
                model.ApiOdataEndpointsUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/$odata"), Request, true);
            }

            if (!ModelState.IsValid)
            {
                // We need to detect override checkboxes if "Configure" was called with invalid model state.
                await _settingHelper.DetectOverrideKeysAsync(settings, model);
            }

            ViewBag.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;

            return View(model);
        }

        [Permission(WebApiPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model, IFormCollection form)
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<WebApiSettings>(storeScope);

            if (!ModelState.IsValid)
            {
                return await Configure(settings);
            }

            var tmpSettings = storeScope == 0 ? settings : await Services.SettingFactory.LoadSettingsAsync<WebApiSettings>(0);

            var reconfigureODataOptions = model.MaxBatchNestingDepth != tmpSettings.MaxBatchNestingDepth
                || model.MaxBatchOperationsPerChangeset != tmpSettings.MaxBatchOperationsPerChangeset
                || model.MaxBatchReceivedMessageSize != tmpSettings.MaxBatchReceivedMessageSize;

            settings = ((ISettings)settings).Clone() as WebApiSettings;
            MiniMapper.Map(model, settings);

            _settingHelper.Contextualize(storeScope);
            await _settingHelper.UpdateSettingsAsync(settings, form);
            await Services.Settings.ApplySettingAsync(settings, x => x.MaxBatchNestingDepth);
            await Services.Settings.ApplySettingAsync(settings, x => x.MaxBatchOperationsPerChangeset);
            await Services.Settings.ApplySettingAsync(settings, x => x.MaxBatchReceivedMessageSize);

            await _db.SaveChangesAsync();
            await Services.Cache.RemoveByPatternAsync(WebApiService.StatePatternKey);

            if (reconfigureODataOptions)
            {
                _odataOptionsConfigurer.Value.Configure(_odataOptions.Value);
            }

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [Permission(WebApiPermissions.Read)]
        public async Task<IActionResult> UserList(GridCommand command, CustomerSearchModel model)
        {
            var registeredRoleId = await _db.CustomerRoles
                .Where(x => x.SystemName == SystemCustomerRoleNames.Registered)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var yes = T("Admin.Common.Yes");
            var no = T("Admin.Common.No");
            var cachedUsers = await _apiService.GetApiUsersAsync();
            var usersDic = cachedUsers.Values.ToDictionarySafe(x => x.CustomerId, x => x);

            var query =
                from c in _db.Customers
                join a in
                (
                    from a in _db.GenericAttributes
                    where a.KeyGroup == nameof(Customer) && a.Key == WebApiService.AttributeUserDataKey
                    select a
                )
                on c.Id equals a.EntityId into ga
                from a in ga.DefaultIfEmpty()
                where c.CustomerRoleMappings.Select(rm => rm.CustomerRoleId).Contains(registeredRoleId)
                orderby a.Value descending
                select c;

            query = query.ApplyIdentFilter(model.SearchEmail, model.SearchUsername, model.SearchCustomerNumber);

            if (model.SearchCustomerNumber.HasValue())
            {
                query = query.Where(x => x.CustomerNumber.Contains(model.SearchCustomerNumber));
            }
            if (model.SearchTerm.HasValue())
            {
                query = query.ApplySearchTermFilter(model.SearchTerm);
            }
            if (model.SearchActiveOnly != null)
            {
                query = query.Where(x => x.Active == model.SearchActiveOnly);
            }

            var customers = await query
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = customers
                .Select(x =>
                {
                    var user = new WebApiUserModel
                    {
                        Id = x.Id,
                        Active = x.Active,
                        Username = x.Username,
                        FullName = x.GetFullName(),
                        Email = x.Email,
                        AdminComment = x.AdminComment,
                        EditUrl = Url.Action("Edit", "Customer", new { id = x.Id, area = "Admin" })
                    };

                    if (usersDic.TryGetValue(user.Id, out var cachedUser))
                    {
                        user.PublicKey = cachedUser.PublicKey;
                        user.SecretKey = Mask(cachedUser.SecretKey);
                        user.Enabled = cachedUser.Enabled;
                        user.EnabledString = cachedUser.Enabled ? yes : no;

                        if (cachedUser.LastRequest.HasValue)
                        {
                            user.LastRequestDate = Services.DateTimeHelper.ConvertToUserTime(cachedUser.LastRequest.Value, DateTimeKind.Utc);
                            user.LastRequestDateString = user.LastRequestDate.ToHumanizedString(false);
                        }
                        else
                        {
                            user.LastRequestDateString = "-";
                        }
                    }

                    return user;
                })
                .ToList();

            return Json(new GridModel<WebApiUserModel>
            {
                Rows = rows,
                Total = await customers.GetTotalCountAsync()
            });

            static string Mask(string key)
            {
                var len = key?.Length ?? 0;
                if (len == 0)
                {
                    return key;
                }

                return key[..4] + new string('*', len - 8) + key.Substring(len - 4, 4);
            }
        }

        // AJAX.
        [HttpPost]
        [Permission(WebApiPermissions.Create)]
        public async Task<IActionResult> UserKeys(int customerId, bool create)
        {
            ViewBag.BodyOnly = true;

            WebApiUser apiUser = null;

            if (customerId != 0)
            {
                var cachedUsers = await _apiService.GetApiUsersAsync();

                if (create)
                {
                    for (var i = 0; i < 9999; i++)
                    {
                        if (WebApiService.CreateKeys(out var publicKey, out var secretKey) && !cachedUsers.ContainsKey(publicKey))
                        {
                            await RemoveKeys(customerId);

                            apiUser = new WebApiUser
                            {
                                CustomerId = customerId,
                                PublicKey = publicKey,
                                SecretKey = secretKey,
                                Enabled = true
                            };

                            _db.GenericAttributes.Add(new()
                            {
                                EntityId = customerId,
                                KeyGroup = nameof(Customer),
                                Key = WebApiService.AttributeUserDataKey,
                                Value = apiUser.ToString()
                            });

                            await _db.SaveChangesAsync();
                            _apiService.ClearApiUserCache();

                            break;
                        }
                    }
                }
                else
                {
                    apiUser = cachedUsers.Values.FirstOrDefault(x => x.CustomerId == customerId);
                }
            }

            if (apiUser == null)
            {
                return new EmptyResult();
            }

            var customer = await _db.Customers.FindByIdAsync(customerId, false);
            var customerName = customer?.FullName ?? customer?.Email ?? StringExtensions.NotAvailable;

            var model = new ApiKeysModel
            {
                CustomerName = customerName,
                PublicKey = apiUser.PublicKey,
                SecretKey = apiUser.SecretKey,
                Enabled = apiUser.Enabled
            };

            return PartialView("_ApiKeysPopup", model);
        }

        // AJAX.
        [HttpPost]
        [Permission(WebApiPermissions.Delete)]
        public async Task<IActionResult> DeleteUserKeys(int customerId)
        {
            await RemoveKeys(customerId);
            _apiService.ClearApiUserCache();

            return Ok();
        }

        // AJAX.
        [HttpPost]
        [Permission(WebApiPermissions.Update)]
        public async Task<IActionResult> EnableUser(int customerId, bool enable)
        {
            if (customerId == 0)
            {
                return BadRequest();
            }

            var cachedUsers = await _apiService.GetApiUsersAsync();
            var apiUser = cachedUsers.Values.FirstOrDefault(x => x.CustomerId == customerId);

            if (apiUser != null)
            {
                apiUser.Enabled = enable;

                var attribute = await _db.GenericAttributes.FindByIdAsync(apiUser.GenericAttributeId);
                if (attribute != null)
                {
                    attribute.Value = apiUser.ToString();
                    await _db.SaveChangesAsync();
                }
            }

            return Ok();
        }

        private async Task<int> RemoveKeys(int customerId)
        {
            if (customerId != 0)
            {
                return await _db.GenericAttributes
                    .Where(x => x.EntityId == customerId && x.KeyGroup == nameof(Customer) && x.Key == WebApiService.AttributeUserDataKey)
                    .ExecuteDeleteAsync();
            }

            return 0;
        }
    }
}
