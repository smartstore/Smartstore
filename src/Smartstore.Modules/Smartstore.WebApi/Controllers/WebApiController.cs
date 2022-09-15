using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Http;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api.Controllers
{
    public class WebApiController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IWebApiService _apiService;
        private readonly CustomerSettings _customerSettings;

        public WebApiController(
            SmartDbContext db, 
            IWebApiService apiService,
            CustomerSettings customerSettings)
        {
            _db = db;
            _apiService = apiService;
            _customerSettings = customerSettings;
        }

        [Permission(WebApiPermissions.Read)]
        [LoadSetting]
        public IActionResult Configure(WebApiSettings settings)
        {
            var model = MiniMapper.Map<WebApiSettings, ConfigurationModel>(settings);

            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;

            // TODO: (mg) (core) check URLs. Probably changes.
            model.ApiOdataUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/odata/v1"), Request, true).EnsureEndsWith("/");
            model.ApiOdataMetadataUrl = model.ApiOdataUrl + "$metadata";
            model.SwaggerUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/swagger/ui/index"), Request, true);

            return View(model);
        }

        [Permission(WebApiPermissions.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Configure(ConfigurationModel model, WebApiSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            MiniMapper.Map(model, settings);

            await Services.Cache.RemoveByPatternAsync(WebApiService.StatePatternKey);
            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [Permission(WebApiPermissions.Read)]
        public async Task<IActionResult> UserList(GridCommand command, ConfigurationModel model)
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

            query = query.ApplyIdentFilter(model.SearchEmail, model.SearchUsername, null);

            if (model.SearchTerm.HasValue())
            {
                query = query.ApplySearchTermFilter(model.SearchTerm);
            }

            if (model.SearchActiveOnly.HasValue)
            {
                query = query.Where(x => x.Active == model.SearchActiveOnly.Value);
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
                        user.SecretKey = cachedUser.SecretKey;
                        user.Enabled = cachedUser.Enabled;
                        user.EnabledString = cachedUser.Enabled ? yes : no;

                        if (cachedUser.LastRequest.HasValue)
                        {
                            user.LastRequestDate = Services.DateTimeHelper.ConvertToUserTime(cachedUser.LastRequest.Value, DateTimeKind.Utc);
                            user.LastRequestDateString = user.LastRequestDate.Humanize(false);
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
        }

        [HttpPost]
        [Permission(WebApiPermissions.Create)]
        public async Task<IActionResult> CreateUserKeys(int customerId)
        {
            if (customerId == 0)
            {
                return BadRequest();
            }

            var cachedUsers = await _apiService.GetApiUsersAsync();

            for (var i = 0; i < 9999; i++)
            {
                if (WebApiService.CreateKeys(out var key1, out var key2) && !cachedUsers.ContainsKey(key1))
                {
                    await RemoveKeys(customerId);

                    var apiUser = new WebApiUser
                    {
                        CustomerId = customerId,
                        PublicKey = key1,
                        SecretKey = key2,
                        Enabled = true
                    };

                    _db.GenericAttributes.Add(new GenericAttribute
                    {
                        EntityId = customerId,
                        KeyGroup = nameof(Customer),
                        Key = WebApiService.AttributeUserDataKey,
                        Value = apiUser.ToString()
                    });

                    await _db.SaveChangesAsync();
                    await Services.Cache.RemoveAsync(WebApiService.UsersKey);

                    break;
                }
            }

            return Ok();
        }

        [HttpPost]
        [Permission(WebApiPermissions.Delete)]
        public async Task<IActionResult> DeleteUserKeys(int customerId)
        {
            await RemoveKeys(customerId);
            await Services.Cache.RemoveAsync(WebApiService.UsersKey);

            return Ok();
        }

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
                    .BatchDeleteAsync();
            }

            return 0;
        }
    }
}
