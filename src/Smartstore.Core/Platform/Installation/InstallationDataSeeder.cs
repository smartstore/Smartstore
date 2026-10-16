using System.Xml;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Installation.Seeding;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Theming;
using Smartstore.Data.Hooks;
using Smartstore.IO;

namespace Smartstore.Core.Installation;

public partial class InstallationDataSeeder : DataSeeder<SmartDbContext>
{
    private readonly DbMigrator<SmartDbContext> _migrator;
    private readonly IMessageTemplateService _messageTemplateService;
    private readonly SeedDataConfiguration _config;
    private readonly InvariantSeedData _data;

    private readonly SeedReferenceResolver _seedResolver = new();
    private SeedDataSource _seedSource;
    private SeedEntityFactory _seedFactory;

    private IXmlResourceManager _xmlResourceManager;
    private int _defaultStoreId;

    public InstallationDataSeeder(
        IApplicationContext appContext,
        DbMigrator<SmartDbContext> migrator,
        IMessageTemplateService messageTemplateService,
        SeedDataConfiguration configuration,
        ILogger logger)
        : base(appContext, logger)
    {
        Guard.NotNull(migrator);
        Guard.NotNull(configuration);
        Guard.NotNull(configuration.Language);
        Guard.NotNull(configuration.Data);

        _migrator = migrator;
        _messageTemplateService = messageTemplateService;
        _config = configuration;
        _data = configuration.Data;
    }

    protected IXmlResourceManager XmlResourceManager
    {
        get
        {
            // Some dependencies are "null" because they are not needed during installation.
            _xmlResourceManager ??= new XmlResourceManager(
                Context,
                NullRequestCache.Instance,
                NullCache.Instance,
                null,
                null,
                Logger,
                null,
                null,
                ApplicationContext,
                null);

            return _xmlResourceManager;
        }
    }

    #region IDataSeeder

