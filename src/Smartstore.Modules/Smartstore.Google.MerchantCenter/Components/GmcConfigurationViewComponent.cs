using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Google.MerchantCenter.Models;
using Smartstore.Web.Components;

namespace Smartstore.Google.MerchantCenter.Components;

/// <summary>
/// Component to render profile configuration of GMC feed.
/// </summary>
public class GmcConfigurationViewComponent : SmartViewComponent
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;

    public GmcConfigurationViewComponent(SmartDbContext db, IWorkContext workContext)
    {
        _db = db;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(object data)
    {
        var model = data as ProfileConfigurationModel;

        var counts = await _db.Products
            .Select(_ => new
            {
                NumProducts = _db.Products.Where(x => !x.IsSystemProduct).Count(),
                NumGoogleProducts = _db.GoogleProducts().Count()
            })
            .FirstAsync();

        ViewBag.DefaultValueSettingsNote = T("Plugins.Feed.Froogle.DefaultValueSettingsNote",
            counts.NumGoogleProducts.ToString("N0"), counts.NumProducts.ToString("N0"));

        ViewBag.LanguageSeoCode = _workContext.WorkingLanguage.UniqueSeoCode.EmptyNull().ToLower();
        ViewBag.AvailableCategories = model.DefaultGoogleCategory.HasValue()
            ? new List<SelectListItem> { new() { Text = model.DefaultGoogleCategory, Value = model.DefaultGoogleCategory, Selected = true } }
            : null;

        return View(model);
    }
}
