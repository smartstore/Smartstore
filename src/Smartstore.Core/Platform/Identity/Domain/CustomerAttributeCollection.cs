using Newtonsoft.Json;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;

namespace Smartstore.Core.Identity
{
    public class CustomerAttributeCollection : GenericAttributeCollection<Customer>
    {
        public CustomerAttributeCollection(GenericAttributeCollection collection)
            : base(collection)
        {
        }

        #region Form fields

        // INFO: this address data is for information purposes only (e.g. on the customer profile page).
        // Usually customer addresses are used for it (e.g. in checkout).

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

        #endregion

        #region Other attributes

        public string DiscountCouponCode
        {
            get => Get<string>(SystemCustomerAttributeNames.DiscountCouponCode);
            set => Set(SystemCustomerAttributeNames.DiscountCouponCode, value);
        }

        private IEnumerable<GiftCardCouponCode> _giftCardCouponCodes;
        public IEnumerable<GiftCardCouponCode> GiftCardCouponCodes
        {
            get => _giftCardCouponCodes ??= (RawGiftCardCouponCodes.Convert<List<GiftCardCouponCode>>() ?? Enumerable.Empty<GiftCardCouponCode>());
            set
            {
                Set(SystemCustomerAttributeNames.GiftCardCouponCodes, value.Convert<string>());
                _giftCardCouponCodes = null;
            }
        }

        public string RawGiftCardCouponCodes
        {
            get => Get<string>(SystemCustomerAttributeNames.GiftCardCouponCodes);
            set
            {
                Set(SystemCustomerAttributeNames.GiftCardCouponCodes, value);
                _giftCardCouponCodes = null;
            }
        }

        private CheckoutAttributeSelection _checkoutAttributes;
        public CheckoutAttributeSelection CheckoutAttributes
        {
            get => _checkoutAttributes ??= new(RawCheckoutAttributes);
            set
            {
                Set(SystemCustomerAttributeNames.CheckoutAttributes, value.AsJson());
                _checkoutAttributes = null;
            }
        }

        public string RawCheckoutAttributes
        {
            get => Get<string>(SystemCustomerAttributeNames.CheckoutAttributes);
            set
            {
                Set(SystemCustomerAttributeNames.CheckoutAttributes, value);
                _checkoutAttributes = null;
            }
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

        public string ClientIdent
        {
            get => Get<string>(SystemCustomerAttributeNames.ClientIdent);
            set => Set(SystemCustomerAttributeNames.ClientIdent, value);
        }

        #endregion

        #region Checkout

        public int? DefaultBillingAddressId
        {
            get => Get<int?>(SystemCustomerAttributeNames.DefaultBillingAddressId, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.DefaultBillingAddressId, value, CurrentStoreId);
        }

        public int? DefaultShippingAddressId
        {
            get => Get<int?>(SystemCustomerAttributeNames.DefaultShippingAddressId, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.DefaultShippingAddressId, value, CurrentStoreId);
        }

        public ShippingOption PreferredShippingOption
        {
            get
            {
                var rawOption = Get<string>(SystemCustomerAttributeNames.PreferredShippingOption, CurrentStoreId);
                return rawOption.HasValue() ? JsonConvert.DeserializeObject<ShippingOption>(rawOption) : null;
            }
            set
            {
                string rawOption = null;
                var methodId = value?.ShippingMethodId ?? 0;

                if (methodId != 0)
                {
                    rawOption = JsonConvert.SerializeObject(new ShippingOption
                    {
                        ShippingMethodId = methodId,
                        ShippingRateComputationMethodSystemName = value?.ShippingRateComputationMethodSystemName
                    });
                }

                Set(SystemCustomerAttributeNames.PreferredShippingOption, rawOption, CurrentStoreId);
            }
        }

        public string PreferredPaymentMethod
        {
            get => Get<string>(SystemCustomerAttributeNames.PreferrePaymentMethod, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.PreferrePaymentMethod, value, CurrentStoreId);
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

        public ShippingOption SelectedShippingOption
        {
            get => Get<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.SelectedShippingOption, value, CurrentStoreId);
        }

        public List<ShippingOption> OfferedShippingOptions
        {
            get => Get<List<ShippingOption>>(SystemCustomerAttributeNames.OfferedShippingOptions, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.OfferedShippingOptions, value, CurrentStoreId);
        }

        public string SelectedPaymentMethod
        {
            get => Get<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.SelectedPaymentMethod, value, CurrentStoreId);
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

        public string LastContinueShoppingPage
        {
            get => Get<string>(SystemCustomerAttributeNames.LastContinueShoppingPage, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.LastContinueShoppingPage, value, CurrentStoreId);
        }

        public string WorkingThemeName
        {
            get => Get<string>(SystemCustomerAttributeNames.WorkingThemeName, CurrentStoreId);
            set => Set(SystemCustomerAttributeNames.WorkingThemeName, value, CurrentStoreId);
        }

        #endregion
    }
}
