using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
    public class CustomerModel : TabbableModel
    {
        public bool AllowUsersToChangeUsernames { get; set; }
        public bool UsernamesEnabled { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }
        public bool TitleEnabled { get; set; }

        public bool GenderEnabled { get; set; }
        [LocalizedDisplay("*Gender")]
        public string Gender { get; set; }

        [LocalizedDisplay("*FirstName")]
        public string FirstName { get; set; }

        [LocalizedDisplay("*LastName")]
        public string LastName { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }

        public bool DateOfBirthEnabled { get; set; }
        [LocalizedDisplay("*DateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        public bool CompanyEnabled { get; set; }

        [LocalizedDisplay("*Company")]
        public string Company { get; set; }

        public bool CustomerNumberEnabled { get; set; }

        [LocalizedDisplay("Account.Fields.CustomerNumber")]
        public string CustomerNumber { get; set; }

        public bool StreetAddressEnabled { get; set; }

        [LocalizedDisplay("*StreetAddress")]
        public string StreetAddress { get; set; }

        public bool StreetAddress2Enabled { get; set; }

        [LocalizedDisplay("*StreetAddress2")]
        public string StreetAddress2 { get; set; }

        public bool ZipPostalCodeEnabled { get; set; }

        [LocalizedDisplay("*ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        public bool CityEnabled { get; set; }

        [LocalizedDisplay("*City")]
        public string City { get; set; }

        public bool CountryEnabled { get; set; }

        [LocalizedDisplay("*Country")]
        public int CountryId { get; set; }

        public bool StateProvinceEnabled { get; set; }

        [LocalizedDisplay("*StateProvince")]
        public int StateProvinceId { get; set; }

        public bool PhoneEnabled { get; set; }

        [LocalizedDisplay("*Phone")]
        public string Phone { get; set; }

        public bool FaxEnabled { get; set; }

        [LocalizedDisplay("*Fax")]
        public string Fax { get; set; }

        [UIHint("Textarea"), AdditionalMetadata("rows", 6)]
        [LocalizedDisplay("*AdminComment")]
        public string AdminComment { get; set; }

        [LocalizedDisplay("*IsTaxExempt")]
        public bool IsTaxExempt { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("*Affiliate")]
        public int AffiliateId { get; set; }
        public string AffiliateFullName { get; set; }

        [LocalizedDisplay("*TimeZoneId")]
        public string TimeZoneId { get; set; }

        public bool AllowCustomersToSetTimeZone { get; set; }

        [LocalizedDisplay("*VatNumber")]
        public string VatNumber { get; set; }
        public string VatNumberStatusNote { get; set; }
        public bool DisplayVatNumber { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*LastActivityDate")]
        public DateTime LastActivityDate { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string LastIpAddress { get; set; }

        [LocalizedDisplay("*LastVisitedPage")]
        public string LastVisitedPage { get; set; }

        [LocalizedDisplay("*CustomerRoles")]
        public string CustomerRoleNames { get; set; }

        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Customers.CustomerRoles")]
        public int[] SelectedCustomerRoleIds { get; set; }
        public bool AllowManagingCustomerRoles { get; set; }

        public bool DisplayRewardPointsHistory { get; set; }
        public bool DisplayProfileLink { get; set; }

        [LocalizedDisplay("Admin.Customers.Customers.AssociatedExternalAuth")]
        public List<AssociatedExternalAuthModel> AssociatedExternalAuthRecords { get; set; } = new();

        public bool Deleted { get; set; }
        public string EditUrl { get; set; }
        public bool HasOrders { get; set; }
        public PermissionTree PermissionTree { get; set; }
        public List<AddressModel> Addresses { get; set; } = new();

        #region Nested classes

        [LocalizedDisplay("Admin.Customers.Customers.AssociatedExternalAuth.Fields.")]
        public class AssociatedExternalAuthModel : EntityModelBase
        {
            [LocalizedDisplay("*Email")]
            public string Email { get; set; }

            [LocalizedDisplay("*ExternalIdentifier")]
            public string ExternalIdentifier { get; set; }

            [LocalizedDisplay("*AuthMethodName")]
            public string AuthMethodName { get; set; }
        }

        [LocalizedDisplay("Admin.Customers.Customers.RewardPoints.Fields.")]
        public class RewardPointsHistoryModel : EntityModelBase
        {
            [LocalizedDisplay("*Points")]
            public int Points { get; set; }

            [LocalizedDisplay("*PointsBalance")]
            public int PointsBalance { get; set; }

            [LocalizedDisplay("*Message")]
            public string Message { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        [LocalizedDisplay("Admin.Customers.Customers.SendEmail.")]
        public class SendEmailModel : ModelBase
        {
            public int CustomerId { get; set; }

            [LocalizedDisplay("*Subject")]
            public string Subject { get; set; }

            [UIHint("Textarea"), AdditionalMetadata("rows", 6)]
            [LocalizedDisplay("*Body")]
            public string Body { get; set; }
        }

        [LocalizedDisplay("Admin.Customers.Customers.Orders.")]
        public class OrderModel : EntityModelBase
        {
            [LocalizedDisplay("*ID")]
            public override int Id { get; set; }

            [LocalizedDisplay("*OrderStatus")]
            public string OrderStatus { get; set; }

            [LocalizedDisplay("*PaymentStatus")]
            public string PaymentStatus { get; set; }

            [LocalizedDisplay("*ShippingStatus")]
            public string ShippingStatus { get; set; }

            [LocalizedDisplay("*OrderTotal")]
            public Money OrderTotal { get; set; }

            [LocalizedDisplay("*Store")]
            public string StoreName { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            public string EditUrl { get; set; }
        }

        public class SendEmailValidator : SmartValidator<SendEmailModel>
        {
            public SendEmailValidator()
            {
                RuleFor(x => x.Subject).NotEmpty();
                RuleFor(x => x.Body).NotEmpty();
            }
        }
        #endregion
    }

    public partial class CustomerValidator : AbstractValidator<CustomerModel>
    {
        public CustomerValidator(CustomerSettings customerSettings)
        {
            RuleFor(x => x.Password).NotEmpty().When(x => x.Id == 0);

            if (customerSettings.FirstNameRequired)
                RuleFor(x => x.FirstName).NotEmpty();

            if (customerSettings.LastNameRequired)
                RuleFor(x => x.LastName).NotEmpty();

            if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
                RuleFor(x => x.Company).NotEmpty();

            if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
                RuleFor(x => x.StreetAddress).NotEmpty();

            if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
                RuleFor(x => x.StreetAddress2).NotEmpty();

            if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
                RuleFor(x => x.ZipPostalCode).NotEmpty();

            if (customerSettings.CityRequired && customerSettings.CityEnabled)
                RuleFor(x => x.City).NotEmpty();

            if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
                RuleFor(x => x.Phone).NotEmpty();

            if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
                RuleFor(x => x.Fax).NotEmpty();
        }
    }
}
