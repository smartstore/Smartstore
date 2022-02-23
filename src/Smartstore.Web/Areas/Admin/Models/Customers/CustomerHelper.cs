using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Customers
{
    public partial class CustomerHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly CustomerSettings _customerSettings;
        // INFO: (mh) (core) Controllers seem to be instanciated before the HttpContext scope is available.
        // Therefore IUrlHelper resolution will fail when passed to a controller ctor > Allways pass Lazy<IUrlHelper>
        private readonly Lazy<IUrlHelper> _urlHelper;
        
        public CustomerHelper(SmartDbContext db, ICommonServices services, CustomerSettings customerSettings, Lazy<IUrlHelper> urlHelper)
        {
            _db = db;
            _services = services;
            _customerSettings = customerSettings;
            _urlHelper = urlHelper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<List<TopCustomerReportLineModel>> CreateCustomerReportLineModelAsync(IList<TopCustomerReportLine> items)
        {
            var customerIds = items.ToDistinctArray(x => x.CustomerId);
            if (customerIds.Length == 0)
            {
                return new List<TopCustomerReportLineModel>();
            }

            var customers = await _db.Customers
                .AsNoTracking()
                .Include(x => x.BillingAddress)
                .Include(x => x.ShippingAddress)
                .Include(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .Where(x => customerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var guestStr = T("Admin.Customers.Guest").Value;

            var model = items.Select(x =>
            {
                var customer = customers.Get(x.CustomerId);

                var m = new TopCustomerReportLineModel
                {
                    OrderTotal = _services.CurrencyService.PrimaryCurrency.AsMoney(x.OrderTotal),
                    OrderCount = x.OrderCount.ToString("N0"),
                    CustomerId = x.CustomerId,
                    CustomerNumber = customer?.CustomerNumber,
                    CustomerDisplayName = customer?.FindEmail() ?? customer?.FormatUserName(_customerSettings, T, false) ?? StringExtensions.NotAvailable,
                    Email = customer?.Email.NullEmpty() ?? (customer != null && customer.IsGuest() ? guestStr : StringExtensions.NotAvailable),
                    Username = customer?.Username,
                    FullName = customer?.GetFullName(),
                    Active = customer?.Active == true,
                    LastActivityDate = _services.DateTimeHelper.ConvertToUserTime(customer?.LastActivityDateUtc ?? DateTime.MinValue, DateTimeKind.Utc),
                    EditUrl = _urlHelper.Value.Action("Edit", "Customer", new { id = x.CustomerId })
                };

                return m;
            })
            .ToList();

            return model;
        }
    }
}
