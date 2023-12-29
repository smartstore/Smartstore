using System.Runtime.CompilerServices;

using Smartstore.Collections;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Rules.Impl;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Identity.Rules
{
    public partial class TargetGroupService : RuleProviderBase, ITargetGroupService
    {
        private readonly SmartDbContext _db;
        private readonly IRuleService _ruleService;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;

        private readonly Currency _primaryCurrency;

        public TargetGroupService(
            SmartDbContext db,
            IRuleService ruleService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            ICurrencyService currencyService)
            : base(RuleScope.Customer)
        {
            _db = db;
            _ruleService = ruleService;
            _storeContext = storeContext;
            _localizationService = localizationService;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<FilterExpressionGroup> CreateExpressionGroupAsync(int ruleSetId)
        {
            return await _ruleService.CreateExpressionGroupAsync(ruleSetId, this) as FilterExpressionGroup;
        }

        public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
        {
            var expression = new FilterExpression();
            await base.ConvertRuleAsync(rule, expression);
            expression.Descriptor = ((RuleExpression)expression).Descriptor as FilterDescriptor;
            return expression;
        }

        public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        {
            var group = new FilterExpressionGroup(typeof(Customer))
            {
                Id = ruleSet.Id,
                LogicalOperator = ruleSet.LogicalOperator,
                IsSubGroup = ruleSet.IsSubGroup,
                Value = ruleSet.Id,
                RawValue = ruleSet.Id.ToString(),
                Provider = this
                // INFO: filter group does NOT access any descriptor
            };

            return group;
        }

        public async Task<IPagedList<Customer>> ProcessFilterAsync(
            int[] ruleSetIds,
            LogicalRuleOperator logicalOperator,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            Guard.NotNull(ruleSetIds, nameof(ruleSetIds));

            var filters = await ruleSetIds
                .SelectAwait(id => _ruleService.CreateExpressionGroupAsync(id, this))
                .Where(x => x != null)
                .Cast<FilterExpression>()
                .ToArrayAsync();

            return ProcessFilter(filters, logicalOperator, pageIndex, pageSize);
        }

        public IPagedList<Customer> ProcessFilter(
            FilterExpression[] filters,
            LogicalRuleOperator logicalOperator,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            Guard.NotNull(filters, nameof(filters));

            if (filters.Length == 0)
            {
                return Array.Empty<Customer>().ToPagedList(0, int.MaxValue);
            }

            var query = _db.Customers.AsNoTracking().Where(x => !x.IsSystemAccount);

            FilterExpressionGroup group = null;

            if (filters.Length == 1 && filters[0] is FilterExpressionGroup group2)
            {
                group = group2;
            }
            else
            {
                group = new FilterExpressionGroup(typeof(Customer)) { LogicalOperator = logicalOperator };
                group.AddExpressions(filters);
            }

            // Create lambda predicate
            var predicate = group.ToPredicate(query.Provider);

            // Apply predicate to query
            query = query
                .Where(predicate)
                .Cast<Customer>()
                .OrderByDescending(c => c.CreatedOnUtc);

            return query.ToPagedList(pageIndex, pageSize);
        }

        protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
        {
            var stores = _storeContext.GetAllStores()
                .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                .ToArray();

            var vatNumberStatus = ((VatNumberStatus[])Enum.GetValues(typeof(VatNumberStatus)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = _localizationService.GetLocalizedEnum(x) })
                .ToArray();

            var taxDisplayTypes = ((TaxDisplayType[])Enum.GetValues(typeof(TaxDisplayType)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = _localizationService.GetLocalizedEnum(x) })
                .ToArray();

            var shippingStatus = ((ShippingStatus[])Enum.GetValues(typeof(ShippingStatus)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = _localizationService.GetLocalizedEnum(x) })
                .ToArray();

            var paymentStatus = ((PaymentStatus[])Enum.GetValues(typeof(PaymentStatus)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = _localizationService.GetLocalizedEnum(x) })
                .ToArray();

            var descriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor<Customer, bool>(x => x.Active)
                {
                    Name = "Active",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Active"),
                    RuleType = RuleType.Boolean
                },
                new FilterDescriptor<Customer, string>(x => x.Salutation)
                {
                    Name = "Salutation",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Salutation"),
                    RuleType = RuleType.String
                },
                new FilterDescriptor<Customer, string>(x => x.Title)
                {
                    Name = "Title",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Title"),
                    RuleType = RuleType.String
                },
                new FilterDescriptor<Customer, string>(x => x.Company)
                {
                    Name = "Company",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Company"),
                    RuleType = RuleType.String
                },
                new FilterDescriptor<Customer, string>(x => x.Gender)
                {
                    Name = "Gender",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Gender"),
                    RuleType = RuleType.String
                },
                new FilterDescriptor<Customer, string>(x => x.CustomerNumber)
                {
                    Name = "CustomerNumber",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CustomerNumber"),
                    RuleType = RuleType.String
                },
                new AnyFilterDescriptor<Customer, CustomerRoleMapping, int>(x => x.CustomerRoleMappings, rm => rm.CustomerRoleId)
                {
                    Name = "IsInCustomerRole",
                    DisplayName = T("Admin.Rules.FilterDescriptor.IsInCustomerRole"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("CustomerRole") { Multiple = true }
                },
                new FilterDescriptor<Customer, bool>(x => x.IsTaxExempt)
                {
                    Name = "TaxExempt",
                    DisplayName = T("Admin.Rules.FilterDescriptor.TaxExempt"),
                    RuleType = RuleType.Boolean
                },
                new FilterDescriptor<Customer, int>(x => x.VatNumberStatusId)
                {
                    Name = "VatNumberStatus",
                    DisplayName = T("Admin.Rules.FilterDescriptor.VatNumberStatus"),
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(vatNumberStatus)
                },
                new FilterDescriptor<Customer, int>(x => x.TaxDisplayTypeId)
                {
                    Name = "TaxDisplayType",
                    DisplayName = T("Admin.Rules.FilterDescriptor.TaxDisplayType"),
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(taxDisplayTypes)
                },
                new FilterDescriptor<Customer, string>(x => x.TimeZoneId)
                {
                    Name = "TimeZone",
                    DisplayName = T("Admin.Rules.FilterDescriptor.TimeZone"),
                    RuleType = RuleType.String
                },
                new FilterDescriptor<Customer, string>(x => x.LastUserAgent)
                {
                    Name = "LastUserAgent",
                    DisplayName = T("Admin.Rules.FilterDescriptor.LastUserAgent"),
                    RuleType = RuleType.String
                },
                new FilterDescriptor<Customer, string>(x => x.LastUserDeviceType)
                {
                    Name = "LastDeviceFamily",
                    DisplayName = T("Admin.Rules.FilterDescriptor.LastDeviceFamily"),
                    RuleType = RuleType.StringArray,
                    SelectList = new LocalRuleValueSelectList(DeviceRule.GetDefaultOptions()) { Multiple = true }
                },
                new FilterDescriptor<Customer, int?>(x => x.BillingAddress != null ? x.BillingAddress.CountryId : 0)
                {
                    Name = "BillingCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BillingCountry"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
                },
                new FilterDescriptor<Customer, int?>(x => x.ShippingAddress != null ? x.ShippingAddress.CountryId : 0)
                {
                    Name = "ShippingCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingCountry"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
                },
                new FilterDescriptor<Customer, int>(x => x.ReturnRequests.Count)
                {
                    Name = "ReturnRequestCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ReturnRequestCount"),
                    RuleType = RuleType.Int
                },

                new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.LastActivityDateUtc, DateTime.UtcNow))
                {
                    Name = "LastActivityDays",
                    DisplayName = T("Admin.Rules.FilterDescriptor.LastActivityDays"),
                    RuleType = RuleType.NullableInt
                },
                new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.LastLoginDateUtc, DateTime.UtcNow))
                {
                    Name = "LastLoginDays",
                    DisplayName = T("Admin.Rules.FilterDescriptor.LastLoginDays"),
                    RuleType = RuleType.NullableInt
                },
                new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.LastForumVisit, DateTime.UtcNow))
                {
                    Name = "LastForumVisitDays",
                    DisplayName = T("Admin.Rules.FilterDescriptor.LastForumVisit"),
                    RuleType = RuleType.NullableInt
                },
                new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.CreatedOnUtc, DateTime.UtcNow))
                {
                    Name = "CreatedDays",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CreatedDays"),
                    RuleType = RuleType.NullableInt
                },
                new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.BirthDate, DateTime.UtcNow))
                {
                    Name = "BirthDateDays",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BirthDate"),
                    RuleType = RuleType.NullableInt
                },
                new TargetGroupFilterDescriptor(_ruleService, this)
                {
                    Name = "RuleSet",
                    DisplayName = T("Admin.Rules.FilterDescriptor.RuleSet"),
                    RuleType = RuleType.Int,
                    Operators = new[] { RuleOperator.IsEqualTo, RuleOperator.IsNotEqualTo },
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.TargetGroup)
                },

                new AnyFilterDescriptor<Customer, Order, int>(x => x.Orders, o => o.StoreId)
                {
                    Name = "OrderInStore",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OrderInStore"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    SelectList = new LocalRuleValueSelectList(stores) { Multiple = true }
                },
                new FilterDescriptor<Customer, int>(x => x.Orders.Count(o => o.OrderStatusId == 10 || o.OrderStatusId == 20))
                {
                    Name = "NewOrderCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.NewOrderCount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int
                },
                new FilterDescriptor<Customer, int>(x => x.Orders.Count(o => o.OrderStatusId == 30))
                {
                    Name = "CompletedOrderCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CompletedOrderCount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int
                },
                new FilterDescriptor<Customer, int>(x => x.Orders.Count(o => o.OrderStatusId == 40))
                {
                    Name = "CancelledOrderCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CancelledOrderCount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int
                },
                new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.Orders.Max(o => o.CreatedOnUtc), DateTime.UtcNow))
                {
                    Name = "LastOrderDateDays",
                    DisplayName = T("Admin.Rules.FilterDescriptor.LastOrderDateDays"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.NullableInt
                },
                new AnyFilterDescriptor<Customer, Order, decimal>(x => x.Orders, o => o.OrderTotal)
                {
                    Name = "OrderTotal",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OrderTotal"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Money
                },
                new AnyFilterDescriptor<Customer, Order, decimal>(x => x.Orders, o => o.OrderSubtotalInclTax)
                {
                    Name = "OrderSubtotalInclTax",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OrderSubtotalInclTax"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Money
                },
                new AnyFilterDescriptor<Customer, Order, decimal>(x => x.Orders, o => o.OrderSubtotalExclTax)
                {
                    Name = "OrderSubtotalExclTax",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OrderSubtotalExclTax"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Money
                },
                new AnyFilterDescriptor<Customer, Order, int>(x => x.Orders, o => o.ShippingStatusId)
                {
                    Name = "ShippingStatus",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingStatus"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(shippingStatus)
                },
                new AnyFilterDescriptor<Customer, Order, int>(x => x.Orders, o => o.PaymentStatusId)
                {
                    Name = "PaymentStatus",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PaymentStatus"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(paymentStatus)
                },
                new AnyFilterDescriptor<Customer, OrderItem, int>(x => x.Orders.SelectMany(o => o.OrderItems), oi => oi.ProductId)
                {
                    Name = "HasPurchasedProduct",
                    DisplayName = T("Admin.Rules.FilterDescriptor.HasPurchasedProduct"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Product") { Multiple = true }
                },
                new AllFilterDescriptor<Customer, OrderItem, int>(x => x.Orders.SelectMany(o => o.OrderItems), oi => oi.ProductId)
                {
                    Name = "HasPurchasedAllProducts",
                    DisplayName = T("Admin.Rules.FilterDescriptor.HasPurchasedAllProducts"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Product") { Multiple = true }
                },
                new AnyFilterDescriptor<Customer, Order, bool>(x => x.Orders, o => o.AcceptThirdPartyEmailHandOver)
                {
                    Name = "AcceptThirdPartyEmailHandOver",
                    DisplayName = T("Admin.Rules.FilterDescriptor.AcceptThirdPartyEmailHandOver"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Boolean
                },
                new AnyFilterDescriptor<Customer, Order, string>(x => x.Orders, o => o.CustomerCurrencyCode)
                {
                    Name = "CurrencyCode",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Currency"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.String,
                    SelectList = new RemoteRuleValueSelectList("Currency")
                },
                new AnyFilterDescriptor<Customer, Order, int>(x => x.Orders, o => o.CustomerLanguageId)
                {
                    Name = "OrderLanguage",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Language"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int,
                    SelectList = new RemoteRuleValueSelectList("Language")
                },
                new AnyFilterDescriptor<Customer, Order, string>(x => x.Orders, o => o.PaymentMethodSystemName)
                {
                    Name = "PaymentMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PaidBy"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.String,
                    SelectList = new RemoteRuleValueSelectList("PaymentMethod")
                },
                new AnyFilterDescriptor<Customer, Order, string>(x => x.Orders, o => o.ShippingMethod)
                {
                    Name = "ShippingMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingMethod"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.String,
                    SelectList = new RemoteRuleValueSelectList("ShippingMethod")
                },
                new AnyFilterDescriptor<Customer, Order, string>(x => x.Orders, o => o.ShippingRateComputationMethodSystemName)
                {
                    Name = "ShippingRateComputationMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingRateComputationMethod"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.String,
                    SelectList = new RemoteRuleValueSelectList("ShippingRateComputationMethod")
                }
            };

            descriptors
                .Where(x => x.RuleType == RuleType.Money)
                .Each(x => x.Metadata["postfix"] = _primaryCurrency.CurrencyCode);

            return Task.FromResult(descriptors.Cast<RuleDescriptor>());
        }
    }
}
