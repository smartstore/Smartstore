using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common;

namespace Smartstore.Core.Customers
{
    public class CustomerAttributeCollection : GenericAttributeCollection<Customer>
    {
        public CustomerAttributeCollection(GenericAttributeCollection collection)
            : base(collection)
        {
        }

        #region Form fields

        public string StreetAddress
        {
            get => Get<string>(SystemCustomerAttributeNames.StreetAddress);
            set => Set(SystemCustomerAttributeNames.StreetAddress, value);
        }

        public string StreetAddress2
        {
            get => Get<string>(SystemCustomerAttributeNames.StreetAddress2);
            set => Set(SystemCustomerAttributeNames.StreetAddress2, value);
        }

        public string ZipPostalCode
        {
            get => Get<string>(SystemCustomerAttributeNames.ZipPostalCode);
            set => Set(SystemCustomerAttributeNames.ZipPostalCode, value);
        }

        public string City
        {
            get => Get<string>(SystemCustomerAttributeNames.City);
            set => Set(SystemCustomerAttributeNames.City, value);
        }

        public int? StateProvinceId
        {
            get => Get<int?>(SystemCustomerAttributeNames.StateProvinceId);
            set => Set(SystemCustomerAttributeNames.StateProvinceId, value);
        }

        public int? CountryId
        {
            get => Get<int?>(SystemCustomerAttributeNames.CountryId);
            set => Set(SystemCustomerAttributeNames.CountryId, value);
        }

        public string Phone
        {
            get => Get<string>(SystemCustomerAttributeNames.Phone);
            set => Set(SystemCustomerAttributeNames.Phone, value);
        }

        public string Fax
        {
            get => Get<string>(SystemCustomerAttributeNames.Fax);
            set => Set(SystemCustomerAttributeNames.Fax, value);
        }

        public string VatNumber
        {
            get => Get<string>(SystemCustomerAttributeNames.VatNumber);
            set => Set(SystemCustomerAttributeNames.VatNumber, value);
        }

        public int? AvatarPictureId
        {
            get => Get<int?>(SystemCustomerAttributeNames.AvatarPictureId);
            set => Set(SystemCustomerAttributeNames.AvatarPictureId, value);
        }

        public string AvatarColor
        {
            get => Get<string>(SystemCustomerAttributeNames.AvatarColor);
            set => Set(SystemCustomerAttributeNames.AvatarColor, value);
        }

        public int ForumPostCount
        {
            get => Get<int>(SystemCustomerAttributeNames.ForumPostCount);
            set => Set(SystemCustomerAttributeNames.ForumPostCount, value);
        }

        public string Signature
        {
            get => Get<string>(SystemCustomerAttributeNames.Signature);
            set => Set(SystemCustomerAttributeNames.Signature, value);
        }

        public string PasswordRecoveryToken
        {
            get => Get<string>(SystemCustomerAttributeNames.PasswordRecoveryToken);
            set => Set(SystemCustomerAttributeNames.PasswordRecoveryToken, value);
        }

        public string AccountActivationToken
        {
            get => Get<string>(SystemCustomerAttributeNames.AccountActivationToken);
            set => Set(SystemCustomerAttributeNames.AccountActivationToken, value);
        }

        public string LastVisitedPage
        {
            get => Get<string>(SystemCustomerAttributeNames.LastVisitedPage);
            set => Set(SystemCustomerAttributeNames.LastVisitedPage, value);
        }

        public int? ImpersonatedCustomerId
        {
            get => Get<int?>(SystemCustomerAttributeNames.ImpersonatedCustomerId);
            set => Set(SystemCustomerAttributeNames.ImpersonatedCustomerId, value);
        }

        public int AdminAreaStoreScopeConfiguration
        {
            get => Get<int>(SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration);
            set => Set(SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration, value);
        }

        public string MostRecentlyUsedCategories
        {
            get => Get<string>(SystemCustomerAttributeNames.MostRecentlyUsedCategories);
            set => Set(SystemCustomerAttributeNames.MostRecentlyUsedCategories, value);
        }

        public string MostRecentlyUsedManufacturers
        {
            get => Get<string>(SystemCustomerAttributeNames.MostRecentlyUsedManufacturers);
            set => Set(SystemCustomerAttributeNames.MostRecentlyUsedManufacturers, value);
        }

        public bool HasConsentedToGdpr
        {
            get => Get<bool>(SystemCustomerAttributeNames.HasConsentedToGdpr);
            set => Set(SystemCustomerAttributeNames.HasConsentedToGdpr, value);
        }

        // TODO: (mg) (core) WalletEnabled belongs to Wallet module (as extension method for CustomerAttributeCollection)

        #endregion

        #region Other attributes

        public string DiscountCouponCode
        {
            get => Get<string>(SystemCustomerAttributeNames.DiscountCouponCode);
            set => Set(SystemCustomerAttributeNames.DiscountCouponCode, value);
        }

        public IEnumerable<GiftCardCouponCode> GiftCardCouponCodes
        {
            get => Get<string>(SystemCustomerAttributeNames.GiftCardCouponCodes).Convert<List<GiftCardCouponCode>>() ?? Enumerable.Empty<GiftCardCouponCode>();
            set => Set(SystemCustomerAttributeNames.GiftCardCouponCodes, value.Convert<string>());
        }

        public CheckoutAttributeSelection CheckoutAttributes
        {
            get => new(Get<string>(SystemCustomerAttributeNames.CheckoutAttributes));
            set => Set(SystemCustomerAttributeNames.CheckoutAttributes, value.AsJson());
        }

        #endregion

        #region Depends on store

        public int? CurrencyId
        {
            get => Get<int?>(SystemCustomerAttributeNames.CurrencyId, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.CurrencyId, value, CurrentStoreId);
        }

        public int? LanguageId
        {
            get => Get<int?>(SystemCustomerAttributeNames.LanguageId, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.LanguageId, value, CurrentStoreId);
        }

        public string SelectedPaymentMethod
        {
            get => Get<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.SelectedPaymentMethod, value, CurrentStoreId);
        }

        // TODO: (core) Use Domain type 'ShippingOption', not string.
        public string SelectedShippingOption
        {
            get => Get<string>(SystemCustomerAttributeNames.SelectedShippingOption, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.SelectedShippingOption, value, CurrentStoreId);
        }

        // TODO: (core) Use type 'List<ShippingOption>', not string.
        public string OfferedShippingOptions
        {
            get => Get<string>(SystemCustomerAttributeNames.OfferedShippingOptions, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.OfferedShippingOptions, value, CurrentStoreId);
        }

        public string LastContinueShoppingPage
        {
            get => Get<string>(SystemCustomerAttributeNames.LastContinueShoppingPage, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.LastContinueShoppingPage, value, CurrentStoreId);
        }

        public bool NotifiedAboutNewPrivateMessages
        {
            get => Get<bool>(SystemCustomerAttributeNames.NotifiedAboutNewPrivateMessages, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.NotifiedAboutNewPrivateMessages, value, CurrentStoreId);
        }

        public string WorkingThemeName
        {
            get => Get<string>(SystemCustomerAttributeNames.WorkingThemeName, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.WorkingThemeName, value, CurrentStoreId);
        }

        public bool UseRewardPointsDuringCheckout
        {
            get => Get<bool>(SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, value, CurrentStoreId);
        }

        public decimal UseCreditBalanceDuringCheckout
        {
            get => Get<decimal>(SystemCustomerAttributeNames.UseCreditBalanceDuringCheckout, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.UseCreditBalanceDuringCheckout, value, CurrentStoreId);
        }

        #endregion
    }
}
