using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Profiles;

namespace Smartstore.Web.Controllers
{
    // TODO: (mh) (core) This belongs to IdentityController IMHO. Discuss.
    public class ProfileController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly CustomerSettings _customerSettings;

        public ProfileController(SmartDbContext db, CustomerSettings customerSettings)
        {
            _db = db;
            _customerSettings = customerSettings;
        }

        [LocalizedRoute("/profile/{id:int}", Name = "CustomerProfile")]
        public async Task<IActionResult> Index(int id)
        {
            if (!_customerSettings.AllowViewingProfiles)
            {
                return NotFound();
            }

            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(id, false);

            // Guests do not have a customer profile.
            if (customer?.IsGuest() ?? true)
            {
                return NotFound();
            }

            var info = new ProfileInfoModel
            {
                Id = customer.Id,
                Avatar = customer.ToAvatarModel(null, true)
            };

            // Location.
            if (_customerSettings.ShowCustomersLocation)
            {
                var country = await _db.Countries.FindByIdAsync(customer.GenericAttributes.CountryId ?? 0, false);

                info.LocationEnabled = country != null;
                info.Location = country?.GetLocalized(x => x.Name);
            }

            // Registration date.
            if (_customerSettings.ShowCustomersJoinDate)
            {
                info.JoinDateEnabled = true;
                info.JoinDate = Services.DateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc).ToString("f");
            }

            // Birth date.
            if (_customerSettings.DateOfBirthEnabled && customer.BirthDate.HasValue)
            {
                info.DateOfBirthEnabled = true;
                info.DateOfBirth = customer.BirthDate.Value.ToString("D");
            }

            var model = new ProfileIndexModel
            {
                Id = customer.Id,
                CustomerName = customer.FormatUserName(_customerSettings, T, true),
                ProfileInfo = info
            };

            return View(model);
        }
    }
}
