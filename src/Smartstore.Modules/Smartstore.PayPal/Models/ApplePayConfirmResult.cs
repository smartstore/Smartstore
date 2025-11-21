namespace Smartstore.PayPal.Models
{
    public class ApplePayConfirmResult
    {
        public string Id;
        public string Status;
        public ApplePayAddress BillingAddress;
        public ApplePayAddress ShippingAddress;
    }

    public class ApplePayAddress
    {
        public List<string> AddressLines { get; set; }
        public string AdministrativeArea { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string EmailAddress { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public string Locality { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneticFamilyName { get; set; }
        public string PhoneticGivenName { get; set; }
        public string PostalCode { get; set; }
        public string SubAdministrativeArea { get; set; }
        public string SubLocality { get; set; }
    }
}