    protected override async Task SeedCoreAsync()
    {
        Context.ChangeTracker.AutoDetectChangesEnabled = false;
        Context.MinHookImportance = HookImportance.Essential;

        _config.ProgressMessageCallback("Progress.CreatingRequiredData");

        // Special mandatory (non-visible) settings
        await Context.MigrateSettingsAsync(x =>
        {
            x.Add("Media.Storage.Provider", _config.StoreMediaInDB ? DatabaseMediaStorageProvider.SystemName : FileSystemMediaStorageProvider.SystemName);
        });

        // Initialize seeder AFTER we added "Media.Storage.Provider" key,
        // because SampleMediaUtility depends on it.
        _data.Initialize(Context, _config, EngineContext.Current.Application);

        // File-based seed data (App_Data/Seed). Resource references become available after the locale resources.
        _seedSource = new SeedDataSource(
            new LocalFileSystem(PathUtility.Join(ApplicationContext.AppDataRoot.Root, SeedDataSource.AppDataFolder)),
            _config.Language.LanguageCulture);
        _seedFactory = new SeedEntityFactory(_seedResolver);

        Populate("PopulatePictures", () => _data.Pictures(_config.SeedSampleData));
        await PopulateFromSeedAsync("PopulateCurrencies", InstallationSeedDescriptors.Currencies());
        await PopulateAsync("PopulateStores", PopulateStores);
        await PopulateAsync("InstallLanguages", () => PopulateLanguage(_config.Language));

        // Locale resources come right after the language, so that seed data can reference resources.
        await PopulateAsync("PopulateLocaleResources", async () => await PopulateLocaleResources(_config.Language));
        _seedFactory = new SeedEntityFactory(_seedResolver, await SeedResources.TryLoadAsync(Context, _config.Language, CancelToken));

        await PopulateFromSeedAsync("PopulateMeasureDimensions", InstallationSeedDescriptors.MeasureDimensions());
        await PopulateFromSeedAsync("PopulateMeasureWeights", InstallationSeedDescriptors.MeasureWeights());
        await PopulateAsync("PopulateTaxCategories", PopulateTaxCategories);
        await PopulateFromSeedAsync("PopulateCountriesAndStates", InstallationSeedDescriptors.Countries());
        await PopulateFromSeedAsync("PopulateShippingMethods", InstallationSeedDescriptors.ShippingMethods(_config.SeedSampleData));
        await PopulateFromSeedAsync("PopulateDeliveryTimes", InstallationSeedDescriptors.DeliveryTimes());
        await PopulateFromSeedAsync("PopulateQuantityUnits", InstallationSeedDescriptors.QuantityUnits());
        await PopulateAsync("PopulateCustomersAndUsers", async () => await PopulateCustomersAndUsers(_config.DefaultUserName, _config.DefaultUserPassword));
        await PopulateFromSeedAsync("PopulateEmailAccounts", InstallationSeedDescriptors.EmailAccounts());
        await PopulateAsync("PopulateMessageTemplates", PopulateMessageTemplates);
        await PopulateAsync("PopulateTopics", PopulateTopics);
        await PopulateFromSeedAsync("PopulatePriceLabels", InstallationSeedDescriptors.PriceLabels());
        await PopulateAsync("PopulateSettings", PopulateSettings);
        await PopulateFromSeedAsync("PopulateActivityLogTypes", InstallationSeedDescriptors.ActivityLogTypes());
        await PopulateAsync("PopulateCustomersAndUsers", async () => await HashDefaultCustomerPassword(_config.DefaultUserName, _config.DefaultUserPassword));
        await PopulateFromSeedAsync("PopulateProductTemplates", InstallationSeedDescriptors.ProductTemplates());
        await PopulateFromSeedAsync("PopulateCategoryTemplates", InstallationSeedDescriptors.CategoryTemplates());
        await PopulateFromSeedAsync("PopulateManufacturerTemplates", InstallationSeedDescriptors.ManufacturerTemplates());
        await PopulateFromSeedAsync("PopulateScheduleTasks", InstallationSeedDescriptors.TaskDescriptors());
        await PopulateAsync("PopulateMenus", _data.Menus());

        if (_config.SeedSampleData)
        {
            Logger.Info("Seeding sample data");

            _config.ProgressMessageCallback("Progress.CreatingSampleData");

            await PopulateAsync("PopulateSpecificationAttributes", _data.SpecificationAttributes());
            await PopulateAsync("PopulateProductAttributes", _data.ProductAttributes());
            await PopulateAsync("PopulateProductAttributeOptionsSets", _data.ProductAttributeOptionsSets());
            await PopulateAsync("PopulateProductAttributeOptions", _data.ProductAttributeOptions());
            await PopulateAsync("PopulateCampaigns", _data.Campaigns());
            await PopulateAsync("PopulateRuleSets", _data.RuleSets());
            await PopulateAsync("PopulateDiscounts", _data.Discounts());
            await PopulateAsync("PopulateCategories", PopulateCategories);
            await PopulateAsync("PopulateManufacturers", PopulateManufacturers);
            await PopulateAsync("PopulateProducts", PopulateProducts);
            await PopulateAsync("PopulateProductBundleItems", _data.ProductBundleItems());
            await PopulateAsync("PopulateProductVariantAttributes", _data.ProductVariantAttributes());
            await PopulateAsync("ProductVariantAttributeCombinations", _data.ProductVariantAttributeCombinations());
            await PopulateAsync("PopulateProductTags", _data.ProductTags());
            Populate("FinalizeSamples", _data.FinalizeSamples);
        }

        // Perf
        Context.DetachEntities<BaseEntity>();
    }

    #endregion

    #region Populate

    private List<T> CreateFromSeed<T>(SeedEntityDescriptor<T> descriptor) where T : BaseEntity, new()
        => _seedFactory.CreateAll<T>(_seedSource.Load(descriptor));

    private Task PopulateFromSeedAsync<T>(string stage, SeedEntityDescriptor<T> descriptor) where T : BaseEntity, new()
        => PopulateAsync(stage, () => SaveRangeAsync(CreateFromSeed(descriptor)));

    private async Task PopulateStores()
    {
        var stores = _data.Stores();
        await SaveRangeAsync(stores);
        _defaultStoreId = stores.First().Id;
    }

