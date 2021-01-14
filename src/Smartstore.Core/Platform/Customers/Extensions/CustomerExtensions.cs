using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Core.Customers;
using Smartstore.Core.Localization;
using Smartstore.Engine;

namespace Smartstore
{
    public static class CustomerExtentions
    {
        private static readonly string[] _systemColors = new string[] { "primary", "secondary", "success", "info", "warning", "danger", "light", "dark" };

        /// <summary>
        /// Gets a value indicating whether customer is in a certain customer role.
        /// </summary>
        /// <param name="customerRoleSystemName">Customer role system name.</param>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles.</param>
        public static bool IsInCustomerRole(this Customer customer, string customerRoleSystemName, bool onlyActiveCustomerRoles = true)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotEmpty(customerRoleSystemName, nameof(customerRoleSystemName));

            var result = customer.CustomerRoleMappings
                .Where(rm => !onlyActiveCustomerRoles || rm.CustomerRole.Active)
                .Where(rm => rm.CustomerRole.SystemName == customerRoleSystemName)
                .FirstOrDefault() != null;

            return result;
        }

        /// <summary>
        /// Gets a value indicating whether the customer is a built-in record for background tasks.
        /// </summary>
        public static bool IsBackgroundTaskAccount(this Customer customer)
        {
            Guard.NotNull(customer, nameof(customer));

            if (!customer.IsSystemAccount || customer.SystemName.IsEmpty())
                return false;

            return customer.SystemName.Equals(SystemCustomerNames.BackgroundTask, StringComparison.InvariantCultureIgnoreCase); ;
        }

        /// <summary>
        /// Gets a value indicating whether customer is a search engine.
        /// </summary>
        public static bool IsSearchEngineAccount(this Customer customer)
        {
            Guard.NotNull(customer, nameof(customer));

            if (!customer.IsSystemAccount || customer.SystemName.IsEmpty())
                return false;

            return customer.SystemName.Equals(SystemCustomerNames.SearchEngine, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets a value indicating whether customer is the pdf converter.
        /// </summary>
        public static bool IsPdfConverter(this Customer customer)
        {
            Guard.NotNull(customer, nameof(customer));

            if (!customer.IsSystemAccount || customer.SystemName.IsEmpty())
                return false;

            return customer.SystemName.Equals(SystemCustomerNames.PdfConverter, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets a value indicating whether customer is administrator.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAdmin(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.Administrators, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is super administrator.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuperAdmin(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.SuperAdministrators, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is a forum moderator.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsForumModerator(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.ForumModerators, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is registered.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRegistered(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.Registered, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is guest.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGuest(this Customer customer, bool onlyActiveCustomerRoles = true)
        {
            return IsInCustomerRole(customer, SystemCustomerRoleNames.Guests, onlyActiveCustomerRoles);
        }

        /// <summary>
        /// Gets the customer's full name.
        /// </summary>
        public static string GetFullName(this Customer customer)
        {
            if (customer == null)
                return string.Empty;

            if (customer.FullName.HasValue())
            {
                return customer.FullName;
            }

            string name = customer.BillingAddress?.GetFullName();
            if (name.IsEmpty())
            {
                name = customer.ShippingAddress?.GetFullName();
            }
            if (name.IsEmpty())
            {
                name = customer.Addresses.FirstOrDefault()?.GetFullName();
            }

            return name.TrimSafe();
        }

        /// <summary>
        /// Gets the display name of a customer (full name, user name or email).
        /// </summary>
        /// <returns>Display name of a customer.</returns>
        public static string GetDisplayName(this Customer customer, Localizer T)
        {
            if (customer != null)
            {
                return customer.IsGuest()
                    ? T("Customer.Guest").Value
                    : customer.GetFullName().NullEmpty() ?? customer.Username ?? customer.FindEmail();
            }

            return null;
        }

        /// <summary>
        /// Formats the customer name.
        /// </summary>
        /// <returns>Formatted customer name.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatUserName(this Customer customer)
        {
            return FormatUserName(customer, false);
        }

        /// <summary>
        /// Formats the customer name.
        /// </summary>
        /// <param name="customer">Customer entity.</param>
        /// <param name="stripTooLong">Whether to strip too long customer name.</param>
        /// <returns>Formatted customer name.</returns>
        public static string FormatUserName(this Customer customer, bool stripTooLong)
        {
            // TODO: (mh) (core) Scope or Application?
            var engine = EngineContext.Current.Application.Services;

            var userName = customer.FormatUserName(
                engine.Resolve<CustomerSettings>(),
                engine.Resolve<Localizer>(),
                stripTooLong);

            return userName;
        }

        /// <summary>
        /// Formats the customer name.
        /// </summary>
        /// <param name="customer">Customer entity.</param>
        /// <param name="customerSettings">Customer settings.</param>
        /// <param name="T">Localizer.</param>
        /// <param name="stripTooLong">Whether to strip too long customer name.</param>
        /// <returns>Formatted customer name.</returns>
        public static string FormatUserName(
            this Customer customer,
            CustomerSettings customerSettings,
            Localizer T,
            bool stripTooLong)
        {
            Guard.NotNull(customerSettings, nameof(customerSettings));
            Guard.NotNull(T, nameof(T));

            if (customer == null)
            {
                return string.Empty;
            }
            if (customer.IsGuest())
            {
                return T("Customer.Guest");
            }

            var result = string.Empty;

            switch (customerSettings.CustomerNameFormat)
            {
                case CustomerNameFormat.ShowEmail:
                    result = customer.Email;
                    break;
                case CustomerNameFormat.ShowFullName:
                    result = customer.GetFullName();
                    break;
                case CustomerNameFormat.ShowUsername:
                    result = customer.Username;
                    break;
                case CustomerNameFormat.ShowFirstName:
                    result = customer.FirstName;
                    break;
                case CustomerNameFormat.ShowNameAndCity:
                    {
                        var firstName = customer.FirstName;
                        var lastName = customer.LastName;
                        var city = customer.GenericAttributes.City;

                        if (firstName.IsEmpty())
                        {
                            var address = customer.Addresses.FirstOrDefault();
                            if (address != null)
                            {
                                firstName = address.FirstName;
                                lastName = address.LastName;
                                city = address.City;
                            }
                        }

                        result = firstName;
                        if (lastName.HasValue())
                        {
                            result = "{0} {1}.".FormatInvariant(result, lastName.First());
                        }

                        if (city.HasValue())
                        {
                            var from = T("Common.ComingFrom");
                            result = "{0} {1} {2}".FormatInvariant(result, from, city);
                        }
                    }
                    break;
                default:
                    break;
            }

            var maxLength = customerSettings.CustomerNameFormatMaxLength;
            if (stripTooLong && maxLength > 0 && result != null && result.Length > maxLength)
            {
                result = result.Truncate(maxLength, "...");
            }

            return result;
        }

        /// <summary>
        /// Find any email address of customer.
        /// </summary>
        public static string FindEmail(this Customer customer)
        {
            if (customer != null)
            {
                return customer.Email.NullEmpty()
                    ?? customer.BillingAddress?.Email?.NullEmpty()
                    ?? customer.ShippingAddress?.Email?.NullEmpty();
            }

            return null;
        }

        // TODO: (mh) (core) > Evaluate & implement other relevant extension methods.
    }
}
