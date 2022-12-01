#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Rendering
{
    public static class SelectListExtensions
    {
        public static SelectList ToSelectList<TEnum>(this TEnum enumObj, bool markCurrentAsSelected = true)
            where TEnum : struct
        {
            Guard.IsEnumType(typeof(TEnum));

            var languageId = EngineContext.Current.Scope.Resolve<IWorkContext>().WorkingLanguage.Id;

            var values = from TEnum enumValue in Enum.GetValues(typeof(TEnum))
                         select new 
                         { 
                             ID = Convert.ToInt32(enumValue), 
                             Name = enumValue.GetLocalizedEnum(languageId) 
                         };

            object? selectedValue = null;
            if (markCurrentAsSelected)
            {
                selectedValue = Convert.ToInt32(enumObj);
            }

            return new SelectList(values, "ID", "Name", selectedValue!);
        }

        /// <summary>
        /// Get a select list of all stores
        /// </summary>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<Store> stores, params int[] selectedStoreIds)
        {
            Guard.NotNull(stores);

            return stores.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToStringInvariant(),
                Selected = selectedStoreIds != null && selectedStoreIds.Contains(x.Id)
            })
            .ToList();
        }

        /// <summary>
        /// Get a select list of all customer roles
        /// </summary>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<CustomerRole> roles, params int[] selectedCustomerRoleIds)
        {
            Guard.NotNull(roles);

            return roles.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToStringInvariant(),
                Selected = selectedCustomerRoleIds != null && selectedCustomerRoleIds.Contains(x.Id)
            })
            .ToList();
        }

        /// <summary>
        /// Get a select list of all payment methods.
        /// </summary>
        /// <param name="paymentProviders">Payment providers.</param>
        /// <param name="pluginMediator">Plugin mediator.</param>
        /// <param name="selectedMethods">System name of selected methods.</param>
        /// <returns>List of select items</returns>
        public static IList<ExtendedSelectListItem> ToSelectListItems(
            this IEnumerable<Provider<IPaymentMethod>> paymentProviders,
            ModuleManager moduleManager,
            params string[] selectedMethods)
        {
            Guard.NotNull(paymentProviders, nameof(paymentProviders));
            Guard.NotNull(moduleManager, nameof(moduleManager));

            var list = new List<ExtendedSelectListItem>();

            foreach (var provider in paymentProviders)
            {
                var systemName = provider.Metadata.SystemName;
                var name = moduleManager.GetLocalizedFriendlyName(provider.Metadata);

                if (name.IsEmpty())
                {
                    name = provider.Metadata.FriendlyName;
                }

                if (name.IsEmpty())
                {
                    name = provider.Metadata.SystemName;
                }

                var item = new ExtendedSelectListItem
                {
                    Text = name.NaIfEmpty(),
                    Value = systemName,
                    Selected = selectedMethods != null && selectedMethods.Contains(systemName)
                };

                item.CustomProperties.Add("hint", systemName.Replace("SmartStore.", "").Replace("Payments.", ""));

                list.Add(item);
            }

            return list.OrderBy(x => x.Text).ToList();
        }

        /// <summary>
        /// Gets a select list of countries.
        /// </summary>
        /// <param name="countries">Countries.</param>
        /// <param name="selectedCountryIds">Identifiers of countries to be selected.</param>
        /// <returns>Select list of countries.</returns>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<Country> countries, params int[] selectedCountryIds)
        {
            Guard.NotNull(countries);

            return countries.Select(country => new CountrySelectListItem
            {
                Text = country.GetLocalized(x => x.Name),
                Value = country.Id.ToStringInvariant(),
                TwoLetterIsoCode = country.TwoLetterIsoCode,
                ThreeLetterIsoCode = country.ThreeLetterIsoCode,
                Selected = selectedCountryIds != null && selectedCountryIds.Contains(country.Id)
            })
            .ToList<SelectListItem>();
        }

        /// <summary>
        /// Gets a select list of countries.
        /// </summary>
        /// <param name="countries">Countries.</param>
        /// <param name="selectedCountryCodes">2-letter ISO codes of countries to be selected.</param>
        /// <returns>Select list of countries.</returns>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<Country> countries, string?[] selectedCountryCodes)
        {
            Guard.NotNull(countries);
            Guard.NotNull(selectedCountryCodes);

            return countries.Select(country => new CountrySelectListItem
            {
                Text = country.GetLocalized(x => x.Name),
                Value = country.TwoLetterIsoCode,
                TwoLetterIsoCode = country.TwoLetterIsoCode,
                ThreeLetterIsoCode = country.ThreeLetterIsoCode,
                Selected = selectedCountryCodes != null && selectedCountryCodes.Any(code => IsSelected(country, code))
            })
            .ToList<SelectListItem>();

            static bool IsSelected(Country country, string? code)
            {
                if (code == null) return false;
                if (code.Length == 2)
                {
                    return country.TwoLetterIsoCode == code;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a select list of state provinces.
        /// </summary>
        /// <param name="stateProvinces">State provinces.</param>
        /// <param name="selectedStateProvinceIds">Identifiers of state provinces to be selected.</param>
        /// <returns>Select list of state provinces. <c>null</c> if <paramref name="stateProvinces"/> does not contain any state provinces.</returns>
        public static IList<SelectListItem>? ToSelectListItems(this IEnumerable<StateProvince> stateProvinces, params int[] selectedStateProvinceIds)
        {
            if (stateProvinces?.Any() ?? false)
            {
                return stateProvinces.Select(x => new SelectListItem
                {
                    Text = x.GetLocalized(x => x.Name),
                    Value = x.Id.ToStringInvariant(),
                    Selected = selectedStateProvinceIds != null && selectedStateProvinceIds.Contains(x.Id)
                })
                .ToList();
            }

            return null;
        }

        /// <summary>
        /// Gets a select list of time zone infos.
        /// </summary>
        /// <param name="timeZoneInfos">Time zone infos.</param>
        /// <param name="selectedTimeZoneId">Identifier of time zone info to be selected.</param>
        /// <returns>Select list of time zone infos.</returns>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<TimeZoneInfo> timeZoneInfos, string? selectedTimeZoneId = null)
        {
            Guard.NotNull(timeZoneInfos);

            return timeZoneInfos.Select(x => new SelectListItem
            {
                Text = x.DisplayName,
                Value = x.Id,
                Selected = selectedTimeZoneId != null && selectedTimeZoneId.EqualsNoCase(x.Id)
            })
            .ToList();
        }

        public static void SelectValue(this IEnumerable<SelectListItem>? list, string value, string? defaultValue = null)
        {
            if (list == null)
            {
                return;
            }

            var item = list.FirstOrDefault(x => x.Value.EqualsNoCase(value));
            if (item == null && defaultValue != null)
            {
                item = list.FirstOrDefault(x => x.Value.EqualsNoCase(defaultValue));
            }

            if (item != null)
            {
                item.Selected = true;
            } 
        }
    }

    public partial class CountrySelectListItem : SelectListItem
    {
        public string? TwoLetterIsoCode { get; set; }
        public string? ThreeLetterIsoCode { get; set; }
    }

    public partial class ExtendedSelectListItem : SelectListItem
    {
        public Dictionary<string, object?> CustomProperties { get; set; } = new();

        public TProperty? Get<TProperty>(string key, TProperty? defaultValue = default)
        {
            if (CustomProperties.TryGetValue(key, out object value))
            {
                return (TProperty)value;
            }

            return defaultValue;
        }
    }
}
