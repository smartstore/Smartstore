using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminController
    {
        #region Related products

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> RelatedProductList(GridCommand command, int productId)
        {
            var relatedProducts = await _db.RelatedProducts
                .AsNoTracking()
                .ApplyProductId1Filter(productId, true)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var productIds2 = relatedProducts.ToDistinctArray(x => x.ProductId2);
            var products2 = await _db.Products
                .AsNoTracking()
                .Where(x => productIds2.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            var rows = relatedProducts
                .Select(x =>
                {
                    var product2 = products2[x.ProductId2];

                    return new ProductModel.RelatedProductModel
                    {
                        Id = x.Id,
                        ProductId2 = x.ProductId2,
                        Product2Name = product2.Name,
                        ProductTypeName = product2.GetProductTypeLabel(Services.Localization),
                        ProductTypeLabelHint = product2.ProductTypeLabelHint,
                        DisplayOrder = x.DisplayOrder,
                        Product2Sku = product2.Sku,
                        Product2Published = product2.Published,
                        EditUrl = Url.Action(nameof(ProductController.Edit), "Product", new { id = x.ProductId2 })
                    };
                })
                .ToList();

            return Json(new GridModel<ProductModel.RelatedProductModel>
            {
                Rows = rows,
                Total = await relatedProducts.GetTotalCountAsync()
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> RelatedProductUpdate(ProductModel.RelatedProductModel model)
        {
            var relatedProduct = await _db.RelatedProducts.FindByIdAsync(model.Id);
            relatedProduct.DisplayOrder = model.DisplayOrder;

            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public async Task<IActionResult> RelatedProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.RelatedProducts.GetManyAsync(ids);
                _db.RelatedProducts.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> RelatedProductAdd(int productId, int[] selectedProductIds)
        {
            RelatedProduct relation = null;
            var maxDisplayOrder = -1;

            var products = await _db.Products
                .AsNoTracking()
                .Where(x => selectedProductIds.Contains(x.Id))
                .ApplyStandardFilter(true)
                .ToListAsync();

            var existingRelations = await _db.RelatedProducts
                .ApplyProductId1Filter(productId, true)
                .ToListAsync();

            foreach (var product in products)
            {
                if (!existingRelations.Any(x => x.ProductId1 == productId && x.ProductId2 == product.Id))
                {
                    if (maxDisplayOrder == -1 && (relation = existingRelations.OrderByDescending(x => x.DisplayOrder).FirstOrDefault()) != null)
                    {
                        maxDisplayOrder = relation.DisplayOrder;
                    }

                    var relatedProduct = new RelatedProduct
                    {
                        ProductId1 = productId,
                        ProductId2 = product.Id,
                        DisplayOrder = ++maxDisplayOrder
                    };

                    _db.RelatedProducts.Add(relatedProduct);
                    existingRelations.Add(relatedProduct);
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CreateAllMutuallyRelatedProducts(int productId)
        {
            string message = null;
            var product = await _db.Products.FindByIdAsync(productId, false);

            if (product != null)
            {
                var count = await _productService.EnsureMutuallyRelatedProductsAsync(productId);
                message = T("Admin.Common.CreateMutuallyAssociationsResult", count);
            }

            return new JsonResult(new { Message = message });
        }

        #endregion

        #region Cross-sell products

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> CrossSellProductList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.CrossSellProductModel>();
            var crossSellProducts = await _db.CrossSellProducts
                .AsNoTracking()
                .ApplyProductId1Filter(productId, true)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var productIds2 = crossSellProducts.ToDistinctArray(x => x.ProductId2);
            var products2 = await _db.Products
                .AsNoTracking()
                .Where(x => productIds2.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            var crossSellProductsModel = crossSellProducts
                .Select(x =>
                {
                    var product2 = products2[x.ProductId2];

                    return new ProductModel.CrossSellProductModel
                    {
                        Id = x.Id,
                        ProductId2 = x.ProductId2,
                        Product2Name = product2.Name,
                        ProductTypeName = product2.GetProductTypeLabel(Services.Localization),
                        ProductTypeLabelHint = product2.ProductTypeLabelHint,
                        Product2Sku = product2.Sku,
                        Product2Published = product2.Published,
                        EditUrl = Url.Action(nameof(ProductController.Edit), "Product", new { id = x.ProductId2 })
                    };
                })
                .ToList();

            model.Rows = crossSellProductsModel;
            model.Total = await crossSellProducts.GetTotalCountAsync();

            return Json(model);
        }

        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CrossSellProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.CrossSellProducts.GetManyAsync(ids);
                _db.CrossSellProducts.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CrossSellProductAdd(int productId, int[] selectedProductIds)
        {
            var products = await _db.Products
                .AsNoTracking()
                .Where(x => selectedProductIds.Contains(x.Id))
                .ToListAsync();

            var existingRelations = await _db.CrossSellProducts
                .ApplyProductId1Filter(productId, true)
                .ToListAsync();

            foreach (var product in products.OrderBySequence(selectedProductIds))
            {
                if (!existingRelations.Any(x => x.ProductId1 == productId && x.ProductId2 == product.Id))
                {
                    var crossSellProduct = new CrossSellProduct
                    {
                        ProductId1 = productId,
                        ProductId2 = product.Id
                    };

                    _db.CrossSellProducts.Add(crossSellProduct);
                    existingRelations.Add(crossSellProduct);
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CreateAllMutuallyCrossSellProducts(int productId)
        {
            string message = null;
            var product = await _db.Products.FindByIdAsync(productId, false);
            if (product != null)
            {
                var count = await _productService.EnsureMutuallyCrossSellProductsAsync(productId);
                message = T("Admin.Common.CreateMutuallyAssociationsResult", count);
            }

            return new JsonResult(new { Message = message });
        }

        #endregion

        #region Associated products

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> AssociatedProductList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.AssociatedProductModel>();
            var searchQuery = new CatalogSearchQuery().HasParentGroupedProduct(productId);
            var query = _catalogSearchService.Value.PrepareQuery(searchQuery);
            var associatedProducts = await query
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var associatedProductsModel = associatedProducts.Select(x =>
            {
                return new ProductModel.AssociatedProductModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProductTypeName = x.GetProductTypeLabel(Services.Localization),
                    ProductTypeLabelHint = x.ProductTypeLabelHint,
                    DisplayOrder = x.DisplayOrder,
                    Sku = x.Sku,
                    Published = x.Published,
                    EditUrl = Url.Action("Edit", "Product", new { id = x.Id })
                };
            })
            .ToList();

            model.Rows = associatedProductsModel;
            model.Total = await associatedProducts.GetTotalCountAsync();

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAssociatedProduct)]
        public async Task<IActionResult> AssociatedProductUpdate(ProductModel.RelatedProductModel model)
        {
            var relatedProduct = await _db.Products.FindByIdAsync(model.Id);
            relatedProduct.DisplayOrder = model.DisplayOrder;

            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [Permission(Permissions.Catalog.Product.EditAssociatedProduct)]
        public async Task<IActionResult> AssociatedProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var products = await _db.Products.GetManyAsync(ids, true);
                products.Each(x => x.ParentGroupedProductId = 0);

                await _db.SaveChangesAsync();
                numDeleted = products.Count;
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAssociatedProduct)]
        public async Task<IActionResult> AssociatedProductAdd(int productId, int[] selectedProductIds)
        {
            var searchQuery = new CatalogSearchQuery().HasParentGroupedProduct(productId);
            var query = _catalogSearchService.Value.PrepareQuery(searchQuery);
            var maxDisplayOrder = query
                .Select(x => x.DisplayOrder)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var products = await _db.Products
                .AsQueryable()
                .Where(x => selectedProductIds.Contains(x.Id) && x.Id != productId && x.ProductTypeId != (int)ProductType.GroupedProduct)
                .ToListAsync();

            foreach (var product in products)
            {
                if (product.ParentGroupedProductId != productId)
                {
                    product.ParentGroupedProductId = productId;
                    product.DisplayOrder = ++maxDisplayOrder;
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        #endregion

        #region Bundle items

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> BundleItemList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.BundleItemModel>();
            var bundleItems = await _db.ProductBundleItem
                .AsNoTracking()
                .ApplyBundledProductsFilter(new[] { productId }, true)
                .Include(x => x.Product)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var bundleItemsModel = bundleItems.Select(x =>
            {
                return new ProductModel.BundleItemModel
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.Name,
                    ProductTypeName = x.Product.GetProductTypeLabel(Services.Localization),
                    ProductTypeLabelHint = x.Product.ProductTypeLabelHint,
                    ProductEditUrl = Url.Action(nameof(ProductController.Edit), "Product", new { id = x.Product.Id, area = "Admin" }),
                    Sku = x.Product.Sku,
                    Quantity = x.Quantity,
                    Discount = x.Discount,
                    DisplayOrder = x.DisplayOrder,
                    Visible = x.Visible,
                    Published = x.Published
                };
            }).ToList();

            model.Rows = bundleItemsModel;
            model.Total = await bundleItems.GetTotalCountAsync();

            return Json(model);
        }

        [Permission(Permissions.Catalog.Product.EditBundle)]
        public async Task<IActionResult> BundleItemDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductBundleItem.GetManyAsync(ids);
                _db.ProductBundleItem.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditBundle)]
        public async Task<IActionResult> BundleItemAdd(int productId, int[] selectedProductIds)
        {
            var products = await _db.Products.GetManyAsync(selectedProductIds, true);

            var maxDisplayOrder = await _db.ProductBundleItem
                .AsNoTracking()
                .ApplyBundledProductsFilter(new[] { productId }, true)
                .OrderByDescending(x => x.DisplayOrder)
                .Select(x => x.DisplayOrder)
                .FirstOrDefaultAsync();

            foreach (var product in products.Where(x => x.CanBeBundleItem()))
            {
                var attributes = await _db.ProductVariantAttributes
                    .ApplyProductFilter(new[] { product.Id })
                    .ToListAsync();

                if (attributes.Count > 0 && attributes.Any(a => a.ProductVariantAttributeValues.Any(v => v.ValueType == ProductVariantAttributeValueType.ProductLinkage)))
                {
                    NotifyError(T("Admin.Catalog.Products.BundleItems.NoAttributeWithProductLinkage"));
                }
                else
                {
                    var bundleItem = new ProductBundleItem
                    {
                        ProductId = product.Id,
                        BundleProductId = productId,
                        Quantity = 1,
                        Visible = true,
                        Published = true,
                        DisplayOrder = ++maxDisplayOrder
                    };

                    _db.ProductBundleItem.Add(bundleItem);
                }
            }

            var num = await _db.SaveChangesAsync();

            if (products.Any(x => !x.CanBeBundleItem()))
            {
                Services.Notifier.Add(num > 0 ? NotifyType.Warning : NotifyType.Error, T("Admin.Catalog.Products.BundleItems.CanBeBundleItemWarning"));
            }

            return new EmptyResult();
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> BundleItemEditPopup(int id, string btnId, string formId)
        {
            var bundleItem = await _db.ProductBundleItem
                .Include(x => x.BundleProduct)
                .Include(x => x.Product)
                .Include(x => x.AttributeFilters)
                .FindByIdAsync(id, false);

            if (bundleItem == null)
            {
                throw new ArgumentException("No bundle item found with the specified id");
            }

            var model = await MapperFactory.MapAsync<ProductBundleItem, ProductBundleItemModel>(bundleItem);
            await PrepareBundleItemEditModelAsync(model, bundleItem, btnId, formId);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Product.EditBundle)]
        public async Task<IActionResult> BundleItemEditPopup(string btnId, string formId, bool continueEditing, ProductBundleItemModel model)
        {
            ViewBag.CloseWindow = !continueEditing;

            if (ModelState.IsValid)
            {
                var bundleItem = await _db.ProductBundleItem
                    .Include(x => x.BundleProduct)
                    .Include(x => x.Product)
                    .Include(x => x.AttributeFilters)
                    .FindByIdAsync(model.Id);

                if (bundleItem == null)
                {
                    throw new ArgumentException("No bundle item found with the specified id");
                }

                await MapperFactory.MapAsync(model, bundleItem);
                await _db.SaveChangesAsync();

                foreach (var localized in model.Locales)
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(bundleItem, x => x.Name, localized.Name, localized.LanguageId);
                    await _localizedEntityService.ApplyLocalizedValueAsync(bundleItem, x => x.ShortDescription, localized.ShortDescription, localized.LanguageId);
                }

                if (bundleItem.FilterAttributes)
                {
                    // Only update filters if attribute filtering is activated to reduce payload.
                    await SaveFilteredAttributesAsync(bundleItem);
                }

                await PrepareBundleItemEditModelAsync(model, bundleItem, btnId, formId, true);

                if (continueEditing)
                {
                    NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));
                }
            }
            else
            {
                await PrepareBundleItemEditModelAsync(model, null, btnId, formId);
            }

            return View(model);
        }

        #endregion
    }
}
