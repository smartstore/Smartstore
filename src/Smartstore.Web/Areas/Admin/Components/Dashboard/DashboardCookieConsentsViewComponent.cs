using System.Text.Json;
using Smartstore.Admin.Models.Customers;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components;

/// <summary>
/// Renders aggregate cookie consent data for existing customers, including guests.
/// </summary>
public class DashboardCookieConsentsViewComponent : SmartViewComponent
{
    /// <summary>
    /// Gets the data permission required to display this report.
    /// </summary>
    public const string RequiredPermission = Permissions.Dashboard.Widgets.Customers.CookieConsents;

    private static readonly (string Property, string Category, bool Required)[] _fields =
    [
        (nameof(ConsentCookie.AllowRequired), "Required", true),
        (nameof(ConsentCookie.AllowAnalytics), "Analytics", true),
        (nameof(ConsentCookie.AllowThirdParty), "ThirdParty", true),
        (nameof(ConsentCookie.AdUserDataConsent), "AdUserDataConsent", false),
        (nameof(ConsentCookie.AdPersonalizationConsent), "AdPersonalizationConsent", false)
    ];

    private readonly SmartDbContext _db;

    public DashboardCookieConsentsViewComponent(SmartDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Reads consent attributes in bounded batches and renders their aggregate counts.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
        {
            return Empty();
        }

        var model = await Services.Cache.GetAsync("admin:dashboard:cookieconsents", async ctx =>
        {
            ctx.ExpiresIn(TimeSpan.FromHours(24));

            var customerIds = _db.Customers
                .Where(x => !x.Deleted && !x.IsSystemAccount)
                .Select(x => x.Id);

            var pager = _db.GenericAttributes
                .AsNoTracking()
                .Where(x => x.KeyGroup == nameof(Customer)
                    && x.Key == SystemCustomerAttributeNames.CookieConsent && x.StoreId == 0
                    && customerIds.Contains(x.EntityId))
                .ToFastPager(500);

            var report = new DashboardCookieConsentModel
            {
                Categories = _fields.Select(field => new DashboardCookieConsentCategoryModel
                {
                    CategoryResourceKey = "CookieManager.Dialog." + field.Category + ".Heading"
                }).ToArray()
            };

            // TODO: (mh) WTF??!!!!!!!!!!!! TBD with MC.
            while ((await pager.ReadNextPageAsync(x => new { x.Id, x.Value }, x => x.Id)).Out(out var page))
            {
                foreach (var attribute in page)
                {
                    AddConsent(report, attribute.Value);
                }
            }

            return report;
        });

        return View(model);
    }

    private static void AddConsent(DashboardCookieConsentModel model, string json)
    {
        // TODO: (mh) WTF??!!!!!!!!!!!! TBD with MC.
        if (string.IsNullOrWhiteSpace(json))
        {
            model.SkippedConsents++;
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                model.SkippedConsents++;
                return;
            }

            var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                properties[property.Name] = property.Value;
            }

            if (!properties.TryGetValue(nameof(ConsentCookie.ConsentedOn), out var date)
                || date.ValueKind != JsonValueKind.String
                || !date.TryGetDateTime(out var consentedOn)
                || consentedOn == DateTime.MinValue)
            {
                model.SkippedConsents++;
                return;
            }

            var answers = new JsonValueKind[_fields.Length];
            for (var i = 0; i < _fields.Length; i++)
            {
                var exists = properties.TryGetValue(_fields[i].Property, out var answer);
                if ((!exists && _fields[i].Required)
                    || (exists && answer.ValueKind is not (JsonValueKind.True or JsonValueKind.False)))
                {
                    model.SkippedConsents++;
                    return;
                }

                answers[i] = answer.ValueKind;
            }

            model.TotalConsents++;
            for (var i = 0; i < answers.Length; i++)
            {
                if (answers[i] == JsonValueKind.True)
                {
                    model.Categories[i].AcceptedCount++;
                }
                else if (answers[i] == JsonValueKind.False)
                {
                    model.Categories[i].DeclinedCount++;
                }
            }
        }
        catch (JsonException)
        {
            model.SkippedConsents++;
        }
    }
}
