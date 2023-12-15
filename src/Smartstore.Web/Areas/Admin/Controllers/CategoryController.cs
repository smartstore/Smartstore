using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Scheduling;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CategoryController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IUrlService _urlService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IDiscountService _discountService;
        private readonly IRuleService _ruleService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly Lazy<ITaskStore> _taskStore;
        private readonly Lazy<ITaskScheduler> _taskScheduler;
        private readonly CatalogSettings _catalogSettings;

        public CategoryController(
            SmartDbContext db,
            IProductService productService,
            ICategoryService categoryService,
            IUrlService urlService,
            ILocalizedEntityService localizedEntityService,
            IDiscountService discountService,
            IRuleService ruleService,
            IStoreMappingService storeMappingService,
            IAclService aclService,
            Lazy<ITaskStore> taskStore,
            Lazy<ITaskScheduler> taskScheduler,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _productService = productService;
            _categoryService = categoryService;
            _urlService = urlService;
            _localizedEntityService = localizedEntityService;
            _discountService = discountService;
            _ruleService = ruleService;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
            _taskStore = taskStore;
            _taskScheduler = taskScheduler;
            _catalogSettings = catalogSettings;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available categories. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all categories as JSON.</returns>
        public async Task<IActionResult> AllCategories(string label, string selectedIds)
        {
            var categoryTree = await _categoryService.GetCategoryTreeAsync(includeHidden: true);
            var categories = categoryTree.Flatten(false);
            var selectedArr = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                categories = (new[] { new Category { Name = label, Id = 0 } }).Concat(categories);
            }

            var query = categories.SelectAwait(async c => new
            {
                id = c.Id.ToString(),
                text = await _categoryService.GetCategoryPathAsync(c, aliasPattern: "<span class='badge badge-secondary'>{0}</span>"),
                selected = selectedArr.Contains(c.Id)
            });

            // Call AsyncToList() to avoid upcoming conflicts with EF.
            var mainList = await query.AsyncToList();

            var mruList = new TrimmedBuffer<string>(
                Services.WorkContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories,
                _catalogSettings.MostRecentlyUsedCategoriesMaxSize)
                .Reverse()
                .Select(x =>
                {
                    var item = categoryTree.SelectNodeById(x.ToInt());
                    if (item != null)
                    {
                        return new
                        {
                            id = x,
                            text = _categoryService.GetCategoryPath(item, aliasPattern: "<span class='badge badge-secondary'>{0}</span>"),
                            selected = false
                        };
                    }

                    return null;
                })
                .Where(x => x != null)
                .ToList();

            object data = mainList;
            if (mruList.Count > 0)
            {
                data = new List<object>
                {
                    new Dictionary<string, object> { ["text"] = T("Common.Mru").Value, ["children"] = mruList },
                    new Dictionary<string, object> { ["text"] = T("Admin.Catalog.Categories").Value, ["children"] = mainList, ["main"] = true }
                };
            }

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            if (Services.WorkContext.CurrentCustomer.GenericAttributes.TryGetEntity("AdminCategoriesType", 0, out var categoriesType) &&
                categoriesType.Value.EqualsNoCase("Tree"))
            {
                return RedirectToAction("Tree");
            }

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Catalog.Category.Read)]
        public async Task<IActionResult> List()
        {
            await UpdateAdminCategoriesType("List");

            var model = new CategoryListModel();

            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View(model);
        }

        [Permission(Permissions.Catalog.Category.Read)]
        public async Task<IActionResult> Tree()
        {
            await UpdateAdminCategoriesType("Tree");

            var model = new CategoryListModel();

            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
            ViewBag.CanEdit = Services.Permissions.Authorize(Permissions.Catalog.Category.Update);

            return View(model);
        }

        [Permission(Permissions.Catalog.Category.Read)]
        public async Task<IActionResult> CategoryList(GridCommand command, CategoryListModel model)
        {
            var languageId = Services.WorkContext.WorkingLanguage.Id;
            var query = _db.Categories.AsNoTracking();

            if (model.SearchCategoryName.HasValue())
            {
                query = query.ApplySearchFilter(model.SearchCategoryName, LogicalRuleOperator.Or, x => x.Name, x => x.FullName);
            }
            if (model.SearchAlias.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Alias, model.SearchAlias);
            }

            var categories = await query
                .ApplyStandardFilter(true, null, model.SearchStoreId)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await categories.SelectAwait(async x => new CategoryModel
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Alias = x.Alias,
                Published = x.Published,
                DisplayOrder = x.DisplayOrder,
                LimitedToStores = x.LimitedToStores,
                ShowOnHomePage = x.ShowOnHomePage,
                EditUrl = Url.Action(nameof(Edit), "Category", new { id = x.Id, area = "Admin" }),
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(x.UpdatedOnUtc, DateTimeKind.Utc),
                Breadcrumb = await _categoryService.GetCategoryPathAsync(x, languageId, "<span class='badge badge-secondary'>{0}</span>")
            })
            .AsyncToList();

            return Json(new GridModel<CategoryModel>
            {
                Rows = rows,
                Total = categories.TotalCount
            });
        }

        [Permission(Permissions.Catalog.Category.Read)]
        public async Task<IActionResult> CategoryTree(int parentId = 0, int searchStoreId = 0)
        {
            var tree = await _categoryService.GetCategoryTreeAsync(parentId, searchStoreId == 0, searchStoreId);

            var nodes = tree.Children
                .Select(x =>
                {
                    var category = x.Value;

                    var nodeValue = new TreeItem
                    {
                        Name = category.Name,
                        NumChildren = x.Children.Count,
                        NumChildrenDeep = x.Flatten(false).Count(),
                        BadgeText = category.Alias,
                        Dimmed = !category.Published,
                        Url = Url.Action(nameof(Edit), "Category", new { id = category.Id })
                    };

                    return new TreeNode<TreeItem>(nodeValue, category.Id);
                })
                .ToList();

            return Json(new { nodes });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.Update)]
        public async Task<IActionResult> TreeDrop(int id, int targetId, string position)
        {
            // INFO: all entities needs to be tracked.
            var category = await _db.Categories.FindByIdAsync(id);
            var targetCategory = await _db.Categories.FindByIdAsync(targetId);

            if (category == null || targetCategory == null)
            {
                return Json(new { success = false, message = T("Admin.Common.ResourceNotFound").Value });
            }

            switch (position)
            {
                case "over":
                    category.ParentId = targetCategory.Id;
                    break;
                case "before":
                case "after":
                    category.ParentId = targetCategory.ParentId;
                    break;
            }

            // Re-calculate display orders.
            // INFO: apply the same sort order as when loading the tree.
            var tmpDisplayOrder = 0;
            var childCategories = await _db.Categories
                .Where(x => x.ParentId == category.ParentId)
                .ApplyStandardFilter(true, null, 0)
                .ToListAsync();

            foreach (var childCategory in childCategories)
            {
                childCategory.DisplayOrder = tmpDisplayOrder;
                tmpDisplayOrder += 10;
            }

            if (childCategories.Any())
            {
                switch (position)
                {
                    case "over":
                        category.DisplayOrder = childCategories.Max(x => x.DisplayOrder) + 10;
                        break;
                    case "before":
                        category.DisplayOrder = targetCategory.DisplayOrder - 5;
                        break;
                    case "after":
                        category.DisplayOrder = targetCategory.DisplayOrder + 5;
                        break;
                }
            }

            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Category.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CategoryModel
            {
                Published = true
            };

            AddLocales(model.Locales);
            await PrepareCategoryModel(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Category.Create)]
        public async Task<IActionResult> Create(CategoryModel model, bool continueEditing, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<CategoryModel, Category>();
                var category = await mapper.MapAsync(model);
                _db.Categories.Add(category);

                await _db.SaveChangesAsync();

                var slugResult = await _urlService.SaveSlugAsync(category, model.SeName, category.GetDisplayName(), true);
                model.SeName = slugResult.Slug;

                await ApplyLocales(model, category);

                await _discountService.ApplyDiscountsAsync(category, model?.SelectedDiscountIds, DiscountType.AssignedToCategories);
                await _ruleService.ApplyRuleSetMappingsAsync(category, model.SelectedRuleSetIds);
                await _storeMappingService.ApplyStoreMappingsAsync(category, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(category, model.SelectedCustomerRoleIds);

                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, category, form));

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewCategory, T("ActivityLog.AddNewCategory"), category.Name);
                NotifySuccess(T("Admin.Catalog.Categories.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = category.Id })
                    : RedirectToAction(nameof(Index));
            }

            await PrepareCategoryModel(model, null);

            return View(model);
        }

        [Permission(Permissions.Catalog.Category.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories
                .AsSplitQuery()
                .Include(x => x.AppliedDiscounts)
                .Include(x => x.RuleSets)
                .FindByIdAsync(id, false);

            if (category == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<Category, CategoryModel>();
            var model = await mapper.MapAsync(category);

            await AddLocalesAsync(model.Locales, async (locale, languageId) =>
            {
                locale.Name = category.GetLocalized(x => x.Name, languageId, false, false);
                locale.FullName = category.GetLocalized(x => x.FullName, languageId, false, false);
                locale.Description = category.GetLocalized(x => x.Description, languageId, false, false);
                locale.BottomDescription = category.GetLocalized(x => x.BottomDescription, languageId, false, false);
                locale.BadgeText = category.GetLocalized(x => x.BadgeText, languageId, false, false);
                locale.MetaKeywords = category.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = category.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = category.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await category.GetActiveSlugAsync(languageId, false, false);
            });

            await PrepareCategoryModel(model, category);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Category.Update)]
        public async Task<IActionResult> Edit(CategoryModel model, bool continueEditing, IFormCollection form)
        {
            var category = await _db.Categories
                .AsSplitQuery()
                .Include(x => x.AppliedDiscounts)
                .Include(x => x.RuleSets)
                .FindByIdAsync(model.Id);

            if (category == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<CategoryModel, Category>();
                await mapper.MapAsync(model, category);
                category.ParentId = model.ParentCategoryId;

                var slugResult = await _urlService.SaveSlugAsync(category, model.SeName, category.GetDisplayName(), true);
                model.SeName = slugResult.Slug;

                await ApplyLocales(model, category);
                await _discountService.ApplyDiscountsAsync(category, model?.SelectedDiscountIds, DiscountType.AssignedToCategories);
                await _ruleService.ApplyRuleSetMappingsAsync(category, model.SelectedRuleSetIds);
                await _storeMappingService.ApplyStoreMappingsAsync(category, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(category, model.SelectedCustomerRoleIds);

                _db.Categories.Update(category);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, category, form));

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditCategory, T("ActivityLog.EditCategory"), category.Name);
                NotifySuccess(T("Admin.Catalog.Categories.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), category.Id)
                    : RedirectToAction(nameof(Index));
            }

            await PrepareCategoryModel(model, category);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.Delete)]
        public async Task<IActionResult> Delete(int id, string deleteType)
        {
            var category = await _db.Categories.FindByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            await _categoryService.DeleteCategoryAsync(category, deleteType.EqualsNoCase("deletechilds"));

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteCategory, T("ActivityLog.DeleteCategory"), category.Name);
            NotifySuccess(T("Admin.Catalog.Categories.Deleted"));

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ActionName("Edit"), FormValueRequired("inherit-acl-into-children")]
        [Permission(Permissions.Catalog.Category.Update)]
        public async Task<IActionResult> InheritAclIntoChildren(CategoryModel model)
        {
            await _categoryService.InheritAclIntoChildrenAsync(model.Id, false, true, false);

            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }

        [HttpPost]
        [ActionName("Edit"), FormValueRequired("inherit-stores-into-children")]
        [Permission(Permissions.Catalog.Category.Update)]
        public async Task<IActionResult> InheritStoresIntoChildren(CategoryModel model)
        {
            await _categoryService.InheritStoresIntoChildrenAsync(model.Id, false, true, false);

            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }

        #region Product categories

        [Permission(Permissions.Catalog.Category.Read)]
        public async Task<IActionResult> ProductCategoryList(GridCommand command, int categoryId)
        {
            var productCategories = await _db.ProductCategories
                .AsNoTracking()
                .ApplyCategoryFilter(categoryId)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = productCategories.Select(x =>
            {
                var product = x.Product;
                var model = MiniMapper.Map<ProductCategory, CategoryProductModel>(x);

                model.ProductName = product.GetLocalized(x => x.Name);
                model.Sku = product.Sku;
                model.ProductTypeName = product.GetProductTypeLabel(Services.Localization);
                model.ProductTypeLabelHint = product.ProductTypeLabelHint;
                model.Published = product.Published;
                model.EditUrl = Url.Action("Edit", "Product", new { id = x.ProductId, area = "Admin" });

                return model;
            })
            .ToList();

            return Json(new GridModel<CategoryProductModel>
            {
                Rows = rows,
                Total = productCategories.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.EditProduct)]
        public async Task<IActionResult> ProductCategoryInsert(CategoryProductModel model, int categoryId)
        {
            var success = false;

            if (!await _db.ProductCategories.AnyAsync(x => x.CategoryId == categoryId && x.ProductId == model.ProductId))
            {
                _db.ProductCategories.Add(new ProductCategory
                {
                    CategoryId = categoryId,
                    ProductId = model.ProductId,
                    IsFeaturedProduct = model.IsFeaturedProduct,
                    DisplayOrder = model.DisplayOrder
                });

                await _db.SaveChangesAsync();
                success = true;
            }
            else
            {
                NotifyError(T("Admin.Catalog.Categories.Products.NoDuplicatesAllowed"));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.EditProduct)]
        public async Task<IActionResult> ProductCategoryUpdate(CategoryProductModel model)
        {
            var success = false;

            var productCategory = await _db.ProductCategories.FindByIdAsync(model.Id);
            if (productCategory != null)
            {
                if (model.ProductId != productCategory.ProductId &&
                    await _db.ProductCategories.AnyAsync(x => x.CategoryId == model.CategoryId && x.ProductId == model.ProductId))
                {
                    NotifyError(T("Admin.Catalog.Categories.Products.NoDuplicatesAllowed"));
                }
                else
                {
                    productCategory.ProductId = model.ProductId;
                    productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
                    productCategory.DisplayOrder = model.DisplayOrder;

                    await _db.SaveChangesAsync();
                    success = true;
                }
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.EditProduct)]
        public async Task<IActionResult> ProductCategoryDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var productCategories = await _db.ProductCategories.GetManyAsync(ids, true);

                _db.ProductCategories.RemoveRange(productCategories);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.Update)]
        public async Task<IActionResult> ApplyRules(int id)
        {
            var category = await _db.Categories.FindByIdAsync(id, false);
            if (category == null)
            {
                return NotFound();
            }

            var task = await _taskStore.Value.GetTaskByTypeAsync<ProductRuleEvaluatorTask>();
            if (task != null)
            {
                _ = _taskScheduler.Value.RunSingleTaskAsync(task.Id, new Dictionary<string, string>
                {
                    { "CategoryIds", category.Id.ToString() }
                });

                NotifyInfo(T("Admin.System.ScheduleTasks.RunNow.Progress"));
            }
            else
            {
                NotifyError(T("Admin.System.ScheduleTasks.TaskNotFound", nameof(ProductRuleEvaluatorTask)));
            }

            return RedirectToAction(nameof(Edit), new { id = category.Id });
        }

        #endregion

        private async Task PrepareCategoryModel(CategoryModel model, Category category)
        {
            if (category != null)
            {
                model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(category.UpdatedOnUtc, DateTimeKind.Utc);
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(category.CreatedOnUtc, DateTimeKind.Utc);
                model.SelectedDiscountIds = category.AppliedDiscounts.Select(x => x.Id).ToArray();
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(category);
                model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(category);
                model.SelectedRuleSetIds = category.RuleSets.Select(x => x.Id).ToArray();
                model.CategoryUrl = await GetEntityPublicUrlAsync(category);

                var showRuleApplyButton = model.SelectedRuleSetIds.Any();

                if (!showRuleApplyButton)
                {
                    // Ignore deleted categories.
                    showRuleApplyButton = await _db.ProductCategories
                        .AsNoTracking()
                        .ApplyCategoryFilter(category.Id, true)
                        .AnyAsync();
                }

                ViewBag.ShowRuleApplyButton = showRuleApplyButton;
            }

            var parentCategoryBreadcrumb = string.Empty;
            if (model.ParentCategoryId.HasValue)
            {
                var parentCategory = await _categoryService.GetCategoryTreeAsync(model.ParentCategoryId.Value, true);
                if (parentCategory != null)
                {
                    parentCategoryBreadcrumb = _categoryService.GetCategoryPath(parentCategory);
                }
                else
                {
                    model.ParentCategoryId = null;
                }
            }

            ViewBag.ParentCategoryBreadcrumb = parentCategoryBreadcrumb;

            var categoryTemplates = await _db.CategoryTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.CategoryTemplates = categoryTemplates
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.DefaultViewModes = new List<SelectListItem>
            {
                new SelectListItem { Value = "grid", Text = T("Common.Grid"), Selected = model.DefaultViewMode.EqualsNoCase("grid") },
                new SelectListItem { Value = "list", Text = T("Common.List"), Selected = model.DefaultViewMode.EqualsNoCase("list") }
            };
        }

        private async Task ApplyLocales(CategoryModel model, Category category)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.FullName, localized.FullName, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.Description, localized.Description, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.BottomDescription, localized.BottomDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.BadgeText, localized.BadgeText, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(category, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);

                await _urlService.SaveSlugAsync(category, localized.SeName, localized.Name, false, localized.LanguageId);
            }
        }

        private Task UpdateAdminCategoriesType(string value)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            customer.GenericAttributes.TryGetEntity("AdminCategoriesType", 0, out var categoriesType);

            if (categoriesType == null || !categoriesType.Value.EqualsNoCase(value))
            {
                customer.GenericAttributes.Set("AdminCategoriesType", value);
                return _db.SaveChangesAsync();
            }

            return Task.CompletedTask;
        }
    }
}