    private async Task PopulateTaxCategories()
    {
        // By reference: entity equality and hash code depend on the ID, which changes on save.
        var rates = new Dictionary<TaxCategory, decimal>(ReferenceEqualityComparer.Instance);
        var taxCategories = CreateFromSeed(InstallationSeedDescriptors.TaxCategories(rates));
        await SaveRangeAsync(taxCategories);

        // Add tax rates to fixed rate provider
        foreach (var taxCategory in taxCategories.OrderBy(x => x.DisplayOrder))
        {
            Context.Settings.Add(new Setting
            {
                Name = string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", taxCategory.Id).ToLowerInvariant(),
                Value = rates.GetValueOrDefault(taxCategory).Convert<string>(),
            });
        }

        await Context.SaveChangesAsync();
    }

    private Task PopulateLanguage(Language primaryLanguage)
    {
        primaryLanguage.Published = true;
        return SaveAsync(primaryLanguage);
    }

    private async Task PopulateLocaleResources(Language language)
    {
        var appDataRoot = EngineContext.Current.Application.AppDataRoot;

        var locDir = appDataRoot.GetDirectory(PathUtility.Join(SeedDataSource.AppDataFolder, SeedDataSource.CulturesFolder, language.LanguageCulture, SeedDataSource.ResourcesFolder));
        if (!locDir.Exists)
        {
            Logger.Warn($"No locale resources found in '{locDir.SubPath}'.");
            return;
        }

        var xmlResourceManager = XmlResourceManager;

        // Perf
        Context.DetachEntities<BaseEntity>();

        // Save resources
        foreach (var file in locDir.EnumerateFiles("*.smres.xml"))
        {
            var doc = new XmlDocument();
            doc.Load(file.PhysicalPath);

            doc = xmlResourceManager.FlattenResourceFile(doc);

            // Now we have a parsed XML file (the same structure as exported language packs)
            // Let's save resources
            await xmlResourceManager.ImportResourcesFromXmlAsync(language, doc);

            // No need to call SaveChanges() here, as the above call made it
            // already without AutoDetectChanges(), so it's fast.

            // Perf
            Context.DetachEntities<LocaleStringResource>();
        }

        await SeedPendingLocaleResources(locDir);
    }

    private async Task SeedPendingLocaleResources(IDirectory locDir)
    {
        var fs = locDir.FileSystem;
        var headFile = fs.GetFile(PathUtility.Join(locDir.SubPath, "head.txt"));

        if (!headFile.Exists)
        {
            return;
        }

        var resHead = headFile.ReadAllText().Trim();
        if (resHead.HasValue())
        {
            if (long.TryParse(resHead, out var version))
            {
                await _migrator.SeedPendingLocaleResourcesAsync(version);
            }
            else
            {
                throw new ArgumentException("Wrong head value (head.txt) for seeding pending locale resources."
                    + $" Must be a migration version number of type 'long'. See {nameof(MigrationHistory.Version)}.");
            }
        }
    }

