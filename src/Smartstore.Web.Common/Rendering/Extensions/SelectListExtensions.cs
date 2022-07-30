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
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("An Enumeration type is required.", nameof(enumObj));

            var workContext = EngineContext.Current.Scope.Resolve<IWorkContext>();

            var values = from TEnum enumValue in Enum.GetValues(typeof(TEnum))
                         select new { ID = Convert.ToInt32(enumValue), Name = enumValue.GetLocalizedEnum(workContext.WorkingLanguage.Id) };

            object selectedValue = null;
            if (markCurrentAsSelected)
            {
                selectedValue = Convert.ToInt32(enumObj);
            }

            return new SelectList(values, "ID", "Name", selectedValue);
        }

        /// <summary>
        /// Get a select list of all stores
        /// </summary>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<Store> stores, params int[] selectedStoreIds)
        {
            Guard.NotNull(stores, nameof(stores));

            return stores.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString(),
                Selected = selectedStoreIds != null && selectedStoreIds.Contains(x.Id)
            })
            .ToList();
        }

        /// <summary>
        /// Get a select list of all customer roles
        /// </summary>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<CustomerRole> roles, params int[] selectedCustomerRoleIds)
        {
            Guard.NotNull(roles, nameof(roles));

            return roles.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString(),
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
            Guard.NotNull(countries, nameof(countries));

            return countries.Select(x => new SelectListItem
            {
                Text = x.GetLocalized(x => x.Name),
                Value = x.Id.ToString(),
                Selected = selectedCountryIds != null && selectedCountryIds.Contains(x.Id)
            })
            .ToList();
        }

        /// <summary>
        /// Gets a select list of state provinces.
        /// </summary>
        /// <param name="stateProvinces">State provinces.</param>
        /// <param name="selectedStateProvinceIds">Identifiers of state provinces to be selected.</param>
        /// <returns>Select list of state provinces. <c>null</c> if <paramref name="stateProvinces"/> does not contain any state provinces.</returns>
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<StateProvince> stateProvinces, params int[] selectedStateProvinceIds)
        {
            if (stateProvinces?.Any() ?? false)
            {
                return stateProvinces.Select(x => new SelectListItem
                {
                    Text = x.GetLocalized(x => x.Name),
                    Value = x.Id.ToString(),
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
        public static IList<SelectListItem> ToSelectListItems(this IEnumerable<TimeZoneInfo> timeZoneInfos, string selectedTimeZoneId = null)
        {
            Guard.NotNull(timeZoneInfos, nameof(timeZoneInfos));

            return timeZoneInfos.Select(x => new SelectListItem
            {
                Text = x.DisplayName,
                Value = x.Id,
                Selected = selectedTimeZoneId != null && selectedTimeZoneId.EqualsNoCase(x.Id)
            })
            .ToList();
        }

        public static void SelectValue(this IEnumerable<SelectListItem> list, string value, string defaultValue = null)
        {
            // INFO: (mh) (core) Please don't port via copy&paste!!!
            if (list == null)
                return;

            var item = list.FirstOrDefault(x => x.Value.EqualsNoCase(value));

            if (item == null && defaultValue != null)
                item = list.FirstOrDefault(x => x.Value.EqualsNoCase(defaultValue));

            if (item != null)
                item.Selected = true;
        }
    }


    public partial class ExtendedSelectListItem : SelectListItem
    {
        public ExtendedSelectListItem()
        {
            CustomProperties = new Dictionary<string, object>();
        }

        public Dictionary<string, object> CustomProperties { get; set; }

        public TProperty Get<TProperty>(string key, TProperty defaultValue = default)
        {
            if (CustomProperties.TryGetValue(key, out object value))
            {
                return (TProperty)value;
            }

            return defaultValue;
        }
    }
}
