using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ProductReviewController : AdminController
    {
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

            var query = _db.CustomerContent
                .AsNoTracking()
                .ApplyAuditDateFilter(createdFrom, createdTo)
                .OfType<ProductReview>()
                .Include(x => x.Product);

            var productReviews = await query
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false)
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
                Total = productReviews.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Delete)]
        public async Task<IActionResult> ProductReviewDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var productReviews = await _db.CustomerContent
                    .OfType<ProductReview>()
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                var productIds = productReviews.ToDistinctArray(x => x.ProductId);

                _db.CustomerContent.RemoveRange(productReviews);
                numDeleted = await _db.SaveChangesAsync();

                var products = await _db.Products
                    .Include(x => x.ProductReviews)
                    .Where(x => productIds.Contains(x.Id))
                    .ToListAsync();

                foreach (var product in products)
                {
                    _productService.ApplyProductReviewTotals(product);
                }

                await _db.SaveChangesAsync();

                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
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
                productReview.Title = model.Title;
                productReview.ReviewText = model.ReviewText;
                productReview.IsApproved = model.IsApproved;

                _productService.ApplyProductReviewTotals(productReview.Product);
                _customerService.ApplyRewardPointsForProductReview(productReview.Customer, productReview.Product, productReview.IsApproved);

                await _db.SaveChangesAsync();

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
        public async Task<IActionResult> ApproveSelected(ICollection<int> selectedIds)
        {
            await UpdateApproved(selectedIds, true);

            NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

            return Json(new { success = true });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.ProductReview.Approve)]
        public async Task<IActionResult> DisapproveSelected(ICollection<int> selectedIds)
        {
            await UpdateApproved(selectedIds, false);
            
            NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

            return Json(new { success = true });
        }

        private void PrepareProductReviewModel(ProductReviewModel model, ProductReview productReview, bool excludeProperties, bool formatReviewText)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(productReview, nameof(productReview));

            model.Id = productReview.Id;
            model.ProductId = productReview.ProductId;
            model.ProductName = productReview.Product.Name;
            model.ProductTypeName = productReview.Product.GetProductTypeLabel(Services.Localization);
            model.ProductTypeLabelHint = productReview.Product.ProductTypeLabelHint;
            model.CustomerId = productReview.CustomerId;
            model.CustomerName = productReview.Customer.GetDisplayName(T);
            model.IpAddress = productReview.IpAddress;
            model.Rating = productReview.Rating;
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(productReview.CreatedOnUtc, DateTimeKind.Utc);
            model.EditUrl = Url.Action("Edit", "Product", new { Id = productReview.ProductId });

            if (!excludeProperties)
            {
                model.Title = productReview.Title;
                model.IsApproved = productReview.IsApproved;

                model.ReviewText = formatReviewText
                    ? HtmlUtility.ConvertPlainTextToHtml(productReview.ReviewText.HtmlEncode())
                    : productReview.ReviewText;
            }
        }

        private async Task<int> UpdateApproved(ICollection<int> selectedIds, bool approved)
        {
            var numApproved = 0;

            if (selectedIds?.Any() ?? false)
            {
                var productReviews = await _db.CustomerContent
                    .OfType<ProductReview>()
                    .Include(x => x.Product)
                    .Include(x => x.Customer)
                    .Where(x => selectedIds.Contains(x.Id))
                    .ToListAsync();

                productReviews.Each(x => x.IsApproved = approved);

                numApproved = await _db.SaveChangesAsync();

                foreach (var productReview in productReviews)
                {
                    _productService.ApplyProductReviewTotals(productReview.Product);
                    _customerService.ApplyRewardPointsForProductReview(productReview.Customer, productReview.Product, productReview.IsApproved);
                }

                await _db.SaveChangesAsync();
            }

            return numApproved;
        }
    }
}
