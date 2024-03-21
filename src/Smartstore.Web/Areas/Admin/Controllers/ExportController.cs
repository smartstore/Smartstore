using System.Collections.Frozen;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Export;
using Smartstore.Admin.Models.Scheduling;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Export.Deployment;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class ExportController : AdminController
    {
        private static readonly IReadOnlyDictionary<ExportDeploymentType, string> DeploymentTypeIconClasses = new Dictionary<ExportDeploymentType, string>()
        {
            { ExportDeploymentType.FileSystem, "far fa-folder-open" },
            { ExportDeploymentType.Email, "far fa-envelope" },
            { ExportDeploymentType.Http, "fa fa-globe" },
            { ExportDeploymentType.Ftp, "far fa-copy" },
            { ExportDeploymentType.PublicFolder, "fa fa-unlock" },
        }.ToFrozenDictionary();

        private readonly SmartDbContext _db;
        private readonly IExportProfileService _exportProfileService;
        private readonly ICategoryService _categoryService;
        private readonly ICurrencyService _currencyService;
        private readonly ILanguageService _languageService;
        private readonly IDataExporter _dataExporter;
        private readonly ITaskScheduler _taskScheduler;
        private readonly IProviderManager _providerManager;
        private readonly ITaskStore _taskStore;
        private readonly DataExchangeSettings _dataExchangeSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ModuleManager _moduleManager;

        public ExportController(
            SmartDbContext db,
            IExportProfileService exportProfileService,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            ILanguageService languageService,
            IDataExporter dataExporter,
            ITaskScheduler taskScheduler,
            IProviderManager providerManager,
            ITaskStore taskStore,
            DataExchangeSettings dataExchangeSettings,
            CustomerSettings customerSettings,
            ModuleManager moduleManager)
        {
            _db = db;
            _exportProfileService = exportProfileService;
            _categoryService = categoryService;
            _currencyService = currencyService;
            _languageService = languageService;
            _dataExporter = dataExporter;
            _taskScheduler = taskScheduler;
            _providerManager = providerManager;
            _taskStore = taskStore;
            _dataExchangeSettings = dataExchangeSettings;
            _customerSettings = customerSettings;
            _moduleManager = moduleManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> List()
        {
            var model = new List<ExportProfileModel>();

            var providers = _exportProfileService.LoadAllExportProviders(0, false)
                .ToDictionarySafe(x => x.Metadata.SystemName);

            var profiles = await _db.ExportProfiles
                .AsNoTracking()
                .Include(x => x.Task)
                .Include(x => x.Deployments)
                .OrderBy(x => x.IsSystemProfile).ThenBy(x => x.Name)
                .ToListAsync();

            var lastExecutionInfos = (await _taskStore.GetExecutionInfoQuery(false)
                .ApplyCurrentMachineNameFilter()
                .ApplyTaskFilter(0, true)
                .ToListAsync())
                .ToDictionarySafe(x => x.TaskDescriptorId);

            foreach (var profile in profiles)
            {
                if (providers.TryGetValue(profile.ProviderSystemName, out var provider))
                {
                    var profileModel = new ExportProfileModel();

                    lastExecutionInfos.TryGetValue(profile.TaskId, out var lastExecutionInfo);
                    await PrepareProfileModel(profileModel, profile, provider, lastExecutionInfo, false);

                    var fileDetailsModel = await CreateFileDetailsModel(profile, null);
                    profileModel.FileCount = fileDetailsModel.FileCount;
                    profileModel.TaskModel = await profile.Task.MapAsync(lastExecutionInfo);

                    model.Add(profileModel);
                }
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> ProfileFileDetails(int profileId, int deploymentId)
        {
            var model = await CreateFileDetailsModel(profileId, deploymentId);

            return model != null ? PartialView(model) : new EmptyResult();
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> ProfileFileCount(int profileId)
        {
            var model = await CreateFileDetailsModel(profileId, 0);

            return model != null ? PartialView(model.FileCount) : new EmptyResult();
        }

        [Permission(Permissions.Configuration.Export.Create)]
        public async Task<IActionResult> Create()
        {
            var num = 0;
            var providers = _exportProfileService.LoadAllExportProviders(0, false)
                .ToDictionarySafe(x => x.Metadata.SystemName);

            var profiles = await _db.ExportProfiles
                .AsNoTracking()
                .ApplyStandardFilter()
                .ToListAsync();

            var model = new ExportProfileModel();

            ViewBag.Providers = providers.Values
                .Select(x => new ExportProfileModel.ProviderSelectItem
                {
                    Id = ++num,
                    SystemName = x.Metadata.SystemName,
                    ImageUrl = GetThumbnailUrl(x),
                    FriendlyName = _moduleManager.GetLocalizedFriendlyName(x.Metadata),
                    Description = _moduleManager.GetLocalizedDescription(x.Metadata)
                })
                .ToList();

            ViewBag.Profiles = profiles
                .Select(x => new ExportProfileModel.ProviderSelectItem
                {
                    Id = x.Id,
                    SystemName = x.ProviderSystemName,
                    FriendlyName = x.Name,
                    ImageUrl = GetThumbnailUrl(providers.Get(x.ProviderSystemName))
                })
                .ToList();

            return PartialView(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Export.Create)]
        public async Task<IActionResult> Create(ExportProfileModel model)
        {
            if (model.ProviderSystemName.HasValue())
            {
                var provider = _providerManager.GetProvider<IExportProvider>(model.ProviderSystemName);
                if (provider != null)
                {
                    var profile = await _exportProfileService.InsertExportProfileAsync(provider, false, null, model.CloneProfileId ?? 0);

                    return RedirectToAction(nameof(Edit), new { id = profile.Id });
                }
            }

            NotifyError(T("Admin.Common.ProviderNotLoaded", model.ProviderSystemName.NaIfEmpty()));

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var (profile, provider) = await LoadProfileAndProvider(id);
            if (profile == null)
            {
                return NotFound();
            }

            var model = new ExportProfileModel();
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(profile.TaskId);
            await PrepareProfileModel(model, profile, provider, lastExecutionInfo, true);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("save", "save-continue"), ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Export.Update)]
        public async Task<IActionResult> Edit(ExportProfileModel model, bool continueEditing)
        {
            var (profile, provider) = await LoadProfileAndProvider(model.Id);
            if (profile == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(profile.TaskId);
                await PrepareProfileModel(model, profile, provider, lastExecutionInfo, true);

                return View(model);
            }

            var dtHelper = Services.DateTimeHelper;

            profile.Name = model.Name.NullEmpty() ?? provider.Metadata.FriendlyName.NullEmpty() ?? provider.Metadata.SystemName;
            profile.FileNamePattern = model.FileNamePattern;
            profile.FolderName = model.FolderName;
            profile.Enabled = model.Enabled;
            profile.ExportRelatedData = model.ExportRelatedData;
            profile.Offset = model.Offset;
            profile.Limit = model.Limit ?? 0;
            profile.BatchSize = model.BatchSize ?? 0;
            profile.PerStore = model.PerStore;
            profile.CompletedEmailAddresses = string.Join(",", model.CompletedEmailAddresses ?? Array.Empty<string>());
            profile.EmailAccountId = model.EmailAccountId ?? 0;
            profile.CreateZipArchive = model.CreateZipArchive;
            profile.Cleanup = model.Cleanup;

            // Projection.
            if (model.Projection != null)
            {
                var projection = MiniMapper.Map<ExportProjectionModel, ExportProjection>(model.Projection);
                projection.NumberOfMediaFiles = model.Projection.NumberOfPictures;
                projection.AppendDescriptionText = string.Join(",", model.Projection.AppendDescriptionText ?? Array.Empty<string>());
                projection.RemoveCriticalCharacters = model.Projection.RemoveCriticalCharacters;
                projection.CriticalCharacters = string.Join(",", model.Projection.CriticalCharacters ?? Array.Empty<string>());

                profile.Projection = XmlHelper.Serialize(projection);
            }

            // Filtering.
            if (model.Filter != null)
            {
                var filter = MiniMapper.Map<ExportFilterModel, ExportFilter>(model.Filter);
                filter.StoreId = model.Filter.StoreId ?? 0;
                filter.CategoryIds = model.Filter.CategoryIds?.Where(x => x != 0)?.ToArray() ?? Array.Empty<int>();

                if (model.Filter.CreatedFrom.HasValue)
                {
                    filter.CreatedFrom = dtHelper.ConvertToUtcTime(model.Filter.CreatedFrom.Value);
                }
                if (model.Filter.CreatedTo.HasValue)
                {
                    filter.CreatedTo = dtHelper.ConvertToUtcTime(model.Filter.CreatedTo.Value);
                }
                if (model.Filter.LastActivityFrom.HasValue)
                {
                    filter.LastActivityFrom = dtHelper.ConvertToUtcTime(model.Filter.LastActivityFrom.Value);
                }
                if (model.Filter.LastActivityTo.HasValue)
                {
                    filter.LastActivityTo = dtHelper.ConvertToUtcTime(model.Filter.LastActivityTo.Value);
                }

                profile.Filtering = XmlHelper.Serialize(filter);
            }

            // Provider configuration.
            profile.ProviderConfigData = null;
            try
            {
                var configInfo = provider.Value.ConfigurationInfo;
                if (configInfo != null && model.CustomProperties.TryGetValue("ProviderConfigData", out object configData))
                {
                    profile.ProviderConfigData = XmlHelper.Serialize(configData, configInfo.ModelType);
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return continueEditing
                ? RedirectToAction(nameof(Edit), new { id = profile.Id })
                : RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> ResolveFileNamePatternExample(int id, string pattern)
        {
            var profile = await _db.ExportProfiles.FindByIdAsync(id, false);

            var resolvedPattern = profile.ResolveFileNamePattern(
                Services.StoreContext.CurrentStore,
                1,
                _dataExchangeSettings.MaxFileNameLength,
                pattern.EmptyNull());

            return Content(resolvedPattern);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Export.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var (profile, _) = await LoadProfileAndProvider(id);
            if (profile == null)
            {
                return NotFound();
            }

            try
            {
                await _exportProfileService.DeleteExportProfileAsync(profile);

                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = profile.Id });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Export.Execute)]
        public async Task<IActionResult> Execute(int id, string selectedIds)
        {
            // Permissions checked internally by DataExporter.
            var profile = await _db.ExportProfiles.FindByIdAsync(id, false);
            if (profile == null)
            {
                return NotFound();
            }

            var taskParams = new Dictionary<string, string>
            {
                { TaskExecutor.CurrentCustomerIdParamName, Services.WorkContext.CurrentCustomer.Id.ToString() },
                { TaskExecutor.CurrentStoreIdParamName, Services.StoreContext.CurrentStore.Id.ToString() }
            };

            if (selectedIds.HasValue())
            {
                taskParams.Add("SelectedIds", selectedIds);
            }

            _ = _taskScheduler.RunSingleTaskAsync(profile.TaskId, taskParams);

            NotifyInfo(T("Admin.System.ScheduleTasks.RunNow.Progress.DataExportTask"));

            TempData["ExecutedProfileId"] = profile.Id.ToString();

            return RedirectToReferrer(null, () => RedirectToAction(nameof(List)));
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> DownloadLogFile(int id)
        {
            var profile = await _db.ExportProfiles.FindByIdAsync(id, false);
            if (profile == null)
            {
                return NotFound();
            }

            var dir = await _exportProfileService.GetExportDirectoryAsync(profile);
            var logFile = await dir.GetFileAsync("log.txt");
            if (logFile.Exists)
            {
                try
                {
                    var stream = await logFile.OpenReadAsync();
                    return new FileStreamResult(stream, MediaTypeNames.Text.Plain);
                }
                catch (IOException)
                {
                    NotifyWarning(T("Admin.Common.FileInUse"));
                }
            }

            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadExportFile(int id, string name, bool? isDeployment)
        {
            if (PathUtility.HasInvalidFileNameChars(name))
            {
                throw new BadHttpRequestException("Invalid file name: " + name.NaIfEmpty());
            }

            string message = null;
            IFile file = null;

            if (await Services.Permissions.AuthorizeAsync(Permissions.Configuration.Export.Read))
            {
                if (isDeployment ?? false)
                {
                    var deployment = await _db.ExportDeployments.FindByIdAsync(id, false);
                    if (deployment != null)
                    {
                        var deploymentDir = await _exportProfileService.GetDeploymentDirectoryAsync(deployment);
                        file = await deploymentDir?.GetFileAsync(name);
                    }
                }
                else
                {
                    var profile = await _db.ExportProfiles.FindByIdAsync(id, false);
                    if (profile != null)
                    {
                        var dir = await _exportProfileService.GetExportDirectoryAsync(profile, "Content");
                        file = await dir.GetFileAsync(name);

                        if (!(file?.Exists ?? false))
                        {
                            file = await dir.Parent.GetFileAsync(name);
                        }
                    }
                }

                if (file?.Exists ?? false)
                {
                    try
                    {
                        var stream = await file.OpenReadAsync();
                        return new FileStreamResult(stream, MimeTypes.MapNameToMimeType(file.PhysicalPath))
                        {
                            FileDownloadName = file.Name
                        };
                    }
                    catch (IOException)
                    {
                        message = T("Admin.Common.FileInUse");
                    }
                }
            }
            else
            {
                message = T("Admin.AccessDenied.Description");
            }

            if (message.IsEmpty())
            {
                message = T("Admin.Common.ResourceNotFound");
            }

            return File(message.GetBytes(), MediaTypeNames.Text.Plain, "DownloadExportFile.txt");
        }

        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> Preview(int id)
        {
            var (profile, provider) = await LoadProfileAndProvider(id);
            if (profile == null)
            {
                return NotFound();
            }

            if (!profile.Enabled)
            {
                NotifyInfo(T("Admin.DataExchange.Export.EnableProfileForPreview"));

                return RedirectToAction(nameof(Edit), new { id = profile.Id });
            }

            var dir = await _exportProfileService.GetExportDirectoryAsync(profile);
            var logFile = await dir.GetFileAsync("log.txt");

            var model = new ExportPreviewModel
            {
                Id = profile.Id,
                Name = profile.Name,
                EntityType = provider.Value.EntityType,
                ThumbnailUrl = GetThumbnailUrl(provider),
                LogFileExists = logFile.Exists,
                UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email
            };

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Export.Read)]
        public async Task<IActionResult> PreviewList(GridCommand command, int id)
        {
            var (profile, provider) = await LoadProfileAndProvider(id);
            if (profile == null)
            {
                throw new BadHttpRequestException($"Cannot find export profile with ID {id}.", StatusCodes.Status404NotFound);
            }

            IGridModel gridModel = null;
            var request = new DataExportRequest(profile, provider);
            var previewResult = await _dataExporter.PreviewAsync(request, command.Page - 1, command.PageSize);

            var normalizedTotal = profile.Limit > 0 && previewResult.TotalRecords > profile.Limit
                ? profile.Limit
                : previewResult.TotalRecords;

            if (provider.Value.EntityType == ExportEntityType.Product)
            {
                var rows = previewResult.Data
                    .Select(x =>
                    {
                        var product = x.Entity as Product;
                        return new ExportPreviewProductModel
                        {
                            Id = product.Id,
                            ProductTypeId = product.ProductTypeId,
                            ProductTypeName = product.GetProductTypeLabel(Services.Localization),
                            ProductTypeLabelHint = product.ProductTypeLabelHint,
                            EditUrl = Url.Action("Edit", "Product", new { id = product.Id }),
                            Name = x.Name,
                            Sku = x.Sku,
                            Price = x.Price,
                            Published = product.Published,
                            StockQuantity = product.StockQuantity,
                            AdminComment = x.AdminComment
                        };
                    })
                    .ToList();

                gridModel = new GridModel<ExportPreviewProductModel> { Rows = rows, Total = normalizedTotal };
            }
            else if (provider.Value.EntityType == ExportEntityType.Order)
            {
                var rows = previewResult.Data
                    .Select(x => new ExportPreviewOrderModel
                    {
                        Id = x.Id,
                        HasNewPaymentNotification = x.HasNewPaymentNotification,
                        EditUrl = Url.Action("Edit", "Order", new { id = x.Id }),
                        OrderNumber = x.OrderNumber,
                        OrderStatus = x.OrderStatus,
                        PaymentStatus = x.PaymentStatus,
                        ShippingStatus = x.ShippingStatus,
                        CustomerId = x.CustomerId,
                        CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                        OrderTotal = x.OrderTotal,
                        StoreName = (string)x.Store.Name
                    })
                    .ToList();

                gridModel = new GridModel<ExportPreviewOrderModel> { Rows = rows, Total = normalizedTotal };
            }
            else if (provider.Value.EntityType == ExportEntityType.Category)
            {
                var rows = await previewResult.Data
                    .SelectAwait(async x =>
                    {
                        var category = x.Entity as Category;
                        return new ExportPreviewCategoryModel
                        {
                            Id = category.Id,
                            Breadcrumb = await _categoryService.GetCategoryPathAsync(category, aliasPattern: "({0})"),
                            FullName = x.FullName,
                            Alias = x.Alias,
                            Published = category.Published,
                            DisplayOrder = category.DisplayOrder,
                            LimitedToStores = category.LimitedToStores
                        };
                    })
                    .AsyncToList();

                gridModel = new GridModel<ExportPreviewCategoryModel> { Rows = rows, Total = normalizedTotal };
            }
            else if (provider.Value.EntityType == ExportEntityType.Manufacturer)
            {
                var rows = previewResult.Data
                    .Select(x => new ExportPreviewManufacturerModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Published = x.Published,
                        DisplayOrder = x.DisplayOrder,
                        LimitedToStores = x.LimitedToStores,
                        SubjectToAcl = x.SubjectToAcl,
                    })
                    .ToList();

                gridModel = new GridModel<ExportPreviewManufacturerModel> { Rows = rows, Total = normalizedTotal };
            }
            else if (provider.Value.EntityType == ExportEntityType.Customer)
            {
                var rows = previewResult.Data
                    .Select(x =>
                    {
                        var customer = x.Entity as Customer;
                        var customerRoles = x.CustomerRoles as List<dynamic>;
                        var customerRolesString = string.Join(", ", customerRoles.Select(x => x.Name));

                        return new ExportPreviewCustomerModel
                        {
                            Id = customer.Id,
                            Active = customer.Active,
                            CreatedOn = Services.DateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
                            CustomerRoleNames = customerRolesString,
                            Email = customer.Email,
                            FullName = x._FullName,
                            LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc),
                            Username = customer.Username
                        };
                    })
                    .ToList();

                gridModel = new GridModel<ExportPreviewCustomerModel> { Rows = rows, Total = normalizedTotal };
            }
            else if (provider.Value.EntityType == ExportEntityType.NewsletterSubscription)
            {
                var rows = previewResult.Data
                    .Select(x =>
                    {
                        var subscription = x.Entity as NewsletterSubscription;
                        return new ExportPreviewNewsletterSubscriptionModel
                        {
                            Id = subscription.Id,
                            Active = subscription.Active,
                            CreatedOn = Services.DateTimeHelper.ConvertToUserTime(subscription.CreatedOnUtc, DateTimeKind.Utc),
                            Email = subscription.Email,
                            StoreName = (string)x.Store.Name
                        };
                    })
                    .ToList();

                gridModel = new GridModel<ExportPreviewNewsletterSubscriptionModel> { Rows = rows, Total = normalizedTotal };
            }
            else if (provider.Value.EntityType == ExportEntityType.ShoppingCartItem)
            {
                var guest = T("Admin.Customers.Guest").Value;
                var cartTypeName = Services.Localization.GetLocalizedEnum(ShoppingCartType.ShoppingCart);
                var wishlistTypeName = Services.Localization.GetLocalizedEnum(ShoppingCartType.Wishlist);

                var rows = previewResult.Data
                    .Select(item =>
                    {
                        var cartItem = item.Entity as ShoppingCartItem;
                        return new ExportPreviewShoppingCartItemModel
                        {
                            Id = cartItem.Id,
                            ShoppingCartTypeId = cartItem.ShoppingCartTypeId,
                            ShoppingCartTypeName = cartItem.ShoppingCartType == ShoppingCartType.Wishlist ? wishlistTypeName : cartTypeName,
                            CustomerId = cartItem.CustomerId,
                            CustomerEmail = cartItem.Customer.IsGuest() ? guest : cartItem.Customer.Email,
                            ProductTypeId = cartItem.Product.ProductTypeId,
                            ProductTypeName = cartItem.Product.GetProductTypeLabel(Services.Localization),
                            ProductTypeLabelHint = cartItem.Product.ProductTypeLabelHint,
                            Name = cartItem.Product.Name,
                            Sku = cartItem.Product.Sku,
                            Price = cartItem.Product.Price,
                            Published = cartItem.Product.Published,
                            StockQuantity = cartItem.Product.StockQuantity,
                            AdminComment = cartItem.Product.AdminComment,
                            CreatedOn = Services.DateTimeHelper.ConvertToUserTime(cartItem.CreatedOnUtc, DateTimeKind.Utc),
                            StoreName = (string)item.Store.Name
                        };
                    })
                    .ToList();

                gridModel = new GridModel<ExportPreviewShoppingCartItemModel> { Rows = rows, Total = normalizedTotal };
            }

            return Json(gridModel);
        }

        #region Deloyment

        [Permission(Permissions.Configuration.Export.Update)]
        public async Task<IActionResult> CreateDeployment(int id)
        {
            var (profile, provider) = await LoadProfileAndProvider(id);
            if (profile == null)
            {
                return NotFound();
            }

            var model = await CreateDeploymentModel(profile, new ExportDeployment
            {
                ProfileId = id,
                Enabled = true,
                DeploymentType = ExportDeploymentType.FileSystem,
                Name = profile.Name
            }, provider, true);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("save", "save-continue"), ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Export.CreateDeployment)]
        public async Task<IActionResult> CreateDeployment(ExportDeploymentModel model, bool continueEditing)
        {
            if (!await _db.ExportProfiles.AnyAsync(x => x.Id == model.ProfileId))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var deployment = MiniMapper.Map<ExportDeploymentModel, ExportDeployment>(model);
                deployment.EmailAddresses = string.Join(",", model.EmailAddresses ?? Array.Empty<string>());
                deployment.Id = 0;  // Route value > Model binding > MiniMapper > deployment.Id != 0 > SqlException!!

                _db.ExportDeployments.Add(deployment);

                await _db.SaveChangesAsync();

                return continueEditing ?
                    RedirectToAction(nameof(EditDeployment), new { id = deployment.Id }) :
                    RedirectToAction(nameof(Edit), new { id = model.ProfileId });
            }

            return await CreateDeployment(model.ProfileId);
        }

        [Permission(Permissions.Configuration.Export.Update)]
        public async Task<IActionResult> EditDeployment(int id)
        {
            var deployment = await _db.ExportDeployments.FindByIdAsync(id, false);
            if (deployment == null)
            {
                return NotFound();
            }

            var (profile, provider) = await LoadProfileAndProvider(deployment.ProfileId);
            if (profile == null)
            {
                return NotFound();
            }

            var model = await CreateDeploymentModel(profile, deployment, provider, true);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("save", "save-continue"), ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Export.Update)]
        public async Task<IActionResult> EditDeployment(ExportDeploymentModel model, bool continueEditing)
        {
            var deployment = await _db.ExportDeployments.FindByIdAsync(model.Id, true);
            if (deployment == null)
            {
                return NotFound();
            }

            var (profile, provider) = await LoadProfileAndProvider(deployment.ProfileId);
            if (profile == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model, deployment);
                deployment.EmailAddresses = string.Join(",", model.EmailAddresses ?? Array.Empty<string>());

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

                return continueEditing ?
                    RedirectToAction("EditDeployment", new { id = deployment.Id }) :
                    RedirectToAction(nameof(Edit), new { id = profile.Id });
            }

            model = await CreateDeploymentModel(profile, deployment, provider, true);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Export.Delete)]
        public async Task<IActionResult> DeleteDeployment(int id)
        {
            var deployment = await _db.ExportDeployments.FindByIdAsync(id, true);
            if (deployment == null)
            {
                return NotFound();
            }

            if (!await _db.ExportProfiles.AnyAsync(x => x.Id == deployment.ProfileId))
            {
                return NotFound();
            }

            _db.ExportDeployments.Remove(deployment);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

            return RedirectToAction(nameof(Edit), new { id = deployment.ProfileId });
        }

        #endregion

        #region Utilities

        private async Task PrepareProfileModel(
            ExportProfileModel model,
            ExportProfile profile,
            Provider<IExportProvider> provider,
            TaskExecutionInfo lastExecutionInfo,
            bool createForEdit)
        {
            MiniMapper.Map(profile, model);

            var dir = await _exportProfileService.GetExportDirectoryAsync(profile);
            var logFile = await dir.GetFileAsync("log.txt");
            var moduleDescriptor = provider.Metadata.ModuleDescriptor;

            model.TaskName = profile.Task.Name.NaIfEmpty();
            model.IsTaskRunning = lastExecutionInfo?.IsRunning ?? false;
            model.IsTaskEnabled = profile.Task.Enabled;
            model.LogFileExists = logFile.Exists;
            model.HasActiveProvider = provider != null;
            model.FileNamePatternDescriptions = T("Admin.DataExchange.Export.FileNamePatternDescriptions").Value.SplitSafe(';').ToArray();

            model.Provider = new ExportProfileModel.ProviderModel
            {
                EntityType = provider.Value.EntityType,
                EntityTypeName = Services.Localization.GetLocalizedEnum(provider.Value.EntityType),
                FileExtension = provider.Value.FileExtension,
                ThumbnailUrl = GetThumbnailUrl(provider),
                FriendlyName = _moduleManager.GetLocalizedFriendlyName(provider.Metadata),
                Description = _moduleManager.GetLocalizedDescription(provider.Metadata),
                Url = moduleDescriptor?.ProjectUrl,
                Author = moduleDescriptor?.Author,
                Version = moduleDescriptor?.Version?.ToString()
            };

            if (!createForEdit)
            {
                return;
            }

            var projection = XmlHelper.Deserialize<ExportProjection>(profile.Projection);
            var filter = XmlHelper.Deserialize<ExportFilter>(profile.Filtering);

            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.CurrentStore;
            var stores = Services.StoreContext.GetAllStores();
            var emailAccounts = await _db.EmailAccounts.AsNoTracking().ToListAsync();
            var languages = await _languageService.GetAllLanguagesAsync(true);

            var currencies = await _db.Currencies
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            model.Offset = profile.Offset;
            model.Limit = profile.Limit == 0 ? null : profile.Limit;
            model.BatchSize = profile.BatchSize == 0 ? null : profile.BatchSize;
            model.PerStore = profile.PerStore;
            model.EmailAccountId = profile.EmailAccountId;
            model.CompletedEmailAddresses = profile.CompletedEmailAddresses.SplitSafe(',').ToArray();
            model.CreateZipArchive = profile.CreateZipArchive;
            model.Cleanup = profile.Cleanup;
            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
            model.FileNamePatternExample = profile.ResolveFileNamePattern(store, 1, _dataExchangeSettings.MaxFileNameLength);

            ViewBag.EmailAccounts = emailAccounts
                .Select(x => new SelectListItem { Text = x.FriendlyName, Value = x.Id.ToString() })
                .ToList();

            ViewBag.CompletedEmailAddresses = new MultiSelectList(profile.CompletedEmailAddresses.SplitSafe(','));

            ViewBag.Stores = stores.ToSelectListItems();
            ViewBag.Languages = languages.ToSelectListItems();

            ViewBag.Currencies = currencies
                .Select(y => new SelectListItem { Text = y.GetLocalized(x => x.Name), Value = y.Id.ToString() })
                .ToList();

            ViewBag.DeploymentTypeIconClasses = DeploymentTypeIconClasses;

            // Projection.
            model.Projection = MiniMapper.Map<ExportProjection, ExportProjectionModel>(projection);
            model.Projection.NumberOfPictures = projection.NumberOfMediaFiles;
            model.Projection.AppendDescriptionText = projection.AppendDescriptionText.SplitSafe(',').ToArray();
            model.Projection.CriticalCharacters = projection.CriticalCharacters.SplitSafe(',').ToArray();

            if (profile.Projection.IsEmpty())
            {
                model.Projection.DescriptionMergingId = (int)ExportDescriptionMerging.Description;
            }

            // Filtering.
            model.Filter = MiniMapper.Map<ExportFilter, ExportFilterModel>(filter);

            // Deployment.
            model.Deployments = await profile.Deployments.SelectAwait(async x =>
            {
                var deploymentModel = await CreateDeploymentModel(profile, x, null, false);

                if (x.ResultInfo.HasValue())
                {
                    var resultInfo = XmlHelper.Deserialize<DataDeploymentResult>(x.ResultInfo);
                    var lastExecution = Services.DateTimeHelper.ConvertToUserTime(resultInfo.LastExecutionUtc, DateTimeKind.Utc);

                    deploymentModel.LastResult = new ExportDeploymentModel.LastResultInfo
                    {
                        Execution = lastExecution,
                        ExecutionPretty = lastExecution.ToHumanizedString(false),
                        Error = resultInfo.LastError
                    };
                }

                return deploymentModel;
            })
            .AsyncToList();

            // Provider.
            if (provider != null)
            {
                model.Provider.Feature = provider.Metadata.ExportFeatures;

                if (model.Provider.EntityType == ExportEntityType.Product)
                {
                    var manufacturers = await _db.Manufacturers
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    var productTags = await _db.ProductTags
                        .AsNoTracking()
                        .ToListAsync();

                    ViewBag.Manufacturers = manufacturers
                        .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                        .ToList();

                    ViewBag.ProductTags = productTags
                        .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                        .ToList();

                    ViewBag.PriceTypes = PriceDisplayType.LowestPrice
                        .ToSelectList(false)
                        .Where(x => x.Value != ((int)PriceDisplayType.Hide).ToString())
                        .ToList();

                    ViewBag.AppendDescriptionTexts = new MultiSelectList(projection.AppendDescriptionText.SplitSafe(','));
                    ViewBag.CriticalCharacters = new MultiSelectList(projection.CriticalCharacters.SplitSafe(','));

                    var categoryTree = !model.Filter.CategoryIds.IsNullOrEmpty() || model.Filter.CategoryId.GetValueOrDefault() != 0
                        ? await _categoryService.GetCategoryTreeAsync(0, true)
                        : null;

                    ViewBag.SelectedCategoryId = CreateSelectedCategoriesList(new[] { model.Filter.CategoryId ?? 0 }, categoryTree);
                    ViewBag.SelectedCategoryIds = CreateSelectedCategoriesList(model.Filter.CategoryIds, categoryTree);
                }
                else if (model.Provider.EntityType == ExportEntityType.Customer)
                {
                    var countries = await _db.Countries
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    ViewBag.Countries = countries
                        .Select(x => new SelectListItem { Text = x.GetLocalized(y => y.Name, language, true, false), Value = x.Id.ToString() })
                        .ToList();
                }

                try
                {
                    var configInfo = provider.Value.ConfigurationInfo;
                    if (configInfo != null)
                    {
                        model.Provider.ConfigurationWidget = configInfo.ConfigurationWidget;
                        model.Provider.ConfigDataType = configInfo.ModelType;
                        model.Provider.ConfigData = XmlHelper.Deserialize(profile.ProviderConfigData, configInfo.ModelType);
                    }
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            List<SelectListItem> CreateSelectedCategoriesList(int[] ids, TreeNode<ICategoryNode> categoryTree)
            {
                if (ids.IsNullOrEmpty())
                {
                    return new();
                }

                return ids
                    .Where(x => x != 0)
                    .Select(x =>
                    {
                        var node = categoryTree.SelectNodeById(x);
                        return new SelectListItem { Selected = true, Value = x.ToString(), Text = node == null ? x.ToString() : _categoryService.GetCategoryPath(node) };
                    })
                    .ToList();
            }
        }

        private async Task<ExportDeploymentModel> CreateDeploymentModel(
            ExportProfile profile,
            ExportDeployment deployment,
            Provider<IExportProvider> provider,
            bool createForEdit)
        {
            var model = MiniMapper.Map<ExportDeployment, ExportDeploymentModel>(deployment);

            model.EmailAddresses = deployment.EmailAddresses.SplitSafe(',').ToArray();
            model.DeploymentTypeName = Services.Localization.GetLocalizedEnum(deployment.DeploymentType);
            model.PublicFolderUrl = await _exportProfileService.GetDeploymentDirectoryUrlAsync(deployment);

            if (createForEdit)
            {
                model.CreateZip = profile.CreateZipArchive;

                ViewBag.DeploymentTypeIconClasses = DeploymentTypeIconClasses;

                if (ViewBag.EmailAccounts == null)
                {
                    var emailAccounts = await _db.EmailAccounts.AsNoTracking().ToListAsync();

                    ViewBag.EmailAccounts = emailAccounts
                        .Select(x => new SelectListItem { Text = x.FriendlyName, Value = x.Id.ToString() })
                        .ToList();
                }

                if (provider != null)
                {
                    model.ThumbnailUrl = GetThumbnailUrl(provider);
                }
            }
            else
            {
                model.FileCount = (await CreateFileDetailsModel(profile, deployment)).FileCount;
            }

            return model;
        }

        private async Task<ExportFileDetailsModel> CreateFileDetailsModel(int profileId, int deploymentId)
        {
            if (profileId != 0)
            {
                var (profile, _) = await LoadProfileAndProvider(profileId);
                if (profile != null)
                {
                    return await CreateFileDetailsModel(profile, null);
                }
            }
            else if (deploymentId != 0)
            {
                var deployment = await _db.ExportDeployments
                    .AsNoTracking()
                    .Include(x => x.Profile)
                    .FirstOrDefaultAsync(x => x.Id == deploymentId);

                if (deployment != null)
                {
                    return await CreateFileDetailsModel(deployment.Profile, deployment);
                }
            }

            return null;
        }

        private async Task<ExportFileDetailsModel> CreateFileDetailsModel(ExportProfile profile, ExportDeployment deployment)
        {
            var model = new ExportFileDetailsModel
            {
                Id = deployment?.Id ?? profile.Id,
                IsForDeployment = deployment != null
            };

            try
            {
                var rootPath = (await Services.ApplicationContext.ContentRoot.GetDirectoryAsync(null)).PhysicalPath;

                // Add export files.
                var dir = await _exportProfileService.GetExportDirectoryAsync(profile, "Content");
                var zipFile = await dir.Parent.GetFileAsync(PathUtility.SanitizeFileName(dir.Parent.Name) + ".zip");
                var resultInfo = XmlHelper.Deserialize<DataExportResult>(profile.ResultInfo);

                if (deployment == null)
                {
                    AddFileInfo(model.ExportFiles, zipFile, rootPath);

                    if (resultInfo.Files != null)
                    {
                        foreach (var fi in resultInfo.Files)
                        {
                            AddFileInfo(model.ExportFiles, await dir.GetFileAsync(fi.FileName), rootPath, null, fi);
                        }
                    }
                }
                else if (deployment.DeploymentType == ExportDeploymentType.FileSystem)
                {
                    if (resultInfo.Files != null)
                    {
                        var deploymentDir = await _exportProfileService.GetDeploymentDirectoryAsync(deployment);
                        if (deploymentDir != null)
                        {
                            foreach (var fi in resultInfo.Files)
                            {
                                AddFileInfo(model.ExportFiles, await deploymentDir.GetFileAsync(fi.FileName), rootPath, null, fi);
                            }
                        }
                    }
                }

                // Add public files.
                var publicDeployment = deployment == null
                    ? profile.Deployments.FirstOrDefault(x => x.DeploymentType == ExportDeploymentType.PublicFolder)
                    : (deployment.DeploymentType == ExportDeploymentType.PublicFolder ? deployment : null);

                if (publicDeployment != null)
                {
                    var currentStore = Services.StoreContext.CurrentStore;
                    var deploymentDir = await _exportProfileService.GetDeploymentDirectoryAsync(publicDeployment);
                    if (deploymentDir != null)
                    {
                        // INFO: public folder is not cleaned up during export. We only have to show files that has been created during last export.
                        // Otherwise the merchant might publish URLs of old export files.
                        if (profile.CreateZipArchive)
                        {
                            var url = await _exportProfileService.GetDeploymentDirectoryUrlAsync(publicDeployment, currentStore);
                            AddFileInfo(model.PublicFiles, await deploymentDir.GetFileAsync(zipFile.Name), rootPath, url);
                        }
                        else if (resultInfo.Files != null)
                        {
                            var stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
                            foreach (var fi in resultInfo.Files)
                            {
                                stores.TryGetValue(fi.StoreId, out var store);

                                var url = await _exportProfileService.GetDeploymentDirectoryUrlAsync(publicDeployment, store ?? currentStore);
                                AddFileInfo(model.PublicFiles, await deploymentDir.GetFileAsync(fi.FileName), rootPath, url, fi, store);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return model;
        }

        private void AddFileInfo(
            List<ExportFileDetailsModel.FileInfo> fileInfos,
            IFile file,
            string rootPath,
            string publicUrl = null,
            DataExportResult.ExportFileInfo fileInfo = null,
            Store store = null)
        {
            if (!(file?.Exists ?? false))
                return;

            if (fileInfos.Any(x => x.File.Name == file.Name))
                return;

            var fi = new ExportFileDetailsModel.FileInfo
            {
                File = file,
                DisplayOrder = file.Extension.EqualsNoCase(".zip") ? 0 : 1,
                FileRootPath = file.PhysicalPath.Replace(rootPath, "~/").Replace('\\', '/')
            };

            if (fileInfo != null)
            {
                fi.RelatedType = fileInfo.RelatedType;

                if (fileInfo.Label.HasValue())
                {
                    fi.Label = fileInfo.Label;
                }
                else
                {
                    fi.Label = T("Admin.Common.Data");

                    if (fileInfo.RelatedType.HasValue)
                    {
                        fi.Label += " " + Services.Localization.GetLocalizedEnum(fileInfo.RelatedType.Value);
                    }
                }
            }

            if (store != null)
            {
                fi.StoreId = store.Id;
                fi.StoreName = store.Name;
            }

            if (publicUrl.HasValue())
            {
                fi.FileUrl = publicUrl + fi.File.Name;
            }

            fileInfos.Add(fi);
        }

        private async Task<(ExportProfile Profile, Provider<IExportProvider> Provider)> LoadProfileAndProvider(int profileId)
        {
            if (profileId != 0)
            {
                var profile = await _db.ExportProfiles
                    .Include(x => x.Deployments)
                    .Include(x => x.Task)
                    .ApplyStandardFilter()
                    .FirstOrDefaultAsync(x => x.Id == profileId);

                if (profile != null)
                {
                    var provider = _providerManager.GetProvider<IExportProvider>(profile.ProviderSystemName);
                    if (provider != null && !provider.Metadata.IsHidden)
                    {
                        return (profile, provider);
                    }
                }
            }

            return (null, null);
        }

        private string GetThumbnailUrl(Provider<IExportProvider> provider)
        {
            string url = null;

            if (provider != null)
                url = _moduleManager.GetIconUrl(provider.Metadata);

            if (url.IsEmpty())
                url = _moduleManager.GetDefaultIconUrl(null);

            url = Url.Content(url);

            return url;
        }

        #endregion
    }
}
