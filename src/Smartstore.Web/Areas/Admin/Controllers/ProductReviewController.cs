using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ProductReviewController : AdminController
    {
        private readonly static SelectListItem[] _ratings = new[]
        {
            new SelectListItem { Text = "5", Value = "5" },
            new SelectListItem { Text = "4", Value = "4" },
            new SelectListItem { Text = "3", Value = "3" },
            new SelectListItem { Text = "2", Value = "2" },
            new SelectListItem { Text = "1", Value = "1" }
        };

        private readonly SmartDbContext _db;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;

        public ProductReviewController(
            SmartDbContext db,
            IProductService productService,
            ICustomerService customerService)
        {
            _db = db;
            _productService = productService;
            _customerService = customerService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Catalog.ProductReview.Read)]
        public IActionResult List()
        {
            var model = new ProductReviewListModel();

            ViewBag.Ratings = _ratings;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Read)]
        public async Task<IActionResult> ProductReviewList(GridCommand command, ProductReviewListModel model)
        {
            var dtHelper = Services.DateTimeHelper;

            DateTime? createdFrom = model.CreatedOnFrom == null
                ? null
                : dtHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, dtHelper.CurrentTimeZone);

            DateTime? createdTo = model.CreatedOnTo == null
                ? null
                : dtHelper.ConvertToUtcTime(model.CreatedOnTo.Value, dtHelper.CurrentTimeZone).AddDays(1);

            var query = _db.ProductReviews
                .AsSplitQuery()
                .Include(x => x.Product)
                .Include(x => x.Customer).ThenInclude(x => x.CustomerRoleMappings).ThenInclude(x => x.CustomerRole)
                .ApplyAuditDateFilter(createdFrom, createdTo);

            if (model.ProductName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Product.Name, model.ProductName);
            }

            if (model.Ratings?.Any() ?? false)
            {
                query = query.Where(x => model.Ratings.Contains(x.Rating));
            }

            if (model.IsVerifiedPurchase != null)
            {
                query = query.Where(x => x.IsVerifiedPurchase == model.IsVerifiedPurchase);
            }

            if (model.IsApproved != null)
            {
                query = query.Where(x => x.IsApproved == model.IsApproved);
            }

            var productReviews = await query
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = productReviews.Select(x =>
            {
                var m = new ProductReviewModel();
                PrepareProductReviewModel(m, x, false, true);
                return m;
            })
            .ToList();

            return Json(new GridModel<ProductReviewModel>
            {
                Rows = rows,
                Total = await productReviews.GetTotalCountAsync()
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Delete)]
        public async Task<IActionResult> ProductReviewDelete(GridSelection selection)
        {
            var success = false;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var productReviews = await _db.ProductReviews.GetManyAsync(ids, true);
                var productIds = productReviews.ToDistinctArray(x => x.ProductId);

                _db.CustomerContent.RemoveRange(productReviews);
                await _db.SaveChangesAsync();

                var products = await _db.Products
                    .Include(x => x.ProductReviews)
                    .Where(x => productIds.Contains(x.Id))
                    .ToListAsync();

                products.Each(x => _productService.ApplyProductReviewTotals(x));
                await _db.SaveChangesAsync();

                success = true;
            }

            return Json(new { Success = success });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var productReview = await _db.CustomerContent
                .OfType<ProductReview>()
                .Include(x => x.Product)
                .FindByIdAsync(id);

            if (productReview == null)
            {
                return NotFound();
            }

            var product = productReview.Product;

            _db.CustomerContent.Remove(productReview);
            await _db.SaveChangesAsync();

            _productService.ApplyProductReviewTotals(product);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Catalog.ProductReviews.Deleted"));

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Catalog.ProductReview.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var productReview = await _db.CustomerContent
                .OfType<ProductReview>()
                .Include(x => x.Product)
                .FindByIdAsync(id);

            if (productReview == null)
            {
                return NotFound();
            }

            var model = new ProductReviewModel();
            PrepareProductReviewModel(model, productReview, false, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.ProductReview.Update)]
        public async Task<IActionResult> Edit(ProductReviewModel model, bool continueEditing)
        {
            var productReview = await _db.CustomerContent
                .OfType<ProductReview>()
                .Include(x => x.Product)
                .Include(x => x.Customer)
                .FindByIdAsync(model.Id);

            if (productReview == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var approvedChanged = productReview.IsApproved != model.IsApproved;

                productReview.Title = model.Title;
                productReview.ReviewText = model.ReviewText;
                productReview.IsApproved = model.IsApproved;
                productReview.IsVerifiedPurchase = model.IsVerifiedPurchase;

                await _db.SaveChangesAsync();

                if (approvedChanged)
                {
                    _productService.ApplyProductReviewTotals(productReview.Product);
                    _customerService.ApplyRewardPointsForProductReview(productReview.Customer, productReview.Product, productReview.IsApproved);

                    await _db.SaveChangesAsync();
                }

                NotifySuccess(T("Admin.Catalog.ProductReviews.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), productReview.Id)
                    : RedirectToAction(nameof(List));
            }

            PrepareProductReviewModel(model, productReview, true, false);
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Approve)]
        public async Task<IActionResult> ApproveSelected(string selectedIds)
        {
            var numApproved = await UpdateApproved(selectedIds, true);

            NotifySuccess(T("Admin.Catalog.ProductReviews.NumberApprovedReviews", numApproved));

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Approve)]
        public async Task<IActionResult> DisapproveSelected(string selectedIds)
        {
            var numDisapproved = await UpdateApproved(selectedIds, false);

            NotifySuccess(T("Admin.Catalog.ProductReviews.NumberDisapprovedReviews", numDisapproved));

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Update)]
        public async Task<IActionResult> VerifySelected(string selectedIds)
        {
            var ids = selectedIds.ToIntArray();

            if (ids.Any())
            {
                var productReviews = await _db.ProductReviews
                    .Where(x => ids.Contains(x.Id) && (x.IsVerifiedPurchase == false || x.IsVerifiedPurchase == null))
                    .ToListAsync();

                if (productReviews.Any())
                {
                    productReviews.Each(x => x.IsVerifiedPurchase = true);

                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Catalog.ProductReviews.NumberVerfifiedReviews", productReviews.Count));
                }
            }

            return RedirectToAction(nameof(List));
        }

        private void PrepareProductReviewModel(ProductReviewModel model, ProductReview productReview, bool excludeProperties, bool forList)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(productReview, nameof(productReview));

            var product = productReview.Product;
            var customer = productReview?.Customer;

            model.Id = productReview.Id;
            model.ProductId = productReview.ProductId;
            model.ProductName = product?.GetLocalized(x => x.Name) ?? StringExtensions.NotAvailable;
            model.ProductTypeName = product?.GetProductTypeLabel(Services.Localization);
            model.ProductTypeLabelHint = product?.ProductTypeLabelHint;
            model.Sku = product?.Sku;
            model.CustomerId = productReview.CustomerId;
            model.CustomerName = customer?.GetDisplayName(T);
            model.IpAddress = productReview.IpAddress;
            model.Rating = productReview.Rating;
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(productReview.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(productReview.UpdatedOnUtc, DateTimeKind.Utc);
            model.HelpfulYesTotal = productReview.HelpfulYesTotal;
            model.HelpfulNoTotal = productReview.HelpfulNoTotal;
            model.IsVerifiedPurchase = productReview.IsVerifiedPurchase;
            model.EditUrl = Url.Action(nameof(Edit), new { productReview.Id });

            if (customer != null)
            {
                model.CustomerEditUrl = Url.Action("Edit", "Customer", new { Id = productReview.CustomerId });
            }

            if (product != null)
            {
                model.ProductEditUrl = Url.Action("Edit", "Product", new { Id = productReview.ProductId });
            }

            if (!excludeProperties)
            {
                model.Title = productReview.Title;
                model.IsApproved = productReview.IsApproved;

                model.ReviewText = forList
                    ? productReview.ReviewText.Truncate(400, "…")
                    : productReview.ReviewText;
            }
        }

        private async Task<int> UpdateApproved(string selectedIds, bool approved)
        {
            var numUpdated = 0;
            var ids = selectedIds.ToIntArray();

            if (ids.Any())
            {
                var productReviews = await _db.CustomerContent
                    .OfType<ProductReview>()
                    .Include(x => x.Customer).ThenInclude(x => x.RewardPointsHistory)
                    .Where(x => ids.Contains(x.Id) && x.IsApproved != approved)
                    .ToListAsync();

                if (productReviews.Any())
                {
                    productReviews.Each(x => x.IsApproved = approved);

                    numUpdated = productReviews.Count;
                    await _db.SaveChangesAsync();

                    // Update product review totals.
                    var productIds = productReviews.ToDistinctArray(x => x.ProductId);

                    var products = await _db.Products
                        .Include(x => x.ProductReviews)
                        .Where(x => productIds.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id, x => x);

                    products.Each(x => _productService.ApplyProductReviewTotals(x.Value));

                    // Update reward points history.
                    foreach (var productReview in productReviews)
                    {
                        _customerService.ApplyRewardPointsForProductReview(productReview.Customer, products.Get(productReview.ProductId), productReview.IsApproved);
                    }

                    await _db.SaveChangesAsync();
                }
            }

            return numUpdated;
        }
    }
}
