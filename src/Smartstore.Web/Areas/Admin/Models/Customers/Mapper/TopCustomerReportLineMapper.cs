using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Customers
{
    internal static partial class TopCustomerReportLineMappingExtensions
    {
        public static async Task<IList<TopCustomerReportLineModel>> MapAsync(this IEnumerable<TopCustomerReportLine> lines, SmartDbContext db)
        {
            var customerIds = lines.ToDistinctArray(x => x.CustomerId);
            if (customerIds.Length == 0)
            {
                return new List<TopCustomerReportLineModel>();
            }

            var customers = await db.Customers
                .AsNoTracking()
                .Include(x => x.BillingAddress)
                .Include(x => x.ShippingAddress)
                .Include(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .Where(x => customerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            dynamic parameters = new ExpandoObject();
            parameters.Customers = customers;

            var mapper = MapperFactory.GetMapper<TopCustomerReportLine, TopCustomerReportLineModel>();
            var models = await lines
                .SelectAwait(async x =>
                {
                    var model = new TopCustomerReportLineModel();
                    await mapper.MapAsync(x, model, parameters);
                    return model;
                })
                .AsyncToList();

            return models;
        }
    }

    internal class TopCustomerReportLineMapper : Mapper<TopCustomerReportLine, TopCustomerReportLineModel>
    {
        private readonly ICommonServices _services;
        private readonly IUrlHelper _urlHelper;
        private readonly CustomerSettings _customerSettings;

        public TopCustomerReportLineMapper(ICommonServices services, IUrlHelper urlHelper, CustomerSettings customerSettings)
        {
            _services = services;
            _urlHelper = urlHelper;
            _customerSettings = customerSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(TopCustomerReportLine from, TopCustomerReportLineModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override Task MapAsync(TopCustomerReportLine from, TopCustomerReportLineModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var customers = parameters.Customers as Dictionary<int, Customer>;
            var customer = customers?.Get(from.CustomerId);

            to.OrderTotal = _services.CurrencyService.CreateMoney(from.OrderTotal, _services.CurrencyService.PrimaryCurrency);
            to.OrderCount = from.OrderCount.ToString("N0");
            to.CustomerId = from.CustomerId;
            to.CustomerNumber = customer?.CustomerNumber;
            to.CustomerDisplayName = customer?.FindEmail() ?? customer?.FormatUserName(_customerSettings, T, false) ?? StringExtensions.NotAvailable;
            to.Email = customer?.Email.NullEmpty() ?? (customer != null && customer.IsGuest() ? T("Admin.Customers.Guest") : StringExtensions.NotAvailable);
            to.Username = customer?.Username;
            to.FullName = customer?.GetFullName();
            to.Active = customer?.Active == true;
            to.LastActivityDate = _services.DateTimeHelper.ConvertToUserTime(customer?.LastActivityDateUtc ?? DateTime.MinValue, DateTimeKind.Utc);
            to.EditUrl = _urlHelper.Action("Edit", "Customer", new { id = from.CustomerId, Area = "Admin" });

            return Task.CompletedTask;
        }
    }
}
