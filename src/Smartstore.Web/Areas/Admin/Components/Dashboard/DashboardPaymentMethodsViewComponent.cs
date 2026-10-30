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
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
        {
            return Empty();
        }

        var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync(
            nameof(Customer), Services.WorkContext.CurrentCustomer.Id);
        var paymentMethodTotals = await _db.Orders
            .AsNoTracking()
            .ApplyCustomerFilter(authorizedStoreIds)
            .Where(x => !x.Deleted && !string.IsNullOrEmpty(x.PaymentMethodSystemName))
            .GroupBy(x => x.PaymentMethodSystemName)
            .Select(group => new
            {
                SystemName = group.Key,
                OrderCount = group.Count(),
                TotalAmount = group.Sum(x => x.OrderTotal)
            })
            .OrderBy(x => x.SystemName)
            .ToListAsync(HttpContext.RequestAborted);

        var topByQuantity = paymentMethodTotals.OrderByDescending(x => x.OrderCount).Take(10);
        var topByAmount = paymentMethodTotals.OrderByDescending(x => x.TotalAmount).Take(10);
        var displayedSystemNames = topByQuantity.Concat(topByAmount)
            .Select(x => x.SystemName)
            .ToHashSet(StringComparer.Ordinal);

        var model = new List<DashboardPaymentMethodModel>(displayedSystemNames.Count);
        foreach (var item in paymentMethodTotals.Where(x => displayedSystemNames.Contains(x.SystemName)))
        {
            model.Add(new DashboardPaymentMethodModel
            {
                Name = await ResolvePaymentMethodNameAsync(item.SystemName),
                OrderCount = item.OrderCount,
                TotalAmount = Services.CurrencyService.CreateMoney(item.TotalAmount, Services.CurrencyService.PrimaryCurrency)
            });
        }

        return View(model);
    }

    private async Task<string> ResolvePaymentMethodNameAsync(string systemName)
    {
        // Historical orders may reference disabled or uninstalled payment providers.
        var provider = await _paymentService.LoadPaymentProviderBySystemNameAsync(systemName);
        if (provider == null)
        {
            return systemName;
        }

        string name = _moduleManager.GetLocalizedFriendlyName(provider.Metadata);
        if (name.IsEmpty())
        {
            return systemName;
        }

        return name;
    }
}
