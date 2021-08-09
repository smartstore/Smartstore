using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Common.Services;
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
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly CustomerSettings _customerSettings;

        public ProfileController(SmartDbContext db, IDateTimeHelper dateTimeHelper, CustomerSettings customerSettings)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
            _customerSettings = customerSettings;
        }

        // TODO: (mh) (core) What to do with this forum post paging param?
        [LocalizedRoute("/profile/{id:int}", Name = "CustomerProfile")]
        public async Task<IActionResult> Index(int id, int? page)
        {
            var customer = await _db.Customers.FindByIdAsync(id, false);
            if (!_customerSettings.AllowViewingProfiles || customer == null || customer.IsGuest())
            {
                return NotFound();
            }

            var name = customer.FormatUserName(_customerSettings, T, true);

            var model = new ProfileIndexModel
            {
                Id = customer.Id,
                ProfileTitle = T("Profile.ProfileOf", name),
                //PostsPage = page ?? 0,
                //PagingPosts = page.HasValue,
                //ForumsEnabled = _forumSettings.ForumsEnabled
            };

            // INFO: (mh) (core) Model preparation for old Info action starts here.
            // TODO: (mh) (core) Make prepare model method so we dont always must refer to model.ProfileInfo...
            model.ProfileInfo.Id = id;

            model.ProfileInfo.Avatar = customer.ToAvatarModel(null, true);

            // Location.
            if (_customerSettings.ShowCustomersLocation)
            {
                model.ProfileInfo.LocationEnabled = true;

                var country = await _db.Countries.FindByIdAsync(customer.GenericAttributes.CountryId ?? 0);
                    
                if (country != null)
                {
                    model.ProfileInfo.Location = country.GetLocalized(x => x.Name);
                }
                else
                {
                    model.ProfileInfo.LocationEnabled = false;
                }
            }

            // TODO: (mh) (core) Forum module must handle this somehow. Also ask if forum is enabled (because now PMs can be sent even if the Forum is turned off). 
            // Private message.
            //model.ProfileInfo.PMEnabled = _forumSettings.AllowPrivateMessages && !customer.IsGuest();

            // TODO: (mh) (core) Forum module must handle this somehow. 
            // Total forum posts.
            //if (_forumSettings.ForumsEnabled && _forumSettings.ShowCustomersPostCount)
            //{
            //    model.TotalPostsEnabled = true;
            //    model.TotalPosts = customer.GetAttribute<int>(SystemCustomerAttributeNames.ForumPostCount, _genericAttributeService);
            //}

            // Registration date.
            if (_customerSettings.ShowCustomersJoinDate)
            {
                model.ProfileInfo.JoinDateEnabled = true;
                model.ProfileInfo.JoinDate = _dateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc).ToString("f");
            }

            // Birth date.
            if (_customerSettings.DateOfBirthEnabled && customer.BirthDate.HasValue)
            {
                model.ProfileInfo.DateOfBirthEnabled = true;
                model.ProfileInfo.DateOfBirth = customer.BirthDate.Value.ToString("D");
            }

            return View(model);
        }
    }
}
