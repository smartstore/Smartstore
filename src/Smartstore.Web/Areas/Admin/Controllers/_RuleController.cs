using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    public class RuleController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        
        public RuleController(SmartDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// Gets a list of all available rule sets. 
        /// </summary>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="RuleScope">Specifies the <see cref="RuleScope"/>.</param>
        /// <returns>List of all rule sets as JSON.</returns>
        public async Task<IActionResult> AllRuleSets(string selectedIds, RuleScope? scope)
        {
            var ruleSets = await _db
                .RuleSets
                .AsNoTracking()
                .ApplyStandardFilter(scope, includeHidden: true)
                .ToListAsync();

            var selectedArr = selectedIds.ToIntArray();

            ruleSets.Add(new RuleSetEntity { Id = -1, Name = T("Admin.Rules.AddRule").Value + "…" });

            // TODO: (mh) (core) Implement Create & Edit.
            var data = ruleSets
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = selectedArr.Contains(x.Id),
                    UrlTitle = x.Id == -1 ? string.Empty : T("Admin.Rules.OpenRule").Value,
                    Url = x.Id == -1
                        ? Url.Action("Create", "Rule", new { scope, area = "Admin" })
                        : Url.Action("Edit", "Rule", new { id = x.Id, area = "Admin" })
                })
                .ToList();

            return new JsonResult(data);
        }

    }
}
