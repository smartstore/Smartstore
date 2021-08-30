using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Discounts;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class DiscountController : AdminController
    {
        private readonly SmartDbContext _db;

        public DiscountController(SmartDbContext db)
        {
            _db = db;
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
        public async Task<IActionResult> DiscountList(GridCommand command)
        {
            var mapper = MapperFactory.GetMapper<Discount, DiscountModel>();
            // TODO: (mg) (core) add search for discount list: DiscountType, UsePercentage, RequiresCouponCode
            var query = _db.Discounts.AsNoTracking();

            var discounts = await query
                .Include(x => x.RuleSets)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await discounts.SelectAsync(async x =>
            {
                var model = await mapper.MapAsync(x);
                model.EditUrl = Url.Action("Edit", "Discount", new { id = x.Id, area = "Admin" });
                model.NumberOfRules = x.RuleSets.Count;
                model.DiscountTypeName = await Services.Localization.GetLocalizedEnumAsync(x.DiscountType);
                model.FormattedDiscountAmount = !x.UsePercentage
                    ? Services.CurrencyService.PrimaryCurrency.AsMoney(x.DiscountAmount).ToString(true)
                    : string.Empty;

                return model;
            })
            .AsyncToList();

            return Json(new GridModel<DiscountModel>
            {
                Rows = rows,
                Total = discounts.TotalCount
            });
        }


        private void PrepareDiscountModel(DiscountModel model, Discount discount)
        {
            if (discount != null)
            {
                var language = Services.WorkContext.WorkingLanguage;

                model.SelectedRuleSetIds = discount.RuleSets.Select(x => x.Id).ToArray();

                model.AppliedToCategories = discount.AppliedToCategories
                    .Where(x => x != null && !x.Deleted)
                    .Select(x => new DiscountModel.AppliedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name, language) })
                    .ToList();

                model.AppliedToManufacturers = discount.AppliedToManufacturers
                    .Where(x => x != null && !x.Deleted)
                    .Select(x => new DiscountModel.AppliedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name, language) })
                    .ToList();

                model.AppliedToProducts = discount.AppliedToProducts
                    .Where(x => x != null && !x.Deleted)
                    .Select(x => new DiscountModel.AppliedToEntityModel { Id = x.Id, Name = x.GetLocalized(y => y.Name, language) })
                    .ToList();
            }

            ViewBag.PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
        }
    }
}
