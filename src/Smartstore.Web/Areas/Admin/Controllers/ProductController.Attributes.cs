using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminController
    {
        #region Product specification attributes

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductSpecAttrList(GridCommand command, int productId)
        {
            var productSpecAttributes = await _db.ProductSpecificationAttributes
                .AsNoTracking()
                .Include(x => x.SpecificationAttributeOption)
                .ThenInclude(x => x.SpecificationAttribute)
                .ApplyProductsFilter(new[] { productId })
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = productSpecAttributes.Select(x =>
            {
                var attributeId = x.SpecificationAttributeOption.SpecificationAttributeId;
                var psaModel = new ProductSpecificationAttributeModel
                {
                    Id = x.Id,
                    SpecificationAttributeName = x.SpecificationAttributeOption.SpecificationAttribute.Name,
                    SpecificationAttributeOptionName = x.SpecificationAttributeOption.Name,
                    SpecificationAttributeId = attributeId,
                    SpecificationAttributeOptionId = x.SpecificationAttributeOptionId,
                    AllowFiltering = x.AllowFiltering,
                    ShowOnProductPage = x.ShowOnProductPage,
                    DisplayOrder = x.DisplayOrder,
                    SpecificationAttributeOptionsUrl = Url.Action("GetOptionsByAttributeId", "SpecificationAttribute", new { attributeId })
                };

                return psaModel;
            })
            .ToList();

            return Json(new GridModel<ProductSpecificationAttributeModel>
            {
                Rows = rows,
                Total = await productSpecAttributes.GetTotalCountAsync()
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public async Task<IActionResult> ProductSpecificationAttributeAdd(
            int specificationAttributeOptionId,
            bool? allowFiltering,
            bool? showOnProductPage,
            int displayOrder,
            int productId)
        {
            var success = false;
            var message = string.Empty;

            if (specificationAttributeOptionId != 0)
            {
                var psa = new ProductSpecificationAttribute
                {
                    SpecificationAttributeOptionId = specificationAttributeOptionId,
                    ProductId = productId,
                    AllowFiltering = allowFiltering,
                    ShowOnProductPage = showOnProductPage,
                    DisplayOrder = displayOrder,
                };

                try
                {
                    _db.ProductSpecificationAttributes.Add(psa);
                    await _db.SaveChangesAsync();
                    success = true;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }
            else
            {
                message = T("Admin.Catalog.Attributes.SpecificationAttributes.PleaseSelect").Value;
            }

            return Json(new { success, message });
        }

        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public async Task<IActionResult> ProductSpecAttrUpdate(ProductSpecificationAttributeModel model)
        {
            var psa = await _db.ProductSpecificationAttributes.FindByIdAsync(model.Id);

            psa.AllowFiltering = model.AllowFiltering ?? false;
            psa.ShowOnProductPage = model.ShowOnProductPage ?? false;
            psa.DisplayOrder = model.DisplayOrder;
            psa.SpecificationAttributeOptionId = model.SpecificationAttributeOptionId;

            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public async Task<IActionResult> ProductSpecAttrDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductSpecificationAttributes.GetManyAsync(ids);
                _db.ProductSpecificationAttributes.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Product variant attributes

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductVariantAttributeList(GridCommand command, int productId)
        {
            var editValuesStr = T("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.ViewLink").Value;
            var copyOptionsStr = T("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.CopyOptions").Value;

            var productVariantAttributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ProductVariantAttributeValues)
                .Include(x => x.ProductAttribute)
                .ThenInclude(x => x.ProductAttributeOptionsSets)
                .ApplyProductFilter(new[] { productId })
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = productVariantAttributes.Select(x =>
            {
                var pvaModel = new ProductModel.ProductVariantAttributeModel
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductAttribute = x.ProductAttribute.Name,
                    ProductAttributeId = x.ProductAttributeId,
                    TextPrompt = x.TextPrompt,
                    CustomData = x.CustomData,
                    IsRequired = x.IsRequired,
                    AttributeControlType = Services.Localization.GetLocalizedEnum(x.AttributeControlType),
                    AttributeControlTypeId = x.AttributeControlTypeId,
                    DisplayOrder = x.DisplayOrder
                };

                if (x.IsListTypeAttribute())
                {
                    pvaModel.ValueCount = x.ProductVariantAttributeValues?.Count ?? 0;
                    pvaModel.EditUrl = Url.Action(nameof(EditAttributeValues), new { productVariantAttributeId = x.Id });
                    pvaModel.EditText = editValuesStr.FormatInvariant(pvaModel.ValueCount);

                    if (x.ProductAttribute.ProductAttributeOptionsSets.Any())
                    {
                        pvaModel.OptionSets.Add(new { Id = string.Empty, Name = copyOptionsStr });

                        x.ProductAttribute.ProductAttributeOptionsSets.Each(set =>
                        {
                            pvaModel.OptionSets.Add(new { set.Id, set.Name });
                        });
                    }
                }

                return pvaModel;
            })
            .ToList();

            return Json(new GridModel<ProductModel.ProductVariantAttributeModel>
            {
                Rows = rows,
                Total = await productVariantAttributes.GetTotalCountAsync(),
            });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeInsert(ProductModel.ProductVariantAttributeModel model, int productId)
        {
            var pva = new ProductVariantAttribute
            {
                ProductId = productId,
                ProductAttributeId = model.ProductAttributeId,
                TextPrompt = model.TextPrompt,
                CustomData = model.CustomData,
                IsRequired = model.IsRequired,
                AttributeControlTypeId = model.AttributeControlTypeId,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                _db.ProductVariantAttributes.Add(pva);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Services.Notifier.Error(ex.Message);
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeUpdate(ProductModel.ProductVariantAttributeModel model)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(model.Id);

            pva.ProductAttributeId = model.ProductAttributeId;
            pva.TextPrompt = model.TextPrompt;
            pva.CustomData = model.CustomData;
            pva.IsRequired = model.IsRequired;
            pva.AttributeControlTypeId = model.AttributeControlTypeId;
            pva.DisplayOrder = model.DisplayOrder;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductVariantAttributes.GetManyAsync(ids);
                _db.ProductVariantAttributes.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> CopyAttributeOptions(int productVariantAttributeId, int optionsSetId, bool deleteExistingValues)
        {
            var pva = await _db.ProductVariantAttributes
                .Include(x => x.ProductVariantAttributeValues)
                .FindByIdAsync(productVariantAttributeId, false);

            if (pva == null)
            {
                NotifyError(T("Products.Variants.NotFound", productVariantAttributeId));
            }
            else
            {
                try
                {
                    var numberOfCopiedOptions = await _productAttributeService.Value.CopyAttributeOptionsAsync(pva, optionsSetId, deleteExistingValues);

                    NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed")
                        + " "
                        + T("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.NumberOfCopiedOptions", numberOfCopiedOptions));
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return Json(string.Empty);
        }

        #endregion

        #region Product variant attribute values

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductAttributeValueList(int productVariantAttributeId, GridCommand command)
        {
            var values = await _db.ProductVariantAttributeValues
                .AsNoTracking()
                .ApplyProductAttributeFilter(productVariantAttributeId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var linkedProductIds = values
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .ToDistinctArray(x => x.LinkedProductId);

            var linkedProducts = linkedProductIds.Any()
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : new Dictionary<int, Product>();

            var rows = values.Select(x =>
            {
                var model = new ProductModel.ProductVariantAttributeValueModel
                {
                    Id = x.Id,
                    ProductVariantAttributeId = x.ProductVariantAttributeId,
                    Name = x.Name,
                    NameString = (x.Color.IsEmpty() ? x.Name : $"{x.Name} - {x.Color}").HtmlEncode(),
                    Alias = x.Alias,
                    Color = x.Color,
                    HasColor = !x.Color.IsEmpty(),
                    PictureId = x.MediaFileId,
                    PriceAdjustment = x.PriceAdjustment,
                    WeightAdjustment = x.WeightAdjustment,
                    PriceAdjustmentString = x.ValueType == ProductVariantAttributeValueType.Simple ? x.PriceAdjustment.ToString("G29") : string.Empty,
                    WeightAdjustmentString = x.ValueType == ProductVariantAttributeValueType.Simple ? x.WeightAdjustment.ToString("G29") : string.Empty,
                    IsPreSelected = x.IsPreSelected,
                    DisplayOrder = x.DisplayOrder,
                    ValueTypeId = x.ValueTypeId,
                    TypeName = Services.Localization.GetLocalizedEnum(x.ValueType),
                    TypeNameClass = x.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr-2" : "d-none hide hidden-xs-up",
                    LinkedProductId = x.LinkedProductId,
                    Quantity = x.Quantity
                };

                if (x.ValueType == ProductVariantAttributeValueType.ProductLinkage && linkedProducts.TryGetValue(x.LinkedProductId, out var linkedProduct))
                {
                    model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
                    model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(Services.Localization);
                    model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;
                    model.LinkedProductEditUrl = Url.Action(nameof(Edit), new { id = linkedProduct.Id });

                    if (model.Quantity > 1)
                    {
                        model.QuantityInfo = $" × {model.Quantity}";
                    }
                }

                return model;
            })
            .ToList();

            return Json(new GridModel<ProductModel.ProductVariantAttributeValueModel>
            {
                Rows = rows,
                Total = await values.GetTotalCountAsync()
            });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> EditAttributeValues(int productVariantAttributeId)
        {
            var pva = await _db.ProductVariantAttributes
                .Include(x => x.ProductAttribute)
                .FindByIdAsync(productVariantAttributeId, false);

            if (pva == null)
                throw new ArgumentException(T("Products.Variants.NotFound", productVariantAttributeId));

            var product = await _db.Products.FindByIdAsync(pva.ProductId, false);
            if (product == null)
                throw new ArgumentException(T("Products.NotFound", pva.ProductId));

            var model = new ProductModel.ProductVariantAttributeValueListModel
            {
                ProductName = product.Name,
                ProductId = pva.ProductId,
                ProductVariantAttributeName = pva.ProductAttribute.Name,
                ProductVariantAttributeId = pva.Id
            };

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueCreatePopup(string btnId, string formId, int productVariantAttributeId)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(productVariantAttributeId, false);
            if (pva == null)
                throw new ArgumentException(T("Products.Variants.NotFound", productVariantAttributeId));

            var model = new ProductModel.ProductVariantAttributeValueModel
            {
                ProductId = pva.ProductId,
                ProductVariantAttributeId = productVariantAttributeId,
                IsListTypeAttribute = pva.IsListTypeAttribute(),
                Color = string.Empty,
                Quantity = 1
            };

            AddLocales(model.Locales);

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueCreatePopup(string btnId, string formId, ProductModel.ProductVariantAttributeValueModel model)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(model.ProductVariantAttributeId);
            if (pva == null)
            {
                return RedirectToAction(nameof(List));
            }

            if (model.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage && await IsBundleItemAsync(pva.ProductId))
            {
                var product = await _db.Products.FindByIdAsync(pva.ProductId, false);
                var productName = product?.Name.NaIfEmpty();

                ModelState.AddModelError(string.Empty, T("Admin.Catalog.Products.BundleItems.NoProductLinkageForBundleItem", productName));
            }

            if (ModelState.IsValid)
            {
                var pvav = new ProductVariantAttributeValue();
                MiniMapper.Map(model, pvav);
                pvav.MediaFileId = model.PictureId;
                pvav.LinkedProductId = pvav.ValueType == ProductVariantAttributeValueType.Simple ? 0 : model.LinkedProductId;

                try
                {
                    _db.ProductVariantAttributeValues.Add(pvav);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                try
                {
                    await UpdateLocalesAsync(pvav, model);
                }
                catch { }

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
                return View(model);
            }

            // If we got this far something failed. Redisplay form!
            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductAttributeValueEditPopup(string btnId, string formId, int id)
        {
            var pvav = await _db.ProductVariantAttributeValues
                .Include(x => x.ProductVariantAttribute)
                .FindByIdAsync(id, false);

            if (pvav == null)
            {
                return RedirectToAction(nameof(List));
            }

            var linkedProduct = await _db.Products.FindByIdAsync(pvav.LinkedProductId, false);

            var model = new ProductModel.ProductVariantAttributeValueModel
            {
                ProductId = pvav.ProductVariantAttribute.ProductId,
                ProductVariantAttributeId = pvav.ProductVariantAttributeId,
                Name = pvav.Name,
                Alias = pvav.Alias,
                Color = pvav.Color,
                PictureId = pvav.MediaFileId,
                IsListTypeAttribute = pvav.ProductVariantAttribute.IsListTypeAttribute(),
                PriceAdjustment = pvav.PriceAdjustment,
                WeightAdjustment = pvav.WeightAdjustment,
                IsPreSelected = pvav.IsPreSelected,
                DisplayOrder = pvav.DisplayOrder,
                ValueTypeId = pvav.ValueTypeId,
                TypeName = await Services.Localization.GetLocalizedEnumAsync(pvav.ValueType),
                TypeNameClass = pvav.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr-2" : "d-none hide hidden-xs-up",
                LinkedProductId = pvav.LinkedProductId,
                Quantity = pvav.Quantity
            };

            if (linkedProduct != null)
            {
                model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
                model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(Services.Localization);
                model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;
                model.LinkedProductEditUrl = Url.Action(nameof(Edit), new { id = linkedProduct.Id });

                if (model.Quantity > 1)
                {
                    model.QuantityInfo = $" × {model.Quantity}";
                }
            }

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = pvav.GetLocalized(x => x.Name, languageId, false, false);
                locale.Alias = pvav.GetLocalized(x => x.Alias, languageId, false, false);
            });

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueEditPopup(string btnId, string formId, ProductModel.ProductVariantAttributeValueModel model)
        {
            var pvav = await _db.ProductVariantAttributeValues
                .Include(x => x.ProductVariantAttribute)
                .FindByIdAsync(model.Id);

            if (pvav == null)
            {
                return RedirectToAction(nameof(List));
            }

            if (model.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage && await IsBundleItemAsync(pvav.ProductVariantAttribute.ProductId))
            {
                var product = await _db.Products.FindByIdAsync(pvav.ProductVariantAttribute.ProductId, false);
                var productName = product?.Name.NaIfEmpty();

                ModelState.AddModelError(string.Empty, T("Admin.Catalog.Products.BundleItems.NoProductLinkageForBundleItem", productName));
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model, pvav);
                pvav.MediaFileId = model.PictureId;
                pvav.LinkedProductId = pvav.ValueType == ProductVariantAttributeValueType.Simple ? 0 : model.LinkedProductId;

                try
                {
                    await UpdateLocalesAsync(pvav, model);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductVariantAttributeValues.GetManyAsync(ids);
                _db.ProductVariantAttributeValues.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        private async Task UpdateLocalesAsync(ProductVariantAttributeValue pvav, ProductModel.ProductVariantAttributeValueModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(pvav, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(pvav, x => x.Alias, localized.Alias, localized.LanguageId);
            }
        }

        private async Task<bool> IsBundleItemAsync(int productId)
        {
            if (productId == 0)
            {
                return false;
            }

            var query =
                from pbi in _db.ProductBundleItem.AsNoTracking()
                join bundle in _db.Products.AsNoTracking() on pbi.BundleProductId equals bundle.Id
                where pbi.ProductId == productId && !bundle.Deleted
                select pbi;

            var result = await query.AnyAsync();
            return result;
        }

        #endregion

        #region Product variant attribute combinations

        private async Task PrepareProductAttributeCombinationModelAsync(
            ProductVariantAttributeCombinationModel model,
            ProductVariantAttributeCombination entity,
            Product product,
            bool formatAttributes = false)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(product, nameof(product));

            var baseDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId);

            model.ProductId = product.Id;
            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
            model.BaseDimensionIn = baseDimension?.GetLocalized(x => x.Name) ?? string.Empty;

            if (entity != null)
            {
                if (formatAttributes)
                {
                    model.AttributesXml = await _productAttributeFormatter.Value.FormatAttributesAsync(
                        entity.AttributeSelection,
                        product,
                        _workContext.CurrentCustomer,
                        "<br />",
                        includeHyperlinks: false);
                }
            }
            else
            {
                // It's a new entity, so initialize it properly.
                model.StockQuantity = 10000;
                model.IsActive = true;
                model.AllowOutOfStockOrders = true;
            }

            var quantityUnits = await _db.QuantityUnits
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.QuantityUnits = quantityUnits
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = entity != null && x.Id == entity.QuantityUnitId.GetValueOrDefault()
                })
                .ToList();
        }

        private async Task PrepareVariantCombinationAttributesAsync(ProductVariantAttributeCombinationModel model, Product product)
        {
            var productVariantAttributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .Include(x => x.ProductAttribute)
                .Include(x => x.ProductVariantAttributeValues)
                .ApplyListTypeFilter()
                .ApplyProductFilter(new[] { product.Id })
                .ToListAsync();

            foreach (var attribute in productVariantAttributes)
            {
                var pvaModel = new ProductVariantAttributeCombinationModel.ProductVariantAttributeModel
                {
                    Id = attribute.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = attribute.ProductAttribute.Name,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType
                };

                foreach (var pvaValue in attribute.ProductVariantAttributeValues)
                {
                    pvaModel.Values.Add(new ProductVariantAttributeCombinationModel.ProductVariantAttributeValueModel
                    {
                        Id = pvaValue.Id,
                        Name = pvaValue.Name,
                        IsPreSelected = pvaValue.IsPreSelected
                    });
                }

                model.ProductVariantAttributes.Add(pvaModel);
            }
        }

        private async Task PrepareVariantCombinationPicturesAsync(ProductVariantAttributeCombinationModel model, Product product)
        {
            var files = await _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyProductFilter(product.Id)
                .Select(x => x.MediaFile)
                .ToListAsync();

            foreach (var file in files)
            {
                model.AssignablePictures.Add(new ProductVariantAttributeCombinationModel.PictureSelectItemModel
                {
                    Id = file.Id,
                    IsAssigned = model.AssignedPictureIds.Contains(file.Id),
                    Media = _mediaService.ConvertMediaFile(file)
                });
            }
        }

        private void PrepareViewBag(string btnId, string formId, bool refreshPage = false, bool isEdit = true)
        {
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            ViewBag.RefreshPage = refreshPage;
            ViewBag.IsEdit = isEdit;
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductVariantAttributeCombinationList(GridCommand command, int productId)
        {
            var customer = _workContext.CurrentCustomer;
            var product = await _db.Products.FindByIdAsync(productId, false);
            var productUrlTitle = T("Common.OpenInShop");
            var productSlug = await product.GetActiveSlugAsync();

            var allCombinations = await _db.ProductVariantAttributeCombinations
                .AsNoTracking()
                .Where(x => x.ProductId == product.Id)
                .OrderBy(x => x.Id)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            await _productAttributeMaterializer.Value.PrefetchProductVariantAttributesAsync(allCombinations.Select(x => x.AttributeSelection));

            var mapper = MapperFactory.GetMapper<ProductVariantAttributeCombination, ProductVariantAttributeCombinationModel>();
            var rows = await allCombinations.SelectAwait(async x =>
            {
                var pvacModel = await mapper.MapAsync(x);
                pvacModel.ProductId = product.Id;
                pvacModel.ProductUrlTitle = productUrlTitle;
                pvacModel.ProductUrl = await _productUrlHelper.Value.GetProductUrlAsync(product.Id, productSlug, x.AttributeSelection);
                pvacModel.AttributesXml = await _productAttributeFormatter.Value.FormatAttributesAsync(x.AttributeSelection, product, customer, "<br />", false, includeHyperlinks: false);

                return pvacModel;
            })
            .AsyncToList();

            return Json(new GridModel<ProductVariantAttributeCombinationModel>
            {
                Rows = rows,
                Total = await allCombinations.GetTotalCountAsync()
            });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeCombinationDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductVariantAttributeCombinations.GetManyAsync(ids);
                _db.ProductVariantAttributeCombinations.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> AttributeCombinationCreatePopup(string btnId, string formId, int productId)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);

            if (product == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = new ProductVariantAttributeCombinationModel();
            await PrepareProductAttributeCombinationModelAsync(model, null, product);
            await PrepareVariantCombinationAttributesAsync(model, product);
            await PrepareVariantCombinationPicturesAsync(model, product);
            PrepareViewBag(btnId, formId, false, false);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> AttributeCombinationCreatePopup(
            string btnId,
            string formId,
            int productId,
            ProductVariantAttributeCombinationModel model,
            ProductVariantQuery query)
        {
            var product = await _db.Products
                .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.ProductAttribute)
                .FindByIdAsync(productId);

            if (product == null)
            {
                return NotFound();
            }

            var productVariantAttributes = product.ProductVariantAttributes
                .AsQueryable()
                .ApplyListTypeFilter();

            var (selection, warnings) = await _productAttributeMaterializer.Value.CreateAttributeSelectionAsync(query, productVariantAttributes, product.Id, 0);

            await _shoppingCartValidator.Value.ValidateProductAttributesAsync(
                product,
                selection,
                Services.StoreContext.CurrentStore.Id,
                warnings);

            var foundCombination = await _productAttributeMaterializer.Value.FindAttributeCombinationAsync(product.Id, selection);

            if (foundCombination != null)
            {
                warnings.Add(T("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.CombiExists"));
            }

            if (warnings.Count == 0)
            {
                var combination = await MapperFactory.MapAsync<ProductVariantAttributeCombinationModel, ProductVariantAttributeCombination>(model);
                combination.RawAttributes = selection.AsJson();
                combination.SetAssignedMediaIds(model.AssignedPictureIds);

                _db.ProductVariantAttributeCombinations.Add(combination);
                await _db.SaveChangesAsync();
            }

            await PrepareProductAttributeCombinationModelAsync(model, null, product);
            await PrepareVariantCombinationAttributesAsync(model, product);
            await PrepareVariantCombinationPicturesAsync(model, product);
            PrepareViewBag(btnId, formId, warnings.Count == 0, false);

            if (warnings.Count > 0)
            {
                model.Warnings = warnings;
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> AttributeCombinationEditPopup(int id, string btnId, string formId)
        {
            var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(id, false);
            if (combination == null)
            {
                return RedirectToAction(nameof(List));
            }

            var product = await _db.Products.FindByIdAsync(combination.ProductId, false);
            if (product == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = await MapperFactory.MapAsync<ProductVariantAttributeCombination, ProductVariantAttributeCombinationModel>(combination);

            await PrepareProductAttributeCombinationModelAsync(model, combination, product, true);
            await PrepareVariantCombinationAttributesAsync(model, product);
            await PrepareVariantCombinationPicturesAsync(model, product);
            PrepareViewBag(btnId, formId);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> AttributeCombinationEditPopup(string btnId, string formId, ProductVariantAttributeCombinationModel model)
        {
            if (ModelState.IsValid)
            {
                var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(model.Id);
                if (combination == null)
                {
                    return RedirectToAction(nameof(List));
                }

                var rawAttributes = combination.RawAttributes;
                await MapperFactory.MapAsync(model, combination);
                combination.RawAttributes = rawAttributes;
                combination.SetAssignedMediaIds(model.AssignedPictureIds);

                await _db.SaveChangesAsync();

                PrepareViewBag(btnId, formId, true);
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> CreateAllAttributeCombinations(ProductVariantAttributeCombinationModel model, int productId)
        {
            var hasProduct = await _db.Products.AnyAsync(x => x.Id == productId);
            if (!hasProduct)
            {
                throw new ArgumentException(T("Products.NotFound", productId));
            }

            await _productAttributeService.Value.CreateAllAttributeCombinationsAsync(productId);

            return Json(string.Empty);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> DeleteAllAttributeCombinations(ProductVariantAttributeCombinationModel model, int productId)
        {
            var hasProduct = await _db.Products.AnyAsync(x => x.Id == productId);
            if (!hasProduct)
            {
                throw new ArgumentException(T("Products.NotFound", productId));
            }

            var toDelete = await _db.ProductVariantAttributeCombinations
                .AsQueryable()
                .Where(x => x.ProductId == productId)
                .ToListAsync();

            _db.ProductVariantAttributeCombinations.RemoveRange(toDelete);
            await _db.SaveChangesAsync();

            return Json(string.Empty);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> CombinationExistenceNote(int productId, ProductVariantQuery query)
        {
            var product = await _db.Products
                .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.ProductAttribute)
                .FindByIdAsync(productId);

            if (product == null)
            {
                return new JsonResult(new { Message = T("Products.NotFound", productId), HasWarning = true });
            }

            var productVariantAttributes = product.ProductVariantAttributes
                .AsQueryable()
                .ApplyListTypeFilter();

            var (selection, warnings) = await _productAttributeMaterializer.Value.CreateAttributeSelectionAsync(query, productVariantAttributes, product.Id, 0);
            var foundCombination = await _productAttributeMaterializer.Value.FindAttributeCombinationAsync(product.Id, selection);

            if (foundCombination == null)
            {
                await _shoppingCartValidator.Value.ValidateProductAttributesAsync(
                    product,
                    selection,
                    Services.StoreContext.CurrentStore.Id,
                    warnings);
            }

            if (warnings.Any())
            {
                return new JsonResult(new { Message = warnings[0], HasWarning = true });
            }

            string message = T(foundCombination != null
                ? "Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.CombiExists"
                : "Admin.Catalog.Products.Variants.ProductVariantAttributes.AttributeCombinations.CombiNotExists");

            return new JsonResult(new
            {
                Message = message,
                HasWarning = foundCombination != null
            });
        }

        #endregion
    }
}
