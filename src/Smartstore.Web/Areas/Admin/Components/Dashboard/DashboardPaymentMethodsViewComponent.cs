using Smartstore.Admin.Models.Dashboard;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Components;

/// <summary>
/// Renders payment method rankings by order count and order total for orders accessible to the current customer.
/// </summary>
public class DashboardPaymentMethodsViewComponent : SmartViewComponent
{
    /// <summary>
    /// Gets the data permission required to display this report.
    /// </summary>
    public const string RequiredPermission = Permissions.Dashboard.Widgets.Sales.PaymentMethods;

    private readonly SmartDbContext _db;
    private readonly IPaymentService _paymentService;
    private readonly ModuleManager _moduleManager;

    public DashboardPaymentMethodsViewComponent(
        SmartDbContext db,
        IPaymentService paymentService,
        ModuleManager moduleManager)
    {
        _db = db;
        _paymentService = paymentService;
        _moduleManager = moduleManager;
    }

    /// <summary>
    /// Builds the payment method ranking across all order dates and statuses.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(IReadOnlyDictionary<string, decimal> feeValues = null, bool configure = false, int count = 10, bool refresh = false,
        IReadOnlyDictionary<string, bool> feeIsPercentage = null)
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
        {
            return Empty();
        }

        count = Math.Clamp(count, 1, 50);
        var providers = (await _paymentService.LoadAllPaymentProvidersAsync(onlyEnabled: true)).ToArray();
        var names = providers.ToDictionary(x => x.Metadata.SystemName,
            x => _moduleManager.GetLocalizedFriendlyName(x.Metadata), StringComparer.OrdinalIgnoreCase);

        if (configure)
        {
            var currencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            var configuration = new DashboardPaymentMethodsConfigurationModel
            {
                Count = count,
                CurrencyCode = currencyCode
            };
            foreach (var provider in providers.OrderBy(x => names[x.Metadata.SystemName], StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.Metadata.SystemName, StringComparer.Ordinal))
            {
                var systemName = provider.Metadata.SystemName;
                configuration.PaymentMethods.Add(new DashboardPaymentMethodFeeModel
                {
                    SystemName = systemName,
                    Name = names[systemName],
                    FeeValue = feeValues?.GetValueOrDefault(systemName) ?? 0,
                    FeeIsPercentage = feeIsPercentage?.GetValueOrDefault(systemName, true) ?? true
                });
            }

            return View("Configure", configuration);
        }

        var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync(nameof(Customer), Services.WorkContext.CurrentCustomer.Id);
        var storeKey = authorizedStoreIds.Length == 0 ? "all" : string.Join("-", authorizedStoreIds.Order());
        var cacheKey = $"admin:dashboard:paymentmethods:v2:{storeKey}";
        if (refresh)
        {
            await Services.Cache.RemoveAsync(cacheKey);
        }

        var totals = await Services.Cache.GetAsync(cacheKey, async ctx =>
        {
            ctx.ExpiresIn(TimeSpan.FromMinutes(5));

            return await _db.Orders
                .AsNoTracking()
                .ApplyCustomerFilter(authorizedStoreIds)
                .Where(x => !x.Deleted && !string.IsNullOrEmpty(x.PaymentMethodSystemName))
                .GroupBy(x => x.PaymentMethodSystemName)
                .Select(group => new DashboardPaymentMethodTotals
                {
                    SystemName = group.Key,
                    OrderCount = group.Count(),
                    TotalAmount = group.Sum(x => x.OrderTotal),
                    PaidAmount = group.Sum(x => x.PaymentStatusId == (int)PaymentStatus.Paid ? x.OrderTotal : 0),
                    PaidOrderCount = group.Count(x => x.PaymentStatusId == (int)PaymentStatus.Paid)
                })
                .ToListAsync(HttpContext.RequestAborted);
        });
        var paymentMethodTotals = totals.Where(x => names.ContainsKey(x.SystemName))
            .OrderBy(x => x.SystemName, StringComparer.Ordinal).ToArray();
        var topByQuantity = paymentMethodTotals.OrderByDescending(x => x.OrderCount).Take(count);
        var topByAmount = paymentMethodTotals.OrderByDescending(x => x.TotalAmount).Take(count);
        var displayedSystemNames = topByQuantity.Concat(topByAmount)
            .Select(x => x.SystemName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var model = new List<DashboardPaymentMethodModel>(displayedSystemNames.Count);
        foreach (var item in paymentMethodTotals.Where(x => displayedSystemNames.Contains(x.SystemName)))
        {
            decimal? fee = feeValues != null && feeValues.TryGetValue(item.SystemName, out var value) ? value : null;
            var percentage = feeIsPercentage?.GetValueOrDefault(item.SystemName, true) ?? true;

            model.Add(new DashboardPaymentMethodModel
            {
                Name = names[item.SystemName],
                OrderCount = item.OrderCount,
                TotalAmount = Services.CurrencyService.CreateMoney(item.TotalAmount, Services.CurrencyService.PrimaryCurrency),
                FeeValue = fee,
                FeeIsPercentage = percentage,
                EstimatedFees = Services.CurrencyService.CreateMoney(percentage
                    ? item.PaidAmount * fee.GetValueOrDefault() / 100m
                    : item.PaidOrderCount * fee.GetValueOrDefault(),
                    Services.CurrencyService.PrimaryCurrency)
            });
        }

        return View(new DashboardPaymentMethodsModel { Count = count, PaymentMethods = model });
    }
}
