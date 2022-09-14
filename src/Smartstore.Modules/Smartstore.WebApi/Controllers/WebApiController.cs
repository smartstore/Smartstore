using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Http;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Controllers
{
    public class WebApiController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IWebApiService _apiService;

        public WebApiController(SmartDbContext db, IWebApiService apiService)
        {
            _db = db;
            _apiService = apiService;
        }

        [Permission(WebApiPermissions.Read)]
        [LoadSetting]
        public IActionResult Configure(WebApiSettings settings)
        {
            var model = MiniMapper.Map<WebApiSettings, ConfigurationModel>(settings);

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
        public async Task<IActionResult> UserList(GridCommand command)
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

            var apiUsers = await query
                .ApplyGridCommand(command)
                .Select(x => new WebApiUserModel
                {
                    Id = x.Id,
                    Username = x.Username,
                    Email = x.Email,
                    AdminComment = x.AdminComment
                })
                .ToPagedList(command)
                .LoadAsync();

            foreach (var user in apiUsers)
            {
                if (usersDic.TryGetValue(user.Id, out var cachedUser))
                {
                    user.PublicKey = cachedUser.PublicKey;
                    user.SecretKey = cachedUser.SecretKey;
                    user.Enabled = cachedUser.Enabled;
                    user.EnabledFriendly = cachedUser.Enabled ? yes : no;

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
            }

            return Json(new GridModel<WebApiUserModel>
            {
                Rows = apiUsers,
                Total = await apiUsers.GetTotalCountAsync()
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
