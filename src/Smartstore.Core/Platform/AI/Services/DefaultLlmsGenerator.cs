using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.AI;

public class DefaultLlmsGenerator : ILlmsGenerator
{
    private readonly SmartDbContext _db;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly ICategoryService _categoryService;
    private readonly Lazy<IProviderManager> _providerManager;
    private readonly Lazy<ModuleManager> _moduleManager;
    private readonly Lazy<IUrlHelper> _urlHelper;
    private readonly CompanyInformationSettings _companyInfoSettings;
    private readonly ContactDataSettings _contactSettings;
    private readonly HomePageSettings _homepageSettings;
    private readonly SocialSettings _socialSettings;
    private readonly TaxSettings _taxSettings;
    private readonly PaymentSettings _paymentSettings;
    private readonly CatalogSettings _catalogSettings;

    protected IUrlHelper Url => _urlHelper.Value;

    public DefaultLlmsGenerator(
        SmartDbContext db,
        IStoreContext storeContext,
        IWorkContext workContext,
        ICategoryService categoryService,
        Lazy<IProviderManager> providerManager,
        Lazy<ModuleManager> moduleManager,
        Lazy<IUrlHelper> urlHelper,
        CompanyInformationSettings companyInfoSettings,
        ContactDataSettings contactSettings,
        HomePageSettings homepageSettings,
        SocialSettings socialSettings,
        TaxSettings taxSettings,
        PaymentSettings paymentSettings,
        CatalogSettings catalogSettings)
    {
        _db = db;
        _storeContext = storeContext;
        _workContext = workContext;
        _categoryService = categoryService;
        _providerManager = providerManager;
        _moduleManager = moduleManager;
        _urlHelper = urlHelper;
        _companyInfoSettings = companyInfoSettings;
        _contactSettings = contactSettings;
        _homepageSettings = homepageSettings;
        _socialSettings = socialSettings;
        _taxSettings = taxSettings;
        _paymentSettings = paymentSettings;
        _catalogSettings = catalogSettings;
    }

    public async Task GenerateLlms(TextWriter writer, HttpRequest httpRequest)
    {
        Guard.NotNull(writer);
        Guard.NotNull(httpRequest);

        await GenerateMetadata(writer, httpRequest);
        await GenerateMainCategories(writer, httpRequest);
        await GenerateLinks(writer, httpRequest);
    }

    protected virtual async Task GenerateMetadata(TextWriter writer, HttpRequest httpRequest)
    {
        var company = _companyInfoSettings;
        var store = _storeContext.CurrentStore;
        var baseUrl = store.GetBaseUrl();

        writer.WriteLine($"# {store.Name} - LLM Directory");
        writer.WriteLine();

        var languages = await _db.Languages
            .AsNoTracking()
            .ApplyStandardFilter(false, store.Id)
            .ToListAsync();

        var providers = _providerManager.Value.GetAllProviders<IPaymentMethod>()
            .Where(x => x.IsPaymentProviderEnabled(_paymentSettings));

        writer.WriteLine("## Metadata");

        WriteLine(writer, "Base URL", baseUrl);
        WriteLine(writer, "Title", _homepageSettings.MetaTitle);
        WriteLine(writer, "Description", _homepageSettings.MetaDescription);
        WriteLine(writer, "Operator", company.CompanyName);
        WriteLine(writer, "Legal Representatives", company.CompanyManagementDescription);

        // Address
        {
            var addressParts = new List<string>();

            var streetLine = string.Join(" ", new[] { company.Street, company.Street2 }.Where(x => x.HasValue()));
            var cityLine = string.Join(" ", new[] { company.ZipCode, company.City }.Where(x => x.HasValue()));

            string countryName = null;
            if (company.CountryId > 0)
            {
                var country = await _db.Countries.FindByIdAsync(company.CountryId, false);
                countryName = country?.GetLocalized(x => x.Name);
            }

            if (streetLine.HasValue()) addressParts.Add(streetLine);
            if (cityLine.HasValue()) addressParts.Add(cityLine);
            if (company.StateName.HasValue()) addressParts.Add(company.StateName);
            if (countryName.HasValue()) addressParts.Add(countryName);

            if (addressParts.Count > 0)
            {
                WriteLine(writer, "Address", string.Join(", ", addressParts));
            }
        }

        WriteLine(writer, "Registered at", company.CommercialRegister);
        WriteLine(writer, "VAT ID", company.VatId);
        WriteLine(writer, "Support Email", _contactSettings.SupportEmailAddress);
        WriteLine(writer, "Support Phone", _contactSettings.HotlineTelephoneNumber);

        if (_taxSettings.TaxDisplayType == TaxDisplayType.IncludingTax)
        {
            WriteLine(writer, "Target Audience", "B2C");
            WriteLine(writer, "Price Display", "Gross (Prices include VAT)");
        }
        else
        {
            WriteLine(writer, "Target Audience", "B2B");
            WriteLine(writer, "Price Display", "Net (Prices exclude VAT)");
        }

        WriteLine(writer, "Currency", _workContext.WorkingCurrency?.CurrencyCode);
        WriteLine(writer, "Available Languages", string.Join(", ", languages.Select(x => x.UniqueSeoCode)));
        WriteLine(writer, "Available Payment Methods", string.Join(", ", providers.Select(x => _moduleManager.Value.GetLocalizedFriendlyName(x.Metadata))));
        writer.WriteLine();
    }

