using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Polls.Components;
using Smartstore.Polls.Extensions;
using Smartstore.Polls.Hooks;
using Smartstore.Polls.Models.Mappers;
using Smartstore.Polls.Models.Public;

namespace Smartstore.Polls.Filters
{
    public class PollFilter : IAsyncResultFilter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly Lazy<IWidgetProvider> _widgetProvider;

        public PollFilter(
            SmartDbContext db,
            ICommonServices services,
            Lazy<IWidgetProvider> widgetProvider)
        {
            _db = db;
            _services = services;
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            // TODO: (mh) (core) This won't work as MyAccount can also be boards. Can we determine some other way we are on pages of the MyAccount menu?
            // INFO: ViewBag ain't no option for early return either. I can't set a property in MyAccount-Layout before this filter runs.
            // RE: Then we need a simple generic purpose action attribute, something like "ActionMetadataAttribute(string name, string value)".
            //     We could then annotate all MyAccount actions with e.g. [ActionMetadata("Layout", "MyAccount")]. 
            //     Search for "ActionDescriptor.FilterDescriptors" or ".EndpointMetadata" to see how to query for such annotations.
            //     OR: you could do the heavy work in a ViewComponent and we forget about all this shit here ;-) !!

            // Analyze route get outta here if it's not blog or myaccount.
            //string controllerName = (string)filterContext.RouteData.Values["Controller"];
            //if (controllerName != "Blog" && controllerName != "Customer")
            //{
            //    await next();
            //    return;
            //}    

            // Should only run on a full view rendering result or HTML ContentResult.
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {// TODO: (mh) (core) Extremely bad API design!! (hitting db before cache delegate, widget zone nightmare). TBD with MC.
                
                var storeId = _services.StoreContext.CurrentStore.Id;
                var languageId = _services.WorkContext.WorkingLanguage.Id;
                var polls = await _db.Polls()
                    .AsNoTracking()
                    .ApplyStandardFilter(storeId, languageId)
                    .Where(x => x.PollAnswers.Count > 0)
                    .Include(x => x.PollAnswers)
                    .ToListAsync();

                if (polls.Count == 0)
                {
                    await next();
                    return;
                }
                
                foreach (var poll in polls)
                {
                    var cacheKey = string.Format(ModelCacheInvalidator.POLL_BY_SYSTEMNAME_MODEL_KEY, poll.Id, poll.SystemKeyword, languageId, storeId);
                    var cachedModel = await _services.Cache.GetAsync(cacheKey, async () =>
                    {
                        return await poll.MapAsync(new { SetAlreadyVotedProperty = false });
                    });

                    // "AlreadyVoted" property of "PollModel" object depends on the current customer. Let's update it.
                    // But first we need to clone the cached model (the updated one should not be cached).
                    var model = (PublicPollModel)cachedModel.Clone();
                    model.AlreadyVoted = await _db.PollAnswers().GetAlreadyVotedAsync(model.Id, _services.WorkContext.CurrentCustomer.Id);
                    
                    var widget = new ComponentWidgetInvoker(typeof(PollBlockViewComponent), model);
                    if (poll.SystemKeyword == "Blog") 
                    {
                        _widgetProvider.Value.RegisterWidget(new[] { "blog_right_bottom" }, widget);
                    }
                    else if (poll.SystemKeyword == "MyAccountMenu")
                    {
                        _widgetProvider.Value.RegisterWidget(new[] { "myaccount_menu_after" }, widget);
                    }
                }
            }

            await next();
        }
    }
}
