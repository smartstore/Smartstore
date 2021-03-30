using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Utilities.Html;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Controllers
{
    public partial class ProductController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IWebHelper _webHelper;
        private readonly IProductService _productService;
        private readonly IProductTagService _productTagService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IMediaService _mediaService;
        private readonly ICustomerService _customerService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly IProductCompareService _productCompareService;
        private readonly CatalogHelper _helper;
        private readonly IBreadcrumb _breadcrumb;
        private readonly SeoSettings _seoSettings;
        private readonly ContactDataSettings _contactDataSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly PrivacySettings _privacySettings;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IMessageFactory> _messageFactory;
        private readonly Lazy<ProductUrlHelper> _productUrlHelper;
        private readonly Lazy<IProductAttributeFormatter> _productAttributeFormatter;
        private readonly Lazy<IProductAttributeMaterializer> _productAttributeMaterializer;

        public ProductController(
            SmartDbContext db,
            IWebHelper webHelper,
            IProductService productService,
            IProductTagService productTagService,
            IProductAttributeService productAttributeService,
            IRecentlyViewedProductsService recentlyViewedProductsService,
            IProductCompareService productCompareService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            ICatalogSearchService catalogSearchService,
            IMediaService mediaService,
            ICustomerService customerService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            CatalogHelper helper,
            IBreadcrumb breadcrumb,
            SeoSettings seoSettings,
            ContactDataSettings contactDataSettings,
            CaptchaSettings captchaSettings,
            LocalizationSettings localizationSettings,
            PrivacySettings privacySettings,
            Lazy<IUrlHelper> urlHelper,
            Lazy<IMessageFactory> messageFactory,
            Lazy<ProductUrlHelper> productUrlHelper,
            Lazy<IProductAttributeFormatter> productAttributeFormatter,
            Lazy<IProductAttributeMaterializer> productAttributeMaterializer)
        {
            _db = db;
            _webHelper = webHelper;
            _productService = productService;
            _productTagService = productTagService;
            _productAttributeService = productAttributeService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _productCompareService = productCompareService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _catalogSearchService = catalogSearchService;
            _mediaService = mediaService;
            _customerService = customerService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _helper = helper;
            _breadcrumb = breadcrumb;
            _seoSettings = seoSettings;
            _contactDataSettings = contactDataSettings;
            _captchaSettings = captchaSettings;
            _localizationSettings = localizationSettings;
            _privacySettings = privacySettings;
            _urlHelper = urlHelper;
            _messageFactory = messageFactory;
            _productUrlHelper = productUrlHelper;
            _productAttributeFormatter = productAttributeFormatter;
            _productAttributeMaterializer = productAttributeMaterializer;
        }

        #region Products

        public async Task<IActionResult> ProductDetails(int productId, ProductVariantQuery query)
        {
            var product = await _db.Products
                .IncludeMedia()
                .IncludeManufacturers()
                .Where(x => x.Id == productId)
                .FirstOrDefaultAsync();

            if (product == null || product.IsSystemProduct)
                return NotFound();

            // Is published? Check whether the current user has a "Manage catalog" permission.
            // It allows him to preview a product before publishing.
            if (!product.Published && !await Services.Permissions.AuthorizeAsync(Permissions.Catalog.Product.Read))
                return NotFound();

            // ACL (access control list).
            if (!await _aclService.AuthorizeAsync(product))
                return NotFound();

            // Store mapping.
            if (!await _storeMappingService.AuthorizeAsync(product))
                return NotFound();

            // Is product individually visible?
            if (product.Visibility == ProductVisibility.Hidden)
            {
                // Find parent grouped product.
                var parentGroupedProduct = await _db.Products.FindByIdAsync(product.ParentGroupedProductId, false);
                if (parentGroupedProduct == null)
                    return NotFound();

                var seName = await parentGroupedProduct.GetActiveSlugAsync();
                if (seName.IsEmpty())
                    return NotFound();

                var routeValues = new RouteValueDictionary
                {
                    { "SeName", seName }
                };

                // Add query string parameters.
                Request.Query.Each(x => routeValues.Add(x.Key, Request.Query[x.Value].ToString()));

                return RedirectToRoute("Product", routeValues);
            }

            // Prepare the view model
            var model = await _helper.MapProductDetailsPageModelAsync(product, query);

            // Some cargo data
            model.PictureSize = _mediaSettings.ProductDetailsPictureSize;
            model.HotlineTelephoneNumber = _contactDataSettings.HotlineTelephoneNumber.NullEmpty();
            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = _urlHelper.Value.RouteUrl("Product", new { model.SeName }, Request.Scheme);
            }

            // Save as recently viewed
            _recentlyViewedProductsService.AddProductToRecentlyViewedList(product.Id);

            // Activity log
            Services.ActivityLogger.LogActivity("PublicStore.ViewProduct", T("ActivityLog.PublicStore.ViewProduct"), product.Name);

            // Breadcrumb
            if (_catalogSettings.CategoryBreadcrumbEnabled)
            {
                await _helper.GetBreadcrumbAsync(_breadcrumb, ControllerContext, product);

                _breadcrumb.Track(new MenuItem
                {
                    Text = model.Name,
                    Rtl = model.Name.CurrentLanguage.Rtl,
                    EntityId = product.Id,
                    Url = Url.RouteUrl("Product", new { model.SeName })
                });
            }

            return View(model.ProductTemplateViewPath, model);
        }

        /// <summary>
        /// This action is used to update the display of product detail view.
        /// It will be called via AJAX upon user interaction (e.g. changing of quantity || attribute selection).
        /// All relevasnt product partials will be rendered with updated models and returned as JSON data.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProductDetails(int productId, string itemType, int bundleItemId, ProductVariantQuery query)
        {
            // TODO: (core) UpdateProductDetails action needs some decent refactoring.
            var form = HttpContext.Request.Form;
            int quantity = 1;
            int galleryStartIndex = -1;
            string galleryHtml = null;
            string dynamicThumbUrl = null;
            var isAssociated = itemType.EqualsNoCase("associateditem");

            var product = await _db.Products.FindByIdAsync(productId);
            var batchContext = _productService.CreateProductBatchContext(new[] { product }, includeHidden: false);
            var bItem = await _db.ProductBundleItem
                .Include(x => x.BundleProduct)
                .FindByIdAsync(bundleItemId, false);

            IList<ProductBundleItemData> bundleItemDatas = null;
            ProductBundleItemData bundleItem = bItem == null ? null : new ProductBundleItemData(bItem);

            // Quantity required for tier prices.
            string quantityKey = form.Keys.FirstOrDefault(k => k.EndsWith("EnteredQuantity"));
            if (quantityKey.HasValue())
            {
                _ = int.TryParse(form[quantityKey], out quantity);
            }

            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                var bundleItemQuery = await _db.ProductBundleItem
                    .AsNoTracking()
                    .ApplyBundledProductsFilter(new[] { product.Id })
                    .Include(x => x.Product)
                    .Include(x => x.BundleProduct)
                    .ToListAsync();

                if (bundleItemQuery.Count > 0)
                {
                    bundleItemDatas = new List<ProductBundleItemData>();
                    bundleItemQuery.Each(x => bundleItemDatas.Add(new ProductBundleItemData(x)));
                }

                if (query.Variants.Count > 0)
                {
                    batchContext.Collect(bundleItemDatas.Select(x => x.Item.Product.Id).ToArray());

                    // May add elements to query object if they are preselected by bundle item filter.
                    foreach (var itemData in bundleItemDatas)
                    {
                        await _helper.MapProductDetailsPageModelAsync(new ProductDetailsModelContext
                        {
                            Product = itemData.Item.Product,
                            BatchContext = batchContext,
                            VariantQuery = query,
                            ProductBundleItem = itemData
                        });
                    }
                }
            }

            var modelContext = new ProductDetailsModelContext
            {
                Product = product,
                BatchContext = batchContext,
                VariantQuery = query,
                IsAssociatedProduct = isAssociated,
                ProductBundleItem = bundleItem,
                BundleItemDatas = bundleItemDatas,
                Customer = batchContext.Customer,
                Store = batchContext.Store,
                Currency = Services.WorkContext.WorkingCurrency
            };

            // Get merged model data.
            var model = new ProductDetailsModel();
            await _helper.PrepareProductDetailModelAsync(model, modelContext, quantity);

            if (bundleItem != null)
            {
                // Update bundle item thumbnail.
                if (!bundleItem.Item.HideThumbnail)
                {
                    var assignedMediaIds = model.SelectedCombination?.GetAssignedMediaIds() ?? Array.Empty<int>();
                    var hasFile = assignedMediaIds.Any() && await _db.MediaFiles.AnyAsync(x => x.Id == assignedMediaIds[0]);
                    if (assignedMediaIds.Any() && hasFile)
                    {
                        var file = await _db.ProductMediaFiles
                            .AsNoTracking()
                            .ApplyProductFilter(new[] { bundleItem.Item.ProductId }, 1)
                            .Select(x => x.MediaFile)
                            .FirstOrDefaultAsync();

                        dynamicThumbUrl = _mediaService.GetUrl(file, _mediaSettings.BundledProductPictureSize, null, false);
                    }
                }
            }
            else if (isAssociated)
            {
                // Update associated product thumbnail.
                var assignedMediaIds = model.SelectedCombination?.GetAssignedMediaIds() ?? new int[0];
                var hasFile = await _db.MediaFiles.AnyAsync(x => x.Id == assignedMediaIds[0]);
                if (assignedMediaIds.Any() && hasFile)
                {
                    var file = await _db.ProductMediaFiles
                        .AsNoTracking()
                        .ApplyProductFilter(new[] { productId }, 1)
                        .Select(x => x.MediaFile)
                        .FirstOrDefaultAsync();

                    dynamicThumbUrl = _mediaService.GetUrl(file, _mediaSettings.AssociatedProductPictureSize, null, false);
                }
            }
            else if (product.ProductType != ProductType.BundledProduct)
            {
                // Update image gallery.
                var files = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .ApplyProductFilter(new[] { productId })
                    .Select(x => _mediaService.ConvertMediaFile(x.MediaFile))
                    .ToListAsync();

                if (product.HasPreviewPicture && files.Count > 1)
                {
                    files.RemoveAt(0);
                }

                if (files.Count <= _catalogSettings.DisplayAllImagesNumber)
                {
                    // All pictures rendered... only index is required.
                    galleryStartIndex = 0;

                    var assignedMediaIds = model.SelectedCombination?.GetAssignedMediaIds() ?? new int[0];
                    if (assignedMediaIds.Any())
                    {
                        var file = files.FirstOrDefault(p => p.Id == assignedMediaIds[0]);
                        galleryStartIndex = file == null ? 0 : files.IndexOf(file);
                    }
                }
                else
                {
                    var allCombinationPictureIds = await _productAttributeService.GetAttributeCombinationFileIdsAsync(product.Id);

                    var mediaModel = _helper.PrepareProductDetailsMediaGalleryModel(
                        files,
                        product.GetLocalized(x => x.Name),
                        allCombinationPictureIds,
                        false,
                        bundleItem,
                        model.SelectedCombination);

                    galleryStartIndex = mediaModel.GalleryStartIndex;
                    galleryHtml = (await this.InvokeViewAsync("Product.Media", mediaModel)).ToString();
                }

                model.PriceDisplayStyle = _catalogSettings.PriceDisplayStyle;
                model.DisplayTextForZeroPrices = _catalogSettings.DisplayTextForZeroPrices;
            }

            object partials = null;

            if (model.IsBundlePart)
            {
                partials = new
                {
                    BundleItemPrice = await this.InvokeViewAsync("Product.Offer.Price", model),
                    BundleItemStock = await this.InvokeViewAsync("Product.StockInfo", model),
                    BundleItemVariants = await this.InvokeViewAsync("Product.Variants", model.ProductVariantAttributes)
                };
            }
            else
            {
                var dataDictAddToCart = new ViewDataDictionary(ViewData) { Model = model };
                dataDictAddToCart.TemplateInfo.HtmlFieldPrefix = $"addtocart_{model.Id}";

                decimal adjustment = decimal.Zero;
                decimal taxRate = decimal.Zero;

                // TODO: (mh) (core) Implement when pricing is available.
                //var finalPriceWithDiscountBase = _taxService.GetProductPrice(product, product.Price, Services.WorkContext.CurrentCustomer, out taxRate);

                //if (!_taxSettings.Value.PricesIncludeTax && Services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax)
                //{
                //    adjustment = (m.ProductPrice.PriceValue - finalPriceWithDiscountBase) / (taxRate / 100 + 1);
                //}
                //else if (_taxSettings.Value.PricesIncludeTax && Services.WorkContext.TaxDisplayType == TaxDisplayType.ExcludingTax)
                //{
                //    adjustment = (m.ProductPrice.PriceValue - finalPriceWithDiscountBase) * (taxRate / 100 + 1);
                //}
                //else
                //{
                //    adjustment = m.ProductPrice.PriceValue - finalPriceWithDiscountBase;
                //}

                partials = new
                {
                    Attrs = await this.InvokeViewAsync("Product.Attrs", model),
                    Price = await this.InvokeViewAsync("Product.Offer.Price", model),
                    Stock = await this.InvokeViewAsync("Product.StockInfo", model),
                    Variants = await this.InvokeViewAsync("Product.Variants", model.ProductVariantAttributes),
                    OfferActions = await this.InvokeViewAsync("Product.Offer.Actions", dataDictAddToCart),
                    
                    // TODO: (mh) (core) Implement when Component or Partial is available.
                    //TierPrices = await _razorViewInvoker.Value.InvokeViewAsync("Product.TierPrices", await _razorViewInvoker.InvokeViewAsync(product, adjustment)),
                    BundlePrice = product.ProductType == ProductType.BundledProduct ? await this.InvokeViewAsync("Product.Bundle.Price", model) : null
                };
            }

            object data = new
            {
                Partials = partials,
                DynamicThumblUrl = dynamicThumbUrl,
                GalleryStartIndex = galleryStartIndex,
                GalleryHtml = galleryHtml
            };

            return new JsonResult(new { Data = data });
        }

        #endregion

        #region Product reviews

        [GdprConsent]
        public async Task<IActionResult> Reviews(int id)
        {
            // INFO: Entitity is being loaded tracked because else navigation properties can't be loaded in PrepareProductReviewsModelAsync.
            var product = await _db.Products
                .IncludeReviews()
                .FindByIdAsync(id);

            if (product == null || product.IsSystemProduct || !product.Published || !product.AllowCustomerReviews)
            {
                return NotFound();
            }

            var model = new ProductReviewsModel
            {
                Rating = _catalogSettings.DefaultProductRatingValue
            };

            await _helper.PrepareProductReviewsModelAsync(model, product);

            model.SuccessfullyAdded = (TempData["SuccessfullyAdded"] as bool?) ?? false;
            if (model.SuccessfullyAdded)
            {
                model.Result = T(_catalogSettings.ProductReviewsMustBeApproved ? "Reviews.SeeAfterApproving" : "Reviews.SuccessfullyAdded");
            }

            // Only registered users can leave reviews.
            if (Services.WorkContext.CurrentCustomer.IsGuest() && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                ModelState.AddModelError("", T("Reviews.OnlyRegisteredUsersCanWriteReviews"));
            }

            return View(model);
        }

        [HttpPost, ActionName("Reviews")]
        [ValidateCaptcha]
        [GdprConsent]
        public async Task<IActionResult> ReviewsAdd(int id, ProductReviewsModel model, string captchaError)
        {
            // INFO: Entitity is being loaded tracked because else navigation properties can't be loaded in PrepareProductReviewsModelAsync.
            var product = await _db.Products
                .IncludeReviews()
                .FindByIdAsync(id);

            if (product == null || product.IsSystemProduct || !product.Published || !product.AllowCustomerReviews)
            {
                return NotFound();
            }

            if (_captchaSettings.ShowOnProductReviewPage && captchaError.HasValue())
            {
                ModelState.AddModelError("", captchaError);
            }

            var customer = Services.WorkContext.CurrentCustomer;

            if (customer.IsGuest() && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                ModelState.AddModelError("", T("Reviews.OnlyRegisteredUsersCanWriteReviews"));
            }

            if (ModelState.IsValid)
            {
                var rating = model.Rating;
                if (rating < 1 || rating > 5)
                {
                    rating = _catalogSettings.DefaultProductRatingValue;
                }

                var isApproved = !_catalogSettings.ProductReviewsMustBeApproved;
                var productReview = new ProductReview
                {
                    ProductId = product.Id,
                    CustomerId = customer.Id,
                    IpAddress = _webHelper.GetClientIpAddress().ToString(),
                    Title = model.Title,
                    ReviewText = model.ReviewText,
                    Rating = rating,
                    HelpfulYesTotal = 0,
                    HelpfulNoTotal = 0,
                    IsApproved = isApproved,
                };

                product.ProductReviews.Add(productReview);
                _productService.ApplyProductReviewTotals(product);

                if (_catalogSettings.NotifyStoreOwnerAboutNewProductReviews)
                {
                    await _messageFactory.Value.SendProductReviewNotificationMessageAsync(productReview, _localizationSettings.DefaultAdminLanguageId);
                }

                Services.ActivityLogger.LogActivity("PublicStore.AddProductReview", T("ActivityLog.PublicStore.AddProductReview"), product.Name);

                if (isApproved)
                {
                    _customerService.ApplyRewardPointsForProductReview(customer, product, true);
                }

                TempData["SuccessfullyAdded"] = true;

                return RedirectToAction("Reviews");
            }

            // If we got this far something failed. Redisplay form.
            await _helper.PrepareProductReviewsModelAsync(model, product);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetReviewHelpfulness(int productReviewId, bool washelpful)
        {
            // INFO: Entitity is being loaded tracked because it must be saved later.
            var productReview = await _db.ProductReviews.FindByIdAsync(productReviewId);

            if (productReview == null)
                throw new ArgumentException(T("Reviews.NotFound", productReviewId));

            var customer = Services.WorkContext.CurrentCustomer;
            if (customer.IsGuest() && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                return Json(new
                {
                    Success = false,
                    Result = T("Reviews.Helpfulness.OnlyRegistered").Value,
                    TotalYes = productReview.HelpfulYesTotal,
                    TotalNo = productReview.HelpfulNoTotal
                });
            }

            // Customers aren't allowed to vote for their own reviews.
            if (productReview.CustomerId == customer.Id)
            {
                return Json(new
                {
                    Success = false,
                    Result = T("Reviews.Helpfulness.YourOwnReview").Value,
                    TotalYes = productReview.HelpfulYesTotal,
                    TotalNo = productReview.HelpfulNoTotal
                });
            }

            var entriesQuery = _db.CustomerContent
                .AsQueryable()
                .OfType<ProductReviewHelpfulness>()
                .Where(x => x.ProductReviewId == productReview.Id);

            // Delete previous helpfulness.
            var oldEntry = await entriesQuery.Where(x => x.CustomerId == customer.Id).FirstOrDefaultAsync();

            if (oldEntry != null)
            {
                _db.CustomerContent.Remove(oldEntry);
            }

            // Insert new helpfulness.
            var newEntry = new ProductReviewHelpfulness
            {
                ProductReviewId = productReview.Id,
                CustomerId = customer.Id,
                IpAddress = _webHelper.GetClientIpAddress().ToString(),
                WasHelpful = washelpful,
                IsApproved = true // Always approved
            };

            _db.CustomerContent.Add(newEntry);

            await _db.SaveChangesAsync();

            // New totals.
            int helpfulYesTotal = await entriesQuery.Where(x => x.WasHelpful).CountAsync();
            int helpfulNoTotal = await entriesQuery.Where(x => !x.WasHelpful).CountAsync();

            productReview.HelpfulYesTotal = helpfulYesTotal;
            productReview.HelpfulNoTotal = helpfulNoTotal;

            await _db.SaveChangesAsync();

            return Json(new
            {
                Success = true,
                Result = T("Reviews.Helpfulness.SuccessfullyVoted").Value,
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });
        }

        #endregion

        #region Ask product question

        [GdprConsent]
        public async Task<IActionResult> AskQuestion(int id)
        {
            if (!_catalogSettings.AskQuestionEnabled)
                return NotFound();

            var product = await _db.Products.FindByIdAsync(id, false);

            if (product == null || product.IsSystemProduct || !product.Published)
                return NotFound();

            var model = await PrepareAskQuestionModelAsync(product);

            return View(model);
        }

        public async Task<IActionResult> AskQuestionAjax(int id, ProductVariantQuery query)
        {
            // Get rawAttributes from product variant query
            if (query != null && id > 0)
            {
                var attributes = await _db.ProductVariantAttributes
                    .Include(x => x.ProductAttribute)
                    .ApplyProductFilter(new[] { id })
                    .ToListAsync();

                var selection = await _productAttributeMaterializer.Value.CreateAttributeSelectionAsync(query, attributes, id, 0, false);
                var rawAttributes = selection.Selection.AsJson();

                if (rawAttributes.HasValue() && TempData["AskQuestionAttributeSelection-" + id] == null)
                {
                    TempData.Add("AskQuestionAttributeSelection-" + id, rawAttributes);
                }
            }

            return new JsonResult(new
            {
                Data = new { redirect = Url.Action("AskQuestion", new { id }) }
            });
        }

        [HttpPost, ActionName("AskQuestion")]
        [ValidateCaptcha, ValidateHoneypot]
        [GdprConsent]
        public async Task<IActionResult> AskQuestionSend(ProductAskQuestionModel model, string captchaError)
        {
            if (!_catalogSettings.AskQuestionEnabled)
                return NotFound();

            var product = await _db.Products.FindByIdAsync(model.Id, false);

            if (product == null || product.IsSystemProduct || !product.Published)
                return NotFound();

            if (_captchaSettings.ShowOnAskQuestionPage && captchaError.HasValue())
            {
                ModelState.AddModelError("", captchaError);
            }

            if (ModelState.IsValid)
            {
                var msg = await _messageFactory.Value.SendProductQuestionMessageAsync(
                    Services.WorkContext.CurrentCustomer,
                    product,
                    model.SenderEmail,
                    model.SenderName,
                    model.SenderPhone,
                    HtmlUtils.ConvertPlainTextToHtml(model.Question.HtmlEncode()),
                    HtmlUtils.ConvertPlainTextToHtml(model.SelectedAttributes.HtmlEncode()),
                    model.ProductUrl,
                    model.IsQuoteRequest);

                if (msg?.Email?.Id != null)
                {
                    TempData.Remove("AskQuestionAttributeSelection-" + product.Id);

                    NotifySuccess(T("Products.AskQuestion.Sent"));
                    return RedirectToRoute("Product", new { SeName = await product.GetActiveSlugAsync() });
                }
                else
                {
                    ModelState.AddModelError("", T("Common.Error.SendMail"));
                }
            }

            // If we got this far something failed. Redisplay form.
            model = await PrepareAskQuestionModelAsync(product);

            return View(model);
        }

        private async Task<ProductAskQuestionModel> PrepareAskQuestionModelAsync(Product product)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var rawAttributes = TempData.Peek("AskQuestionAttributeSelection-" + product.Id) as string;

            // Check if saved rawAttributes belongs to current product id
            var formattedAttributes = string.Empty;
            var selection = new ProductVariantAttributeSelection(rawAttributes);
            if (selection.AttributesMap.Any())
            {
                formattedAttributes = await _productAttributeFormatter.Value.FormatAttributesAsync(
                    selection, 
                    product,
                    customer: null, 
                    separator: ", ", 
                    includePrices: false, 
                    includeGiftCardAttributes: false, 
                    includeHyperlinks: false);
            }

            var seName = await product.GetActiveSlugAsync();
            var model = new ProductAskQuestionModel
            {
                Id = product.Id,
                ProductName = product.GetLocalized(x => x.Name),
                ProductSeName = seName,
                SenderEmail = customer.Email,
                SenderName = customer.GetFullName(),
                SenderNameRequired = _privacySettings.FullNameOnProductRequestRequired,
                SenderPhone = customer.GenericAttributes.Phone,
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnAskQuestionPage,
                SelectedAttributes = formattedAttributes,
                ProductUrl = await _productUrlHelper.Value.GetProductUrlAsync(product.Id, seName, selection),
                IsQuoteRequest = product.CallForPrice
            };

            model.Question = T("Products.AskQuestion.Question." + (model.IsQuoteRequest ? "QuoteRequest" : "GeneralInquiry"), model.ProductName);

            return model;
        }

        #endregion

        #region Email a friend

        [GdprConsent]
        public async Task<IActionResult> EmailAFriend(int id)
        {
            var product = await _db.Products.FindByIdAsync(id, false);

            if (product == null || product.IsSystemProduct || !product.Published || !_catalogSettings.EmailAFriendEnabled)
                return NotFound();

            var model = await PrepareEmailAFriendModelAsync(product);

            return View(model);
        }

        [HttpPost, ActionName("EmailAFriend")]
        [ValidateCaptcha]
        [GdprConsent]
        public async Task<IActionResult> EmailAFriendSend(ProductEmailAFriendModel model, int id, string captchaError)
        {
            var product = await _db.Products.FindByIdAsync(id, false);
            if (product == null || product.IsSystemProduct || !product.Published || !_catalogSettings.EmailAFriendEnabled)
                return NotFound();

            if (_captchaSettings.ShowOnEmailProductToFriendPage && captchaError.HasValue())
            {
                ModelState.AddModelError("", captchaError);
            }

            var customer = Services.WorkContext.CurrentCustomer;

            // Check whether the current customer is guest and is allowed to email a friend.
            if (customer.IsGuest() && !_catalogSettings.AllowAnonymousUsersToEmailAFriend)
            {
                ModelState.AddModelError("", T("Products.EmailAFriend.OnlyRegisteredUsers"));
            }

            if (ModelState.IsValid)
            {
                //email
                await _messageFactory.Value.SendShareProductMessageAsync(
                    customer,
                    product,
                    model.YourEmailAddress,
                    model.FriendEmail,
                    HtmlUtils.ConvertPlainTextToHtml(model.PersonalMessage.HtmlEncode()));

                NotifySuccess(T("Products.EmailAFriend.SuccessfullySent"));

                return RedirectToRoute("Product", new { SeName = await product.GetActiveSlugAsync() });
            }

            // If we got this far something failed. Redisplay form.
            model = await PrepareEmailAFriendModelAsync(product);

            return View(model);
        }

        private async Task<ProductEmailAFriendModel> PrepareEmailAFriendModelAsync(Product product)
        {
            var model = new ProductEmailAFriendModel
            {
                ProductId = product.Id,
                ProductName = product.GetLocalized(x => x.Name),
                ProductSeName = await product.GetActiveSlugAsync(),
                YourEmailAddress = Services.WorkContext.CurrentCustomer.Email,
                AllowChangedCustomerEmail = _catalogSettings.AllowDifferingEmailAddressForEmailAFriend,
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnEmailProductToFriendPage
            };

            return model;
        }

        #endregion
    }
}

