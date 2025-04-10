#nullable enable

using System.Runtime.CompilerServices;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore
{
    public static class CustomerExtensions
    {
        #region Roles

        /// <summary>
        /// Enumerates the system names of the roles the customer is in.
        /// </summary>
        /// <remarks>
        /// Navigation properties <see cref="Customer.CustomerRoleMappings"/> 
        /// then <see cref="CustomerRoleMapping.CustomerRole"/> are required and must be loaded or lazily loadable.
        /// </remarks>
        /// <param name="onlyActiveRoles">A value indicating whether to match only active customer roles.</param>
        public static IEnumerable<string> GetRoleNames(this Customer customer, bool onlyActiveRoles = true)
        {
            Guard.NotNull(customer);

            foreach (var mapping in customer.CustomerRoleMappings)
            {
                var role = mapping.CustomerRole;

                if (role != null && (!onlyActiveRoles || role.Active) && !string.IsNullOrEmpty(role.SystemName))
                {
                    yield return role.SystemName;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether customer is in a certain customer role.
        /// </summary>
        /// <remarks>
        /// Navigation properties <see cref="Customer.CustomerRoleMappings"/> 
        /// then <see cref="CustomerRoleMapping.CustomerRole"/> are required and must be loaded or lazily loadable.
        /// </remarks>
        /// <param name="roleSystemName">Customer role system name.</param>
        /// <param name="onlyActiveRoles">A value indicating whether to match only active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRole(this Customer customer, string roleSystemName, bool onlyActiveRoles = true)
        {
            Guard.NotEmpty(roleSystemName);

            foreach (var mapping in customer.CustomerRoleMappings)
            {
                var role = mapping.CustomerRole;

                if (role != null && (!onlyActiveRoles || role.Active) && roleSystemName.Equals(role.SystemName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether customer is administrator.
        /// </summary>
        /// <remarks>
        /// Navigation properties <see cref="Customer.CustomerRoleMappings"/> 
        /// then <see cref="CustomerRoleMapping.CustomerRole"/> are required and must be loaded or lazily loadable.
        /// </remarks>
        /// <param name="onlyActiveRoles">A value indicating whether to match only active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAdmin(this Customer customer, bool onlyActiveRoles = true)
        {
            return IsInRole(customer, SystemCustomerRoleNames.Administrators, onlyActiveRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is super administrator.
        /// </summary>
        /// <remarks>
        /// Navigation properties <see cref="Customer.CustomerRoleMappings"/> 
        /// then <see cref="CustomerRoleMapping.CustomerRole"/> are required and must be loaded or lazily loadable.
        /// </remarks>
        /// <param name="onlyActiveRoles">A value indicating whether to match only active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuperAdmin(this Customer customer, bool onlyActiveRoles = true)
        {
            return IsInRole(customer, SystemCustomerRoleNames.SuperAdministrators, onlyActiveRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is registered.
        /// </summary>
        /// <remarks>
        /// Navigation properties <see cref="Customer.CustomerRoleMappings"/> 
        /// then <see cref="CustomerRoleMapping.CustomerRole"/> are required and must be loaded or lazily loadable.
        /// </remarks>
        /// <param name="onlyActiveRoles">A value indicating whether to match only active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRegistered(this Customer customer, bool onlyActiveRoles = true)
        {
            return IsInRole(customer, SystemCustomerRoleNames.Registered, onlyActiveRoles);
        }

        /// <summary>
        /// Gets a value indicating whether customer is guest.
        /// </summary>
        /// <remarks>
        /// Navigation properties <see cref="Customer.CustomerRoleMappings"/> 
        /// then <see cref="CustomerRoleMapping.CustomerRole"/> are required and must be loaded or lazily loadable.
        /// </remarks>
        /// <param name="onlyActiveRoles">A value indicating whether to match only active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGuest(this Customer customer, bool onlyActiveRoles = true)
        {
            // Hot path code!
            var isGuest = false;
            var isRegistered = false;

            // A registered user is NOT a guest.
            foreach (var roleName in GetRoleNames(customer, onlyActiveRoles))
            {
                if (!isGuest && string.Equals(roleName, SystemCustomerRoleNames.Guests, StringComparison.OrdinalIgnoreCase))
                {
                    isGuest = true;
                }
                    
                if (!isRegistered && string.Equals(roleName, SystemCustomerRoleNames.Registered, StringComparison.OrdinalIgnoreCase))
                {
                    isRegistered = true;
                }

                if (isGuest && isRegistered)
                {
                    break;
                }
            }

            return isGuest && !isRegistered;
        }

        /// <summary>
        /// Gets a value indicating whether the customer is a built-in record for background tasks.
        /// </summary>
        public static bool IsBackgroundTaskAccount(this Customer customer)
        {
            Guard.NotNull(customer);

            if (!customer.IsSystemAccount)
            {
                return false;
            }

            return SystemCustomerNames.BackgroundTask.EqualsNoCase(customer.SystemName);
        }

        /// <summary>
        /// Gets a value indicating whether customer is a search engine.
        /// </summary>
        public static bool IsBot(this Customer customer)
        {
            Guard.NotNull(customer);

            if (!customer.IsSystemAccount)
            {
                return false;
            }

            return SystemCustomerNames.Bot.Equals(customer.SystemName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a value indicating whether customer is the pdf converter.
        /// </summary>
        public static bool IsPdfConverter(this Customer customer)
        {
            Guard.NotNull(customer);

            if (!customer.IsSystemAccount)
            {
                return false;
            }

            return SystemCustomerNames.PdfConverter.Equals(customer.SystemName, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        /// <summary>
        /// Gets the customer's full name or an empty string if given <paramref name="customer"/> is null.
        /// </summary>
        public static string GetFullName(this Customer? customer)
        {
            if (customer == null)
            {
                return string.Empty;
            }

            if (customer.FullName.HasValue())
            {
                return customer.FullName;
            }

            var name = customer.BillingAddress?.GetFullName();

            if (name.IsEmpty())
            {
                name = customer.ShippingAddress?.GetFullName();
            }

            if (name.IsEmpty())
            {
                name = customer.Addresses.FirstOrDefault()?.GetFullName();
            }

            return name.TrimSafe().EmptyNull();
        }

        /// <summary>
        /// Gets the display name of a customer (full name, user name or email).
        /// </summary>
        /// <returns>Display name of a customer.</returns>
        public static string? GetDisplayName(this Customer? customer, Localizer T)
        {
            Guard.NotNull(T);

            if (customer != null)
            {
                return customer.IsGuest()
                    ? T("Customer.Guest").Value
                    : customer.GetFullName().NullEmpty() ?? customer.Username ?? customer.FindEmail();
            }

            return null;
        }

        /// <summary>
        /// Formats the customer name according to <see cref="CustomerSettings.CustomerNameFormat"/>.
        /// </summary>
        /// <returns>Formatted customer name.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatUserName(this Customer? customer)
        {
            return FormatUserName(customer, false);
        }

        /// <summary>
        /// Formats the customer name according to <see cref="CustomerSettings.CustomerNameFormat"/>.
        /// </summary>
        /// <param name="stripTooLong">
        /// A value that indicates whether to strip too-long customer names according to <see cref="CustomerSettings.CustomerNameFormatMaxLength"/>.
        /// </param>
        /// <param name="appendGuest">
        /// A value that indicates whether the "Guest" suffix should be appended if the customer is a guest.
        /// </param>
        /// <returns>Formatted customer name.</returns>
        public static string FormatUserName(this Customer? customer, bool stripTooLong, bool appendGuest = false)
        {
            if (customer == null)
            {
                return string.Empty;
            }

            var engine = EngineContext.Current.Scope;

            var userName = FormatUserName(
                customer,
                engine.Resolve<CustomerSettings>(),
                engine.Resolve<Localizer>(),
                stripTooLong,
                appendGuest);

            return userName;
        }

        /// <summary>
        /// Formats the customer name according to <see cref="CustomerSettings.CustomerNameFormat"/>.
        /// </summary>
        /// <param name="stripTooLong">
        /// A value that indicates whether to strip too-long customer names according to <see cref="CustomerSettings.CustomerNameFormatMaxLength"/>.
        /// </param>
        /// <param name="appendGuest">
        /// A value that indicates whether the "Guest" suffix should be appended if the customer is a guest.
        /// </param>
        /// <returns>Formatted customer name.</returns>
        public static string FormatUserName(
            this Customer? customer,
            CustomerSettings customerSettings,
            Localizer T,
            bool stripTooLong,
            bool appendGuest = false)
        {
            Guard.NotNull(customerSettings);
            Guard.NotNull(T);

            if (customer == null)
            {
                return string.Empty;
            }

            string? result = null;

            switch (customerSettings.CustomerNameFormat)
            {
                case CustomerNameFormat.ShowEmails:
                    result = customer.Email;
                    break;
                case CustomerNameFormat.ShowFullNames:
                    result = customer.GetFullName();
                    break;
                case CustomerNameFormat.ShowUsernames:
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
                        result = $"{result} {lastName.First()}";
                    }

                    if (city.HasValue())
                    {
                        var from = T("Common.ComingFrom");
                        result = $"{result} {from} {city}";
                    }
                }
                break;
                default:
                    break;
            }

            if (result.IsEmpty())
            {
                result = customer.GetFullName().NullEmpty()
                    ?? customer.Username.NullEmpty()
                    ?? customer.FindEmail().NullEmpty()
                    ?? (!appendGuest && customer.IsGuest() ? T("Customer.Guest") : null)
                    ?? $"#{customer.Id}";
            }

            var suffix = (appendGuest && customer.IsGuest()) ? $" ({T("Customer.Guest")})" : string.Empty;
            var maxLength = stripTooLong ? Math.Max(customerSettings.CustomerNameFormatMaxLength - suffix.Length, 0) : 0;

            if (maxLength > 0 && result != null && result.Length > maxLength)
            {
                result = result.Truncate(maxLength, "…");
            }

            return result + suffix;
        }

        /// <summary>
        /// Find any email address of customer.
        /// </summary>
        public static string? FindEmail(this Customer? customer)
        {
            if (customer != null)
            {
                return customer.Email.NullEmpty()
                    ?? customer.BillingAddress?.Email?.NullEmpty()
                    ?? customer.ShippingAddress?.Email?.NullEmpty();
            }

            return null;
        }

        /// <summary>
        /// Resets data required for checkout. The caller is responsible for database commit.
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="clearCouponCodes">A value indicating whether to clear coupon code</param>
        /// <param name="clearCheckoutAttributes">A value indicating whether to clear selected checkout attributes</param>
        /// <param name="clearRewardPoints">A value indicating whether to clear "Use reward points" flag</param>
        /// <param name="clearShippingMethod">A value indicating whether to clear selected shipping method</param>
        /// <param name="clearPaymentMethod">A value indicating whether to clear selected payment method</param>
        /// <param name="clearCreditBalance">A value indicating whether to clear credit balance.</param>
        public static void ResetCheckoutData(this Customer customer, int storeId,
            bool clearCouponCodes = false, bool clearCheckoutAttributes = false,
            bool clearRewardPoints = false, bool clearShippingMethod = true,
            bool clearPaymentMethod = true,
            bool clearCreditBalance = false)
        {
            Guard.NotNull(customer);

            if (customer.IsTransientRecord())
            {
                return;
            }

            var ga = customer.GenericAttributes;

            if (clearCouponCodes)
            {
                ga.DiscountCouponCode = null;
                ga.RawGiftCardCouponCodes = null;
            }

            if (clearCheckoutAttributes)
            {
                ga.RawCheckoutAttributes = null;
            }

            if (clearRewardPoints)
            {
                ga.Set(SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, false, storeId);
            }

            if (clearCreditBalance)
            {
                ga.Set(SystemCustomerAttributeNames.UseCreditBalanceDuringCheckout, decimal.Zero, storeId);
            }

            if (clearShippingMethod)
            {
                ga.Set<string?>(SystemCustomerAttributeNames.SelectedShippingOption, null, storeId);
                ga.Set<string?>(SystemCustomerAttributeNames.OfferedShippingOptions, null, storeId);
            }

            if (clearPaymentMethod)
            {
                ga.Set<string?>(SystemCustomerAttributeNames.SelectedPaymentMethod, null, storeId);
            }
        }
    }
}
