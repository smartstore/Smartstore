using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    //[AdminAuthorize]
    public class TopicController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        
        public TopicController(
            SmartDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// TODO: (mh) (core) Add documentation.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="selectedId"></param>
        /// <param name="includeWidgets"></param>
        /// <param name="includeHomePage"></param>
        /// <returns></returns>
        public async Task<IActionResult> AllTopics(string label, int selectedId, bool includeWidgets = false, bool includeHomePage = false)
        {
            var topics = await _db.Topics
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .Where(x => includeWidgets || !x.RenderAsWidget)
                .ToListAsync();
            
            var list = topics
                .Select(x =>
                {
                    var item = new ChoiceListItem
                    {
                        Id = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Title).Value.NullEmpty() ?? x.SystemName,
                        Selected = x.Id == selectedId
                    };

                    if (!item.Text.EqualsNoCase(x.SystemName))
                    {
                        item.Description = x.SystemName;
                    }

                    return item;
                })
                .ToList();

            if (label.HasValue())
            {
                list.Insert(0, new ChoiceListItem { Id = "0", Text = label, Selected = false });
            }

            if (includeHomePage)
            {
                list.Insert(0, new ChoiceListItem { Id = "-10", Text = T("Admin.ContentManagement.Homepage").Value, Selected = false });
            }

            return new JsonResult(list);
        }
    }
}
