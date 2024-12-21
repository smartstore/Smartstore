using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules.Filters;

namespace Smartstore.Admin.Controllers
{
    public partial class SettingController : AdminController
    {
        private static bool ShouldUpdateIdentityOptions(CustomerUserSettingsModel.CustomerSettingsModel model, CustomerSettings settings)
        {
            if (model.PasswordMinLength != settings.PasswordMinLength
                || model.PasswordRequireDigit != settings.PasswordRequireDigit
                || model.PasswordRequireUppercase != settings.PasswordRequireUppercase
                || model.PasswordRequiredUniqueChars != settings.PasswordRequiredUniqueChars
                || model.PasswordRequireLowercase != settings.PasswordRequireLowercase
                || model.PasswordRequireNonAlphanumeric != settings.PasswordRequireNonAlphanumeric
                || model.CustomerNameAllowedCharacters != settings.CustomerNameAllowedCharacters)
            {
                return true;
            }

            return false;
        }

        private RedirectToActionResult NotifyAndRedirect(string actionMethod)
        {
            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(actionMethod);
        }

        private SelectListItem ResToSelectListItem(string resourceKey)
        {
            var value = T(resourceKey).Value.EmptyNull();
            return new SelectListItem { Text = value, Value = value };
        }

        private async Task<bool> CheckToDeleteAddress(int addressId, string settingName)
        {
            if (addressId != 0 &&
                !await _db.Settings.AnyAsync(x => x.Value == addressId.ToStringInvariant() && x.Name == settingName))
            {
                // Address can be removed because it is not in use anymore.
                _db.Addresses.Remove(addressId);
                await _db.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private List<SelectListItem> CreateProductSortingsList(ProductSortingEnum selectedSorting)
        {
            var language = Services.WorkContext.WorkingLanguage;
            var sortings = (ProductSortingEnum[])Enum.GetValues(typeof(ProductSortingEnum));

            return sortings
                .Where(x => x != ProductSortingEnum.CreatedOnAsc)
                .Select(x => new SelectListItem
                {
                    Text = Services.Localization.GetLocalizedEnum(x, language.Id),
                    Value = ((int)x).ToString(),
                    Selected = x == selectedSorting
                })
                .ToList();
        }
    }
}
