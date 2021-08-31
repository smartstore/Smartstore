using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Discounts;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class DiscountController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IRuleService _ruleService;

        public DiscountController(SmartDbContext db, IRuleService ruleService)
        {
            _db = db;
            _ruleService = ruleService;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available discounts. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="DiscountType">Specifies the <see cref="DiscountType"/>.</param>
        /// <returns>List of all discounts as JSON.</returns>
        public async Task<IActionResult> AllDiscounts(string label, string selectedIds, DiscountType? type)
        {
            var discounts = await _db.Discounts
                .AsNoTracking()
                .Where(x => x.DiscountTypeId == (int)type)
                .ToListAsync();
                
            var selectedArr = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                discounts.Insert(0, new Discount { Name = label, Id = 0 });
            }

            var data = discounts
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = selectedArr.Contains(x.Id)
                })
                .ToList();

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Promotion.Discount.Read)]
        public IActionResult List()
        {
            return View(new DiscountListModel());
        }

        [Permission(Permissions.Promotion.Discount.Read)]
        public async Task<IActionResult> DiscountList(GridCommand command, DiscountListModel model)
        {
            var mapper = MapperFactory.GetMapper<Discount, DiscountModel>();
            var query = _db.Discounts.AsNoTracking();

            if (model.SearchName.HasValue())
            {
                query = query.ApplySearchTermFilterFor(x => x.Name, model.SearchName);
            }
            if (model.SearchDiscountTypeId.HasValue)
            {
                query = query.Where(x => x.DiscountTypeId == model.SearchDiscountTypeId);
            }
            if (model.SearchUsePercentage.HasValue)
            {
                query = query.Where(x => x.UsePercentage == model.SearchUsePercentage.Value);
            }
            if (model.SearchRequiresCouponCode.HasValue)
            {
                query = query.Where(x => x.RequiresCouponCode == model.SearchRequiresCouponCode.Value);
            }

            var discounts = await query
                .Include(x => x.RuleSets)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await discounts.SelectAsync(async x =>
            {
                var m = await mapper.MapAsync(x);
                m.EditUrl = Url.Action("Edit", "Discount", new { id = x.Id, area = "Admin" });
                m.NumberOfRules = x.RuleSets.Count;
                m.DiscountTypeName = await Services.Localization.GetLocalizedEnumAsync(x.DiscountType);
                m.FormattedDiscountAmount = x.DiscountAmount != decimal.Zero
                    ? Services.CurrencyService.PrimaryCurrency.AsMoney(x.DiscountAmount).ToString(true)
                    : string.Empty;

                if (x.StartDateUtc.HasValue)
                {
                    m.StartDate = Services.DateTimeHelper.ConvertToUserTime(x.StartDateUtc.Value, DateTimeKind.Utc);
                }
                if (x.EndDateUtc.HasValue)
                {
                    m.EndDate = Services.DateTimeHelper.ConvertToUserTime(x.EndDateUtc.Value, DateTimeKind.Utc);
                }

                return m;
            })
            .AsyncToList();

            return Json(new GridModel<DiscountModel>
            {
                Rows = rows,
                Total = discounts.TotalCount
            });
        }

        [Permission(Permissions.Promotion.Discount.Create)]
        public IActionResult Create()
        {
            var model = new DiscountModel
            {
                LimitationTimes = 1
            };

            PrepareDiscountModel(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Promotion.Discount.Create)]
        public async Task<IActionResult> Create(DiscountModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<DiscountModel, Discount>();
                var discount = await mapper.MapAsync(model);
                _db.Discounts.Add(discount);

                await _db.SaveChangesAsync();

                await _ruleService.ApplyRuleSetMappingsAsync(discount, model.SelectedRuleSetIds);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewDiscount, T("ActivityLog.AddNewDiscount"), discount.Name);
                NotifySuccess(T("Admin.Promotions.Discounts.Added"));

                return continueEditing
                    ? RedirectToAction("Edit", new { id = discount.Id })
                    : RedirectToAction("Index");
            }

            PrepareDiscountModel(model, null);
            return View(model);
        }

        [Permission(Permissions.Promotion.Discount.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var discount = await _db.Discounts
                .Include(x => x.RuleSets)
                .Include(x => x.AppliedToCategories)
                .Include(x => x.AppliedToManufacturers)
                .Include(x => x.AppliedToProducts)
                .FindByIdAsync(id, false);

            if (discount == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<Discount, DiscountModel>();
            var model = await mapper.MapAsync(discount);

            PrepareDiscountModel(model, discount);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Promotion.Discount.Update)]
        public async Task<IActionResult> Edit(DiscountModel model, bool continueEditing)
        {
            var discount = await _db.Discounts
                .Include(x => x.RuleSets)
                .Include(x => x.AppliedToCategories)
                .Include(x => x.AppliedToManufacturers)
                .Include(x => x.AppliedToProducts)
                .FindByIdAsync(model.Id);

            if (discount == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var prevDiscountType = discount.DiscountType;

                var mapper = MapperFactory.GetMapper<DiscountModel, Discount>();
                await mapper.MapAsync(model, discount);

                await _ruleService.ApplyRuleSetMappingsAsync(discount, model.SelectedRuleSetIds);

                // Cleanup old references (if changed). "HasDiscountsApplied" properties are updated by hook.
                if (prevDiscountType == DiscountType.AssignedToCategories && discount.DiscountType != DiscountType.AssignedToCategories)
                {
                    discount.AppliedToCategories.Clear();
                }

                if (prevDiscountType == DiscountType.AssignedToManufacturers && discount.DiscountType != DiscountType.AssignedToManufacturers)
                {
                    discount.AppliedToManufacturers.Clear();
                }

                if (prevDiscountType == DiscountType.AssignedToSkus && discount.DiscountType != DiscountType.AssignedToSkus)
                {
                    discount.AppliedToProducts.Clear();
                }

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditDiscount, T("ActivityLog.EditDiscount"), discount.Name);
                NotifySuccess(T("Admin.Promotions.Discounts.Updated"));

                return continueEditing
                    ? RedirectToAction("Edit", new { id = discount.Id })
                    : RedirectToAction("Index");
            }

            PrepareDiscountModel(model, discount);
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Discount.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _db.Discounts.FindByIdAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            _db.Discounts.Remove(discount);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteDiscount, T("ActivityLog.DeleteDiscount"), discount.Name);
            NotifySuccess(T("Admin.Promotions.Discounts.Deleted"));

            return RedirectToAction("List");
        }

        #region Discount usage history

        [Permission(Permissions.Promotion.Discount.Read)]
        public async Task<IActionResult> DiscountUsageHistoryList(GridCommand command, int discountId)
        {
            var historyEntries = await _db.DiscountUsageHistory
                .AsNoTracking()
                .Where(x => x.DiscountId == discountId)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = historyEntries.Select(x => new DiscountUsageHistoryModel
            {
                Id = x.Id,
                DiscountId = x.DiscountId,
                OrderId = x.OrderId,
                CreatedOnUtc = x.CreatedOnUtc,
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                OrderEditUrl = Url.Action("Edit", "Order", new { id = x.OrderId, area = "Admin" })
            })
            .ToList();

            return Json(new GridModel<DiscountUsageHistoryModel>
            {
                Rows = rows,
                Total = historyEntries.TotalCount
            });
        }

        [Permission(Permissions.Promotion.Discount.Update)]
        public async Task<IActionResult> DiscountUsageHistoryDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var historyEntries = await _db.DiscountUsageHistory.GetManyAsync(ids, true);

                _db.DiscountUsageHistory.RemoveRange(historyEntries);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        private void PrepareDiscountModel(DiscountModel model, Discount discount)
        {
            if (discount != null)
            {
                var language = Services.WorkContext.WorkingLanguage;

                model.SelectedRuleSetIds = discount.RuleSets.Select(x => x.Id).ToArray();

                ViewBag.AppliedToCategories = discount.AppliedToCategories
                    .Where(x => x != null && !x.Deleted)
                    .Select(x => new DiscountAppliedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name, language) })
                    .ToList();

                ViewBag.AppliedToManufacturers = discount.AppliedToManufacturers
                    .Where(x => x != null && !x.Deleted)
                    .Select(x => new DiscountAppliedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name, language) })
                    .ToList();

                ViewBag.AppliedToProducts = discount.AppliedToProducts
                    .Where(x => x != null && !x.Deleted)
                    .Select(x => new DiscountAppliedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name, language) })
                    .ToList();
            }

            ViewBag.PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
        }
    }
}