    private async Task PopulateCustomersAndUsers(string defaultUserEmail, string defaultUserPassword)
    {
        var customerRoles = CreateFromSeed(InstallationSeedDescriptors.CustomerRoles(_config.SeedSampleData));
        await SaveRangeAsync(customerRoles);

        //admin user
        var adminUser = new Customer
        {
            CustomerGuid = Guid.NewGuid(),
            Email = defaultUserEmail,
            Username = defaultUserEmail,
            Password = defaultUserPassword,
            PasswordFormat = PasswordFormat.Clear,
            PasswordSalt = "",
            Active = true,
            CreatedOnUtc = DateTime.UtcNow,
            LastActivityDateUtc = DateTime.UtcNow,
        };

        var adminAddress = _data.AdminAddress();

        adminUser.Addresses.Add(adminAddress);
        adminUser.BillingAddress = adminAddress;
        adminUser.ShippingAddress = adminAddress;

        var adminRole = customerRoles.First(x => x.SystemName == SystemCustomerRoleNames.Administrators);
        var forumRole = customerRoles.First(x => x.SystemName == SystemCustomerRoleNames.ForumModerators);
        var registeredRole = customerRoles.First(x => x.SystemName == SystemCustomerRoleNames.Registered);

        adminUser.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = adminUser.Id, CustomerRoleId = adminRole.Id });
        adminUser.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = adminUser.Id, CustomerRoleId = forumRole.Id });
        adminUser.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = adminUser.Id, CustomerRoleId = registeredRole.Id });
        await SaveAsync(adminUser);

        // Set default customer name
        var firstAddress = adminUser.Addresses.FirstOrDefault();
        Context.GenericAttributes.AddRange(new[]
        {
            new GenericAttribute
            {
                EntityId = adminUser.Id,
                Key = "FirstName",
                KeyGroup = "Customer",
                Value = firstAddress.FirstName
            },
            new GenericAttribute
            {
                EntityId = adminUser.Id,
                Key = "LastName",
                KeyGroup = "Customer",
                Value = firstAddress.LastName
            }
        });

        await Context.SaveChangesAsync();

        // Built-in user for search engines (crawlers)
        var guestRole = customerRoles.FirstOrDefault(x => x.SystemName == SystemCustomerRoleNames.Guests);

        var customer = _data.SearchEngineUser();
        customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
        await SaveAsync(customer);

        // Built-in user for background tasks
        customer = _data.BackgroundTaskUser();
        customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
        await SaveAsync(customer);

        // Built-in user for the PDF converter
        customer = _data.PdfConverterUser();
        customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
        await SaveAsync(customer);

        // Built-in user for the webhook client
        customer = _data.WebhookClientUser();
        customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
        await SaveAsync(customer);
    }

    private async Task HashDefaultCustomerPassword(string defaultUserEmail, string defaultUserPassword)
    {
        var encryptor = new Encryptor(new SecuritySettings());
        var saltKey = encryptor.CreateSaltKey(5);
        var adminUser = await Context.Customers.FirstOrDefaultAsync(x => x.Email == _config.DefaultUserName);

        adminUser.PasswordSalt = saltKey;
        adminUser.PasswordFormat = PasswordFormat.Hashed;
        adminUser.Password = encryptor.CreatePasswordHash(defaultUserPassword, saltKey, new CustomerSettings().HashedPasswordFormat);

        await Context.SaveChangesAsync();
    }

    private async Task PopulateSettings()
    {
        var settings = _data.Settings();
        foreach (var setting in settings)
        {
            Type settingType = setting.GetType();
            int storeId = settingType.Equals(typeof(ThemeSettings)) ? _defaultStoreId : 0;
            await SettingFactory.SaveSettingsAsync(Context, setting, storeId: storeId);
        }
    }

    private async Task PopulateMessageTemplates()
    {
        await _messageTemplateService.ImportAllTemplatesAsync(_config.Language.LanguageCulture);
    }

    private async Task PopulateCategories()
    {
        var categoriesFirstLevel = _data.CategoriesFirstLevel();
        await SaveRangeAsync(categoriesFirstLevel);
        await PopulateUrlRecordsFor(categoriesFirstLevel);

        var categoriesSecondLevel = _data.CategoriesSecondLevel();
        await SaveRangeAsync(categoriesSecondLevel);
        await PopulateUrlRecordsFor(categoriesSecondLevel);
    }

    private async Task PopulateManufacturers()
    {
        var manufacturers = _data.Manufacturers();
        await SaveRangeAsync(manufacturers);
        await PopulateUrlRecordsFor(manufacturers);
    }

    private async Task PopulateProducts()
    {
        var products = _data.Products();
        await SaveRangeAsync(products);

        _data.AddDownloads(products);

        await PopulateUrlRecordsFor(products);

        _data.AssignGroupedProducts(products);

        // Fix MainPictureId
        await ProductPictureHelper.FixProductMainPictureIds(Context);
    }

    private async Task PopulateTopics()
    {
        var topics = _data.Topics();
        await SaveRangeAsync(topics);
        await PopulateUrlRecordsFor(topics);
    }

    private Task PopulateUrlRecordsFor<T>(IEnumerable<T> entities) where T : BaseEntity, ISlugSupported, new()
        => PopulateUrlRecordsFor(entities, entity => _data.CreateUrlRecordFor(entity));

    #endregion
}