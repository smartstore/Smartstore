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
        /// Gets a list of all available topics. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="includeWidgets">Specifies whether to include topics which are defined to be rendered as Widgets.</param>
        /// <param name="includeHomePage">Specifies whether to include homepage text.</param>
        /// <returns>List of all topics as JSON.</returns>
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
