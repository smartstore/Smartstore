using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Controllers
{
    public partial class EntityController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;
        private readonly IMediaService _mediaService;
        private readonly ICategoryService _categoryService;

        public EntityController(
            SmartDbContext db,
            ICatalogSearchService catalogSearchService,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            SearchSettings searchSettings,
            IMediaService mediaService,
            ICategoryService categoryService)
        {
            _db = db;
            _catalogSearchService = catalogSearchService;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
            _mediaService = mediaService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Picker(EntityPickerModel model)
        {
            if (model.EntityType.EqualsNoCase("product"))
            {
                ViewBag.AvailableCategories = (await _categoryService.GetCategoryTreeAsync(includeHidden: true))
                    .FlattenNodes(false)
                    .Select(x => new SelectListItem { Text = x.GetCategoryNameIndented(), Value = x.Id.ToString() })
                    .ToList();

                var manufacturers = await _db.Manufacturers
                    .ApplyStandardFilter(includeHidden: true)
                    .ToListAsync();

                ViewBag.AvailableManufacturers = manufacturers
                    .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                    .ToList();

                ViewBag.AvailableStores = Services.StoreContext.GetAllStores()
                    .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                    .ToList();

                ViewBag.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();
            }
            else if (model.EntityType.EqualsNoCase("customer"))
            {
                ViewBag.AvailableCustomerSearchTypes = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Name", Value = "Name", Selected = true },
                    new SelectListItem { Text = "Email", Value = "Email" }
                };

                if (Services.Settings.GetSettingByKey<CustomerNumberMethod>("CustomerSettings.CustomerNumberMethod") != CustomerNumberMethod.Disabled)
                {
                    ViewBag.AvailableCustomerSearchTypes.Add(new SelectListItem { Text = T("Account.Fields.CustomerNumber"), Value = "CustomerNumber" });
                }
            }

            return PartialView(model);
        }

        [HttpPost]
        [ActionName("Picker")]
        public async Task<IActionResult> PickerPost(EntityPickerModel model)
        {
            try
            {
                var form = Request.Form;
                var languageId = model.LanguageId == 0 ? Services.WorkContext.WorkingLanguage.Id : model.LanguageId;
                var disableIf = model.DisableIf.SplitSafe(",").Select(x => x.ToLower().Trim()).ToList();
                var disableIds = model.DisableIds.SplitSafe(",").Select(x => x.ToInt()).ToList();
                var selected = model.Selected.SplitSafe(",");
                var returnSku = model.ReturnField.EqualsNoCase("sku");

                if (model.EntityType.EqualsNoCase("product"))
                {
                    model.SearchTerm = model.SearchTerm.TrimSafe();

                    var hasPermission = await Services.Permissions.AuthorizeAsync(Permissions.Catalog.Product.Read);
                    var disableIfNotSimpleProduct = disableIf.Contains("notsimpleproduct");
                    var disableIfGroupedProduct = disableIf.Contains("groupedproduct");
                    var labelTextGrouped = T("Admin.Catalog.Products.ProductType.GroupedProduct.Label");
                    var labelTextBundled = T("Admin.Catalog.Products.ProductType.BundledProduct.Label");
                    var sku = T("Products.Sku");

                    var fields = new List<string> { "name" };
                    if (_searchSettings.SearchFields.Contains("sku"))
                    {
                        fields.Add("sku");
                    }    
                    if (_searchSettings.SearchFields.Contains("shortdescription"))
                    {
                        fields.Add("shortdescription");
                    }
                        
                    var searchQuery = new CatalogSearchQuery(fields.ToArray(), model.SearchTerm)
                        .HasStoreId(model.StoreId);

                    if (!hasPermission)
                    {
                        searchQuery = searchQuery.VisibleOnly(Services.WorkContext.CurrentCustomer);
                    }

                    if (model.ProductTypeId > 0)
                    {
                        searchQuery = searchQuery.IsProductType((ProductType)model.ProductTypeId);
                    }

                    if (model.ManufacturerId != 0)
                    {
                        searchQuery = searchQuery.WithManufacturerIds(null, model.ManufacturerId);
                    }

                    if (model.CategoryId != 0)
                    {
                        var node = await _categoryService.GetCategoryTreeAsync(model.CategoryId, true);
                        if (node != null)
                        {
                            searchQuery = searchQuery.WithCategoryIds(null, node.Flatten(true).Select(x => x.Id).ToArray());
                        }
                    }

                    List<EntityPickerProduct> products;
                    var skip = model.PageIndex * model.PageSize;

                    if (_searchSettings.UseCatalogSearchInBackend)
                    {
                        searchQuery = searchQuery
                            .Slice(skip, model.PageSize)
                            .SortBy(ProductSortingEnum.NameAsc);

                        var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                        products = (await searchResult.GetHitsAsync())
                            .Select(x => new EntityPickerProduct
                            {
                                Id = x.Id,
                                Sku = x.Sku,
                                Name = x.Name,
                                Published = x.Published,
                                ProductTypeId = x.ProductTypeId,
                                MainPictureId = x.MainPictureId
                            })
                            .ToList();
                    }
                    else
                    {
                        var query = _catalogSearchService.PrepareQuery(searchQuery);

                        products = query
                            .Select(x => new EntityPickerProduct
                            {
                                Id = x.Id,
                                Sku = x.Sku,
                                Name = x.Name,
                                Published = x.Published,
                                ProductTypeId = x.ProductTypeId,
                                MainPictureId = x.MainPictureId
                            })
                            .OrderBy(x => x.Name)
                            .Skip(skip)
                            .Take(model.PageSize)
                            .ToList();
                    }

                    var fileIds = products
                        .Select(x => x.MainPictureId ?? 0)
                        .Where(x => x != 0)
                        .Distinct()
                        .ToArray();

                    var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

                    model.SearchResult = products
                        .Select(x =>
                        {
                            var item = new EntityPickerModel.SearchResultModel
                            {
                                Id = x.Id,
                                Title = x.Name,
                                Summary = x.Sku,
                                SummaryTitle = $"{sku}: {x.Sku.NaIfEmpty()}",
                                Published = hasPermission ? x.Published : null,
                                ReturnValue = returnSku ? x.Sku : x.Id.ToString()
                            };

                            item.Selected = selected.Contains(item.ReturnValue);

                            if (disableIfNotSimpleProduct)
                            {
                                item.Disable = x.ProductTypeId != (int)ProductType.SimpleProduct;
                            }
                            else if (disableIfGroupedProduct)
                            {
                                item.Disable = x.ProductTypeId == (int)ProductType.GroupedProduct;
                            }

                            if (!item.Disable && disableIds.Contains(x.Id))
                            {
                                item.Disable = true;
                            }

                            if (x.ProductTypeId == (int)ProductType.GroupedProduct)
                            {
                                item.LabelText = labelTextGrouped;
                                item.LabelClassName = "badge-success";
                            }
                            else if (x.ProductTypeId == (int)ProductType.BundledProduct)
                            {
                                item.LabelText = labelTextBundled;
                                item.LabelClassName = "badge-info";
                            }

                            files.TryGetValue(x.MainPictureId ?? 0, out var file);
                            item.ImageUrl = _mediaService.GetUrl(file, _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage, null, !_catalogSettings.HideProductDefaultPictures);

                            return item;
                        })
                        .ToList();
                }
                else if (model.EntityType.EqualsNoCase("category"))
                {
                    var categories = await _db.Categories
                        .ApplyStandardFilter(includeHidden: true)
                        .Where(c => c.Name.Contains(model.SearchTerm) || c.FullName.Contains(model.SearchTerm))
                        .ToListAsync();
                    
                    var fileIds = categories
                        .Select(x => x.MediaFileId ?? 0)
                        .Where(x => x != 0)
                        .Distinct()
                        .ToArray();

                    var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

                    model.SearchResult = await categories
                        .SelectAsync(async x =>
                        {
                            var path = await _categoryService.GetCategoryPathAsync(x, languageId, "({0})");
                            var item = new EntityPickerModel.SearchResultModel
                            {
                                Id = x.Id,
                                Title = x.Name,
                                Summary = path,
                                SummaryTitle = path,
                                Published = x.Published,
                                ReturnValue = x.Id.ToString(),
                                Selected = selected.Contains(x.Id.ToString()),
                                Disable = disableIds.Contains(x.Id)
                            };

                            if (x.Alias.HasValue())
                            {
                                item.LabelText = x.Alias;
                                item.LabelClassName = "badge-secondary";
                            }

                            files.TryGetValue(x.MediaFileId ?? 0, out var file);
                            item.ImageUrl = _mediaService.GetUrl(file, _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage, null, !_catalogSettings.HideProductDefaultPictures);

                            return item;
                        }).AsyncToList();
                }
                else if (model.EntityType.EqualsNoCase("manufacturer"))
                {
                    var manufacturers = await _db.Manufacturers
                        .ApplyStandardFilter(includeHidden: true)
                        .ApplyPaging(model.PageIndex, model.PageSize)
                        .Where(c => c.Name.Contains(model.SearchTerm))
                        .ToListAsync();

                    var fileIds = manufacturers
                        .Select(x => x.MediaFileId ?? 0)
                        .Where(x => x != 0)
                        .Distinct()
                        .ToArray();

                    var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

                    model.SearchResult = manufacturers
                        .Select(x =>
                        {
                            var item = new EntityPickerModel.SearchResultModel
                            {
                                Id = x.Id,
                                Title = x.Name,
                                Published = x.Published,
                                ReturnValue = x.Id.ToString(),
                                Selected = selected.Contains(x.Id.ToString()),
                                Disable = disableIds.Contains(x.Id)
                            };

                            files.TryGetValue(x.MediaFileId ?? 0, out var file);
                            item.ImageUrl = _mediaService.GetUrl(file, _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage, null, !_catalogSettings.HideProductDefaultPictures);

                            return item;
                        })
                        .ToList();
                }
                else if (model.EntityType.EqualsNoCase("customer"))
                {
                    var registeredRole = _db.CustomerRoles
                        .AsNoTracking()
                        .Where(x => x.SystemName == SystemCustomerRoleNames.Registered)
                        .FirstOrDefault();

                    var registeredRoleId = registeredRole.Id;
                    var searchTermName = string.Empty;
                    var searchTermEmail = string.Empty;
                    var searchTermCustomerNumber = string.Empty;

                    if (model.CustomerSearchType.EqualsNoCase("Name"))
                    {
                        searchTermName = model.SearchTerm;
                    }
                    else if (model.CustomerSearchType.EqualsNoCase("Email"))
                    {
                        searchTermEmail = model.SearchTerm;
                    }
                    else if (model.CustomerSearchType.EqualsNoCase("CustomerNumber"))
                    {
                        searchTermCustomerNumber = model.SearchTerm;
                    }

                    var customers = await _db.Customers
                        .ApplySearchTermFilter(searchTermName)
                        .ApplyIdentFilter(searchTermEmail, searchTermEmail, searchTermCustomerNumber)
                        .ApplyRolesFilter(new[] { registeredRoleId })
                        .ApplyPaging(model.PageIndex, model.PageSize)
                        .ToListAsync();

                    model.SearchResult = customers
                        .Select(x =>
                        {
                            var fullName = x.GetFullName();

                            var item = new EntityPickerModel.SearchResultModel
                            {
                                Id = x.Id,
                                ReturnValue = x.Id.ToString(),
                                Title = x.Username.NullEmpty() ?? x.Email,
                                Summary = fullName,
                                SummaryTitle = fullName,
                                Published = true,
                                Selected = selected.Contains(x.Id.ToString()),
                                Disable = disableIds.Contains(x.Id)
                            };

                            return item;
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex.ToAllMessages());
            }

            return PartialView("Picker.List", model);
        }
    }
}