    protected virtual async Task GenerateMainCategories(TextWriter writer, HttpRequest httpRequest)
    {
        var store = _storeContext.CurrentStore;
        var baseUrl = store.GetBaseUrl();

        var categoryTree = await _categoryService.GetCategoryTreeAsync(storeId: store.Id);
        var rootChildren = categoryTree?.Children;

        if (rootChildren != null && rootChildren.Count > 0)
        {
            var languageId = _workContext.WorkingLanguage?.Id;

            writer.WriteLine("## Main product categories");
            foreach (var node in rootChildren)
            {
                var category = node.Value;
                var name = languageId.HasValue
                    ? category.GetLocalized(x => x.Name, languageId.Value)
                    : category.Name;
                var url = Url.RouteUrl("Category", new { SeName = category.GetActiveSlug() }, httpRequest.Scheme);

                WriteLink(writer, name, url.HasValue() ? new Uri(new Uri(baseUrl), url).ToString() : null);
            }

            writer.WriteLine();
        }
    }

    protected virtual async Task GenerateLinks(TextWriter writer, HttpRequest httpRequest)
    {
        var store = _storeContext.CurrentStore;
        var baseUrl = store.GetBaseUrl();
        var scheme = httpRequest.Scheme;
        var social = _socialSettings;

        writer.WriteLine("## Discovery Links");
        WriteLink(writer, "Sitemap", baseUrl + "sitemap.xml");

        if (_catalogSettings.RecentlyAddedProductsEnabled && _catalogSettings.RecentlyAddedProductsNumber > 0)
        {
            WriteLink(writer, "Recently added products", Url.RouteUrl("RecentlyAddedProductsRSS", null, scheme));
        }

        WriteLink(writer, "Contact & Support", Url.RouteUrl("ContactUs", null, scheme));
        WriteLink(writer, "Brands", Url.RouteUrl("ManufacturerList", null, scheme));
        WriteLink(writer, "Shipping & Delivery Info", new Uri(new Uri(baseUrl), await Url.TopicAsync("ShippingInfo")).ToString());
        WriteLink(writer, "Privacy Policy", new Uri(new Uri(baseUrl), await Url.TopicAsync("PrivacyInfo")).ToString());

        WriteLink(writer, "Facebook", social.FacebookLink);
        WriteLink(writer, "Twitter", social.TwitterLink);
        WriteLink(writer, "Instagram", social.InstagramLink);
        WriteLink(writer, "TikTok", social.TikTokLink);
        WriteLink(writer, "YouTube", social.YoutubeLink);
        WriteLink(writer, "Vimeo", social.VimeoLink);
        WriteLink(writer, "Pinterest", social.PinterestLink);
        WriteLink(writer, "Snapchat", social.SnapchatLink);
        WriteLink(writer, "Flickr", social.FlickrLink);
        WriteLink(writer, "LinkedIn", social.LinkedInLink);
        WriteLink(writer, "Xing", social.XingLink);
        WriteLink(writer, "Tumblr", social.TumblrLink);
        WriteLink(writer, "Ello", social.ElloLink);
        WriteLink(writer, "Behance", social.BehanceLink);
    }

    protected static void WriteLine(TextWriter writer, string prefix, string value)
    {
        if (value.HasValue())
        {
            writer.WriteLine($"- {prefix}: {value}");
        }
    }

    protected static void WriteLink(TextWriter writer, string name, string url)
    {
        if (url.HasValue() && url != "#")
        {
            writer.WriteLine($"- [{name}]({url})");
        }
    }
}