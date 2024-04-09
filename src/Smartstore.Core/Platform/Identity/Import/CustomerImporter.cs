using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.DataExchange.Import
{
    public class CustomerImporter : EntityImporterBase
    {
        const string CargoDataKey = "CustomerImporter.CargoData";

        private readonly IMediaImporter _mediaImporter;
        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;
        private readonly PrivacySettings _privacySettings;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CustomerImporter(
            ICommonServices services,
            IStoreMappingService storeMappingService,
            IUrlService urlService,
            IMediaImporter mediaImporter,
            SeoSettings seoSettings,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            PrivacySettings privacySettings,
            DateTimeSettings dateTimeSettings,
            ShoppingCartSettings shoppingCartSettings)
            : base(services, storeMappingService, urlService, seoSettings)
        {
            _mediaImporter = mediaImporter;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
            _privacySettings = privacySettings;
            _dateTimeSettings = dateTimeSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public static string[] SupportedKeyFields => new[]
        {
            nameof(Customer.Id),
            nameof(Customer.CustomerGuid),
            nameof(Customer.Email),
            nameof(Customer.Username)
        };

        public static string[] DefaultKeyFields => new[]
        {
            nameof(Customer.Email),
            nameof(Customer.CustomerGuid)
        };

        protected override async Task ProcessBatchAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            var cargo = await GetCargoData(context);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<Customer>();

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows);

                // ===========================================================================
                // Process customers.
                // ===========================================================================
                try
                {
                    await ProcessCustomersAsync(context, cargo, scope, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessCustomersAsync));
                }

                // Reduce batch to saved (valid) records.
                // No need to perform import operations on errored records.
                batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

                // Update result object.
                context.Result.NewRecords += batch.Count(x => x.IsNew && !x.IsTransient);
                context.Result.ModifiedRecords += batch.Count(x => !x.IsNew && !x.IsTransient);

                // ===========================================================================
                // Process customer roles.
                // ===========================================================================
                if (segmenter.HasColumn("CustomerRoleSystemNames"))
                {
                    try
                    {
                        await ProcessCustomerRolesAsync(context, cargo, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessCustomerRolesAsync));
                    }
                }

                // ===========================================================================
                // Process generic attributes.
                // ===========================================================================
                try
                {
                    await ProcessGenericAttributesAsync(context, cargo, scope, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessGenericAttributesAsync));
                }

                // ===========================================================================
                // Process avatars.
                // ===========================================================================
                if (_customerSettings.AllowCustomersToUploadAvatars)
                {
                    try
                    {
                        cargo.NumberOfNewImages += await ProcessAvatarsAsync(context, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessAvatarsAsync));
                    }
                }

                // ===========================================================================
                // Process addresses.
                // ===========================================================================
                try
                {
                    await ProcessAddressesAsync(context, cargo, scope, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessAddressesAsync));
                }

                if (segmenter.IsLastSegment || context.Abort == DataExchangeAbortion.Hard)
                {
                    AddInfoForDeprecatedFields(context);

                    if (cargo.NumberOfNewImages > 0)
                    {
                        context.Result.AddInfo("Importing new images may result in image duplicates if TinyImage is installed or the images are larger than \"Maximum image size\" setting.");
                    }
                }
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<Customer>(context, batch), cancelToken);
        }

        protected virtual async Task<int> ProcessCustomersAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Customer>>
            batch)
        {
            var currentCustomer = _services.WorkContext.CurrentCustomer;
            var customerQuery = _db.Customers
                .AsSplitQuery()
                .Include(x => x.Addresses)
                .IncludeCustomerRoles();

            foreach (var row in batch)
            {
                Customer customer = null;
                var id = row.GetDataValue<int>(nameof(Customer.Id));
                var email = row.GetDataValue<string>(nameof(Customer.Email));
                var userName = row.GetDataValue<string>(nameof(Customer.Username));

                foreach (var keyName in context.KeyFieldNames)
                {
                    switch (keyName)
                    {
                        case nameof(Customer.Id):
                            customer = await _db.Customers.FindByIdAsync(id, true, context.CancelToken);
                            break;
                        case nameof(Customer.CustomerGuid):
                            var customerGuid = row.GetDataValue<string>(nameof(Customer.CustomerGuid));
                            if (customerGuid.HasValue())
                            {
                                var guid = new Guid(customerGuid);
                                customer = await customerQuery.FirstOrDefaultAsync(x => x.CustomerGuid == guid, context.CancelToken);
                            }
                            break;
                        case nameof(Customer.Email):
                            if (email.HasValue())
                            {
                                customer = await customerQuery.FirstOrDefaultAsync(x => x.Email == email, context.CancelToken);
                            }
                            break;
                        case nameof(Customer.Username):
                            if (userName.HasValue())
                            {
                                customer = await customerQuery.FirstOrDefaultAsync(x => x.Username == userName, context.CancelToken);
                            }
                            break;
                    }

                    if (customer != null)
                        break;
                }

                if (customer == null)
                {
                    if (context.UpdateOnly)
                    {
                        ++context.Result.SkippedRecords;
                        continue;
                    }

                    customer = new Customer
                    {
                        CustomerGuid = new Guid(),
                        AffiliateId = 0,
                        Active = true
                    };
                }
                else
                {
                    await _db.LoadCollectionAsync(customer, x => x.Addresses, false, null, context.CancelToken);
                    await _db.LoadCollectionAsync(customer, x => x.CustomerRoleMappings, false, q => q.Include(x => x.CustomerRole), context.CancelToken);
                }

                var affiliateId = row.GetDataValue<int>(nameof(Customer.AffiliateId));

                row.Initialize(customer, email ?? id.ToString());

                row.SetProperty(context.Result, (x) => x.CustomerGuid);
                row.SetProperty(context.Result, (x) => x.Username);
                row.SetProperty(context.Result, (x) => x.Email);
                row.SetProperty(context.Result, (x) => x.Salutation);
                row.SetProperty(context.Result, (x) => x.FullName);
                row.SetProperty(context.Result, (x) => x.FirstName);
                row.SetProperty(context.Result, (x) => x.LastName);

                if (_customerSettings.TitleEnabled)
                    row.SetProperty(context.Result, (x) => x.Title);

                if (_customerSettings.CompanyEnabled)
                    row.SetProperty(context.Result, (x) => x.Company);

                if (_customerSettings.DateOfBirthEnabled)
                    row.SetProperty(context.Result, (x) => x.BirthDate);

                if (_privacySettings.StoreLastIpAddress)
                    row.SetProperty(context.Result, (x) => x.LastIpAddress);

                if (email.HasValue() && currentCustomer.Email.EqualsNoCase(email))
                {
                    context.Result.AddInfo("Security. Ignored password of current customer (who started this import).", row.RowInfo, nameof(Customer.Password));
                }
                else
                {
                    row.SetProperty(context.Result, (x) => x.Password);
                    row.SetProperty(context.Result, (x) => x.PasswordFormatId);
                    row.SetProperty(context.Result, (x) => x.PasswordSalt);
                }

                row.SetProperty(context.Result, (x) => x.AdminComment);
                row.SetProperty(context.Result, (x) => x.IsTaxExempt);
                row.SetProperty(context.Result, (x) => x.Active);

                row.SetProperty(context.Result, (x) => x.CreatedOnUtc, context.UtcNow);
                row.SetProperty(context.Result, (x) => x.LastActivityDateUtc, context.UtcNow);

                if (_taxSettings.EuVatEnabled)
                    row.SetProperty(context.Result, (x) => x.VatNumberStatusId);

                if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                    row.SetProperty(context.Result, (x) => x.TimeZoneId);

                if (_customerSettings.GenderEnabled)
                    row.SetProperty(context.Result, (x) => x.Gender);

                if (affiliateId > 0 && cargo.AffiliateIds.Contains(affiliateId))
                    customer.AffiliateId = affiliateId;

                string customerNumber = null;

                if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.AutomaticallySet && row.IsTransient)
                {
                    customerNumber = row.Entity.Id.ToString();
                }
                else if (_customerSettings.CustomerNumberMethod == CustomerNumberMethod.Enabled && !row.IsTransient && row.HasDataValue(nameof(Customer.CustomerNumber)))
                {
                    customerNumber = row.GetDataValue<string>(nameof(Customer.CustomerNumber));
                }

                if (customerNumber.HasValue() || !cargo.CustomerNumbers.Contains(customerNumber))
                {
                    row.Entity.CustomerNumber = customerNumber;

                    if (!customerNumber.IsEmpty())
                    {
                        cargo.CustomerNumbers.Add(customerNumber);
                    }
                }

                if (row.IsTransient)
                {
                    _db.Customers.Add(customer);
                }
            }

            // Commit whole batch at once.
            var num = await scope.CommitAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessCustomerRolesAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Customer>> batch)
        {
            if (!cargo.AllowManagingCustomerRoles)
            {
                return 0;
            }

            foreach (var row in batch)
            {
                var customer = row.Entity;
                var importRoleSystemNames = row.GetDataValue<List<string>>("CustomerRoleSystemNames") ?? new();

                var assignedRoles = customer.CustomerRoleMappings
                    .Where(x => !x.IsSystemMapping)
                    .Select(x => x.CustomerRole)
                    .ToDictionarySafe(x => x.SystemName, StringComparer.OrdinalIgnoreCase);

                // Roles to remove.
                foreach (var customerRole in assignedRoles)
                {
                    var systemName = customerRole.Key;
                    if (!systemName.EqualsNoCase(SystemCustomerRoleNames.Administrators) &&
                        !systemName.EqualsNoCase(SystemCustomerRoleNames.SuperAdministrators) &&
                        !importRoleSystemNames.Contains(systemName))
                    {
                        var mappings = customer.CustomerRoleMappings.Where(x => !x.IsSystemMapping && x.CustomerRoleId == customerRole.Value.Id);
                        _db.CustomerRoleMappings.RemoveRange(mappings);
                    }
                }

                // Roles to add.
                foreach (var systemName in importRoleSystemNames)
                {
                    if (systemName.EqualsNoCase(SystemCustomerRoleNames.Administrators) ||
                        systemName.EqualsNoCase(SystemCustomerRoleNames.SuperAdministrators))
                    {
                        context.Result.AddInfo("Security. Ignored administrator role.", row.RowInfo, "CustomerRoleSystemNames");
                    }
                    else if (!assignedRoles.ContainsKey(systemName))
                    {
                        // Add role mapping but never insert roles.
                        // Be careful not to insert mappings several times!
                        if (cargo.CustomerRoleIds.TryGetValue(systemName, out var roleId))
                        {
                            _db.CustomerRoleMappings.Add(new CustomerRoleMapping
                            {
                                CustomerId = customer.Id,
                                CustomerRoleId = roleId
                            });
                        }
                    }
                }
            }

            var num = await scope.CommitAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessGenericAttributesAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Customer>> batch)
        {
            // TODO: (mg) (core) (perf) (low) Prefetch all generic attributes for whole batch and work against batch (to be implemented after initial release).

            foreach (var row in batch)
            {
                if (_taxSettings.EuVatEnabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.VatNumber, row);

                if (_customerSettings.StreetAddressEnabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.StreetAddress, row);

                if (_customerSettings.StreetAddress2Enabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.StreetAddress2, row);

                if (_customerSettings.CityEnabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.City, row);

                if (_customerSettings.ZipPostalCodeEnabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.ZipPostalCode, row);

                if (_customerSettings.CountryEnabled)
                    SetGenericAttribute<int>(SystemCustomerAttributeNames.CountryId, row);

                if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                    SetGenericAttribute<int>(SystemCustomerAttributeNames.StateProvinceId, row);

                if (_customerSettings.PhoneEnabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.Phone, row);

                if (_customerSettings.FaxEnabled)
                    SetGenericAttribute<string>(SystemCustomerAttributeNames.Fax, row);

                var countryId = CountryCodeToId(row.GetDataValue<string>("CountryCode"), cargo);
                var stateId = StateAbbreviationToId(countryId, row.GetDataValue<string>("StateAbbreviation"), cargo);

                if (countryId.HasValue)
                    SetGenericAttribute(SystemCustomerAttributeNames.CountryId, countryId.Value, row);

                if (stateId.HasValue)
                    SetGenericAttribute(SystemCustomerAttributeNames.StateProvinceId, stateId.Value, row);
            }

            var num = await scope.CommitAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessAvatarsAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Customer>> batch)
        {
            _mediaImporter.MessageHandler ??= (msg, item) =>
            {
                AddMessage<Customer>(msg, item, context);
            };

            var items = batch
                .Select(row => new
                {
                    Row = row,
                    Url = row.GetDataValue<string>("AvatarPictureUrl")
                })
                .Where(x => x.Url.HasValue())
                .Select(x => _mediaImporter.CreateDownloadItem(context.ImageDirectory, context.ImageDownloadDirectory, x.Row.Entity, x.Url, x.Row, 1))
                .ToList();

            return await _mediaImporter.ImportCustomerAvatarsAsync(scope, items, DuplicateFileHandling.Rename, context.CancelToken);
        }

        protected virtual async Task<int> ProcessAddressesAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Customer>> batch)
        {
            foreach (var row in batch)
            {
                ImportAddress("BillingAddress.", row, context, cargo);
                ImportAddress("ShippingAddress.", row, context, cargo);
            }

            var num = await scope.CommitAsync(context.CancelToken);

            if (num > 0 && _shoppingCartSettings.QuickCheckoutEnabled)
            {
                // Set default billing and shipping address.
                foreach (var row in batch)
                {
                    var customer = row.Entity;
                    customer.GenericAttributes.DefaultBillingAddressId = customer.BillingAddress?.Id;
                    customer.GenericAttributes.DefaultShippingAddressId = customer.ShippingAddress?.Id;
                }

                await scope.CommitAsync(context.CancelToken);
            }

            return num;
        }

        private static void ImportAddress(
            string fieldPrefix,
            ImportRow<Customer> row,
            ImportExecuteContext context,
            ImporterCargoData cargo)
        {
            // Last name is mandatory for an address to be imported or updated.
            if (!row.HasDataValue(fieldPrefix + "LastName"))
            {
                return;
            }

            Address address = null;

            if (fieldPrefix == "BillingAddress.")
            {
                address = row.Entity.BillingAddress ?? new Address { CreatedOnUtc = context.UtcNow };
            }
            else if (fieldPrefix == "ShippingAddress.")
            {
                address = row.Entity.ShippingAddress ?? new Address { CreatedOnUtc = context.UtcNow };
            }

            var childRow = new ImportRow<Address>(row.Segmenter, row.DataRow, row.Position);
            childRow.Initialize(address, row.EntityDisplayName);

            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.Salutation), x => x.Salutation);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.Title), x => x.Title);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.FirstName), x => x.FirstName);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.LastName), x => x.LastName);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.Email), x => x.Email);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.Company), x => x.Company);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.City), x => x.City);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.Address1), x => x.Address1);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.Address2), x => x.Address2);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.ZipPostalCode), x => x.ZipPostalCode);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.PhoneNumber), x => x.PhoneNumber);
            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.FaxNumber), x => x.FaxNumber);

            childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.CountryId), x => x.CountryId);
            if (childRow.Entity.CountryId == null)
            {
                // Try with country code.
                childRow.SetProperty(context.Result, fieldPrefix + "CountryCode", x => x.CountryId, converter: (val, ci) => CountryCodeToId(val.ToString(), cargo));
            }

            var countryId = childRow.Entity.CountryId;
            if (countryId.HasValue)
            {
                if (row.HasDataValue(fieldPrefix + nameof(Address.StateProvinceId)))
                {
                    childRow.SetProperty(context.Result, fieldPrefix + nameof(Address.StateProvinceId), x => x.StateProvinceId);
                }
                else
                {
                    // Try with state abbreviation.
                    childRow.SetProperty(context.Result, fieldPrefix + "StateAbbreviation", x => x.StateProvinceId, converter: (val, ci) => StateAbbreviationToId(countryId, val.ToString(), cargo));
                }
            }

            if (!childRow.IsDirty)
            {
                // Not one single property could be set. Get out!
                return;
            }

            if (address.Id == 0)
            {
                // Avoid importing two addresses if billing and shipping address are equal.
                var appliedAddress = row.Entity.Addresses.FindAddress(address);
                if (appliedAddress == null)
                {
                    appliedAddress = address;
                    row.Entity.Addresses.Add(appliedAddress);
                }

                // Map address to customer.
                if (fieldPrefix == "BillingAddress.")
                {
                    row.Entity.BillingAddress = appliedAddress;
                }
                else if (fieldPrefix == "ShippingAddress.")
                {
                    row.Entity.ShippingAddress = appliedAddress;
                }
            }
        }

        private static int? CountryCodeToId(string code, ImporterCargoData cargo)
        {
            if (code.HasValue() && cargo.Countries.TryGetValue(code, out var countryId) && countryId != 0)
            {
                return countryId;
            }

            return null;
        }

        private static int? StateAbbreviationToId(int? countryId, string abbreviation, ImporterCargoData cargo)
        {
            if (countryId.HasValue &&
                abbreviation.HasValue() &&
                cargo.StateProvinces.TryGetValue(Tuple.Create(countryId.Value, abbreviation), out var stateId) &&
                stateId != 0)
            {
                return stateId;
            }

            return null;
        }

        private static void SetGenericAttribute<TProp>(string key, ImportRow<Customer> row)
        {
            if (row.IsTransient)
                return;

            SetGenericAttribute(key, row.GetDataValue<TProp>(key), row);
        }

        private static void SetGenericAttribute<TProp>(string key, TProp value, ImportRow<Customer> row)
        {
            if (row.IsTransient)
                return;

            if (row.IsNew || value != null)
            {
                row.Entity.GenericAttributes.Set(key, value);
            }
        }

        private static void AddInfoForDeprecatedFields(ImportExecuteContext context)
        {
            if (context.DataSegmenter.HasColumn("IsGuest"))
            {
                context.Result.AddInfo("Deprecated field. Use CustomerRoleSystemNames instead.", null, "IsGuest");
            }
            if (context.DataSegmenter.HasColumn("IsRegistered"))
            {
                context.Result.AddInfo("Deprecated field. Use CustomerRoleSystemNames instead.", null, "IsRegistered");
            }
            if (context.DataSegmenter.HasColumn("IsAdministrator"))
            {
                context.Result.AddInfo("Deprecated field. Use CustomerRoleSystemNames instead.", null, "IsAdministrator");
            }
        }

        private async Task<ImporterCargoData> GetCargoData(ImportExecuteContext context)
        {
            if (context.CustomProperties.TryGetValue(CargoDataKey, out object value))
            {
                return (ImporterCargoData)value;
            }

            var allowManagingCustomerRoles = await _services.Permissions.AuthorizeAsync(Permissions.Customer.EditRole, _services.WorkContext.CurrentCustomer);

            var affiliateIds = await _db.Affiliates
                .AsQueryable()
                .Select(x => x.Id)
                .ToListAsync(context.CancelToken);

            var customerNumbers = await _db.Customers
                .AsQueryable()
                .Where(x => !string.IsNullOrEmpty(x.CustomerNumber))
                .Select(x => x.CustomerNumber)
                .ToListAsync(context.CancelToken);

            var customerRoleIds = await _db.CustomerRoles
                .AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.SystemName))
                .Select(x => new { x.Id, x.SystemName })
                .ToListAsync(context.CancelToken);

            var allCountries = await _db.Countries
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(context.CancelToken);

            var countries = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var country in allCountries)
            {
                countries[country.TwoLetterIsoCode] = country.Id;
                countries[country.ThreeLetterIsoCode] = country.Id;
            }

            var stateProvinces = await _db.StateProvinces
                .AsNoTracking()
                .ToListAsync(context.CancelToken);

            var result = new ImporterCargoData
            {
                AllowManagingCustomerRoles = allowManagingCustomerRoles,
                AffiliateIds = affiliateIds,
                CustomerNumbers = new HashSet<string>(customerNumbers, StringComparer.OrdinalIgnoreCase),
                CustomerRoleIds = customerRoleIds.ToDictionarySafe(x => x.SystemName, x => x.Id, StringComparer.OrdinalIgnoreCase),
                Countries = countries,
                StateProvinces = stateProvinces.ToDictionarySafe(x => new Tuple<int, string>(x.CountryId, x.Abbreviation), x => x.Id)
            };

            context.CustomProperties[CargoDataKey] = result;
            return result;
        }

        /// <summary>
        /// Perf: contains data that is loaded once per import.
        /// </summary>
        protected class ImporterCargoData
        {
            public bool AllowManagingCustomerRoles { get; init; }
            public List<int> AffiliateIds { get; init; }
            public HashSet<string> CustomerNumbers { get; init; }
            public Dictionary<string, int> CustomerRoleIds { get; init; }
            public Dictionary<string, int> Countries { get; init; }
            public Dictionary<Tuple<int, string>, int> StateProvinces { get; init; }
            public int NumberOfNewImages { get; set; }
        }
    }
}